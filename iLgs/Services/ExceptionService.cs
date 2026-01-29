using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Services.Logs;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public interface IExceptionService<T> where T : class
    {        
        ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunction);
        IQueryable<T> TryCatch(Func<IQueryable<T>> returningQueryableFunction);
    }

    public class ExceptionService<T> : IExceptionService<T> where T : class
    {
        private readonly ILoggingService _loggingService;

        public ExceptionService()
        {
            _loggingService = new LoggingService();
        }
        
        public async ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunction)
        {
            try
            {
                return await returningFunction();
            }
            catch (NullException nullException)
            {
                throw CreateAndLogValidationException(nullException);
            }
            catch (InvalidModelException invalidException)
            {
                throw CreateAndLogValidationException(invalidException);                
            }
            catch (InvalidValueException invalidValueException)
            {
                throw new ValidationException(invalidValueException);
            }
            catch (NotFoundException nullException)
            {
                throw CreateAndLogValidationException(nullException);                
            }
            catch (ArgumentOutOfRangeException outOfRangeException)
            {
                throw CreateAndLogValidationException(outOfRangeException);
            }
            catch (DbEntityValidationException dbEntityValidationException)
            {
                //// Manually create a new DbEntityValidationException with a custom inner exception
                //var newInnerException = new Exception(dbEntityValidationException.Message); // This is the custom inner exception
                //var newDbEntityValidationException = new DbEntityValidationException("Validation failed for one or more entities.", dbEntityValidationException.EntityValidationErrors, newInnerException);

                //throw CreateAndLogValidationException(newInnerException);

                // Build a detailed validation error message
                var validationErrors = dbEntityValidationException.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}");

                var fullErrorMessage = string.Join("; ", validationErrors);
                var detailedMessage = $"{dbEntityValidationException.Message} The validation errors are: {fullErrorMessage}";

                // Log the detailed validation info
                //_logger.LogError(detailedMessage);

                // Create a wrapped exception preserving the details
                var newDbEntityValidationException = new DbEntityValidationException(
                    detailedMessage,
                    dbEntityValidationException.EntityValidationErrors,
                    dbEntityValidationException
                );

                // Pass the rich exception (with validation details) to your centralized handler
                throw CreateAndLogValidationException(newDbEntityValidationException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageException =
                    new FailedStorageException(sqlException);

                throw CreateAndLogCriticalDependencyException(failedStorageException);
            }
            catch (RecordAlreadyPostedException alreadyPostedException)
            {
                //throw CreateAndLogLockedException(alreadyPostedException);
                throw CreateAndLogValidationException(alreadyPostedException);                
            }
            catch (RecordNotYetPostedException notYetPostedException)
            {
                throw CreateAndLogValidationException(notYetPostedException);
            }
            catch (RecordRelationshipException relationshipException)
            {
                throw CreateAndLogValidationException(relationshipException);
            }
            catch (RecordAlreadyExistsException recordExistsException)
            {
                throw CreateAndLogValidationException(recordExistsException);
            }
            catch (RecordLockedException recordLockedException)
            {
                throw CreateAndLogValidationException(recordLockedException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsStudentException =
                    new AlreadyExistsException(duplicateKeyException);

                throw CreateAndLogDependencyValidationException(alreadyExistsStudentException);
            }
            //catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            //{
            //    var lockedStudentException = new RecordLockedException(dbUpdateConcurrencyException);

            //    throw CreateAndLogDependencyException(lockedStudentException);
            //}
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageException =
                    new FailedStorageException(dbUpdateException);

                throw CreateAndLogDependencyException(failedStorageException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw CreateAndLogServiceException(failedServiceException);
            }
        }

        public IQueryable<T> TryCatch(Func<IQueryable<T>> returningQueryableFunction)
        {
            try
            {
                return returningQueryableFunction();
            }
            catch (SqlException sqlException)
            {
                throw CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw CreateAndLogServiceException(failedServiceException);
            }
        }
        
        private ValidationException CreateAndLogValidationException(Xeption exception)
        {            
            var validationException = new ValidationException(exception);
            _loggingService.LogError(validationException);

            return validationException;
        }

        private AlreadyExistsException CreateAndLogAlreadyExistsException(Xeption exception)
        {
            var alreadyExistsException = new AlreadyExistsException(exception);
            _loggingService.LogError(alreadyExistsException);

            return alreadyExistsException;
        }

        private ValidationException CreateAndLogValidationException(DbEntityValidationException exception)
        {
            var validationException = new ValidationException(exception);
            _loggingService.LogError(validationException);

            return validationException;
        }

        private ValidationException CreateAndLogValidationException(Exception exception)
        {
            var validationException = new ValidationException(exception);
            _loggingService.LogError(validationException);

            return validationException;
        }

        private RecordLockedException CreateAndLogLockedException(Xeption exception)
        {
            var lockedException = new RecordLockedException(exception);
            //var lockedException =
            //    new RecordLockedException(validationException);

            _loggingService.LogError(lockedException);

            return lockedException;
        }


        private DependencyValidationException CreateAndLogDependencyValidationException(Xeption exception)

        {
            var dependencyValidationException =
                new DependencyValidationException(exception);

            _loggingService.LogError(dependencyValidationException);

            //int logId = _loggingService.LogErrorWithId(LogLevel.Error, exception.Message, exception.ToString());

            //// Append Log ID to exception message
            //var trackedException = new Exception($"Log ID: {logId} | {exception.Message}", exception);


            return dependencyValidationException;
        }

        private DependencyException CreateAndLogDependencyException(Exception exception)
        {
            var dependencyException = new DependencyException(exception);
            _loggingService.LogError(dependencyException);
            
            return dependencyException;
        }
        
        private DependencyException CreateAndLogCriticalDependencyException(Exception exception)
        {
            var dependencyException = new DependencyException(exception);
            _loggingService.LogCritical(dependencyException);

            return dependencyException;
        }

        private ServiceException CreateAndLogServiceException(Exception exception)
        {
            var serviceException = new ServiceException(exception);
            _loggingService.LogError(serviceException);

            return serviceException;
        }


    }
}