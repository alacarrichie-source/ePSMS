using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using iLgs.Exceptions;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using iLgs.Models;
using iLgs.Exceptions.Service;
using iLgs.Services.Validators;
using System.Collections;

namespace iLgs.Services
{
    public class ExceptionService<T> : IExceptionService<T> where T : class
    {
        //private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        //private delegate ValueTask<T> ReturningFunction();
        //private delegate IQueryable<T> ReturningQueryableFunction();
        private readonly ILoggingService _loggingService;

        public ExceptionService()
        {
            _loggingService = new LoggingService();
        }

        //public ValueTask TryCatch(Func<ValueTask> nonReturningFunction)
        //{
        //    try
        //    {
        //        return nonReturningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RequiredFieldException requiredFieldException)
        //    {
        //        throw requiredFieldException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}

        //public async ValueTask TryCatch(Func<ValueTask> nonReturningFunction)
        //{
        //    try
        //    {
        //        await nonReturningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RequiredFieldException requiredFieldException)
        //    {
        //        throw requiredFieldException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}

        //public ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunction)
        //{
        //    try
        //    {
        //        return returningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RequiredFieldException requiredFieldException)
        //    {
        //        throw requiredFieldException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}

        //public async ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunction)
        //{
        //    try
        //    {
        //        return await returningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RequiredFieldException requiredFieldException)
        //    {
        //        throw requiredFieldException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}

        //public IQueryable<T> TryCatch(Func<IQueryable<T>> returningQueryableFunction)
        //{
        //    try
        //    {
        //        return returningQueryableFunction();
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}

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
            catch (NotFoundException nullException)
            {
                throw CreateAndLogValidationException(nullException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageException =
                    new FailedStorageException(sqlException);

                throw CreateAndLogCriticalDependencyException(failedStorageException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsStudentException =
                    new AlreadyExistsException(duplicateKeyException);

                throw CreateAndLogDependencyValidationException(alreadyExistsStudentException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedStudentException = new LockedException(dbUpdateConcurrencyException);

                throw CreateAndLogDependencyException(lockedStudentException);
            }
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
            //_loggingService.LogError(validationException);

            return validationException;
        }
        private DependencyValidationException CreateAndLogDependencyValidationException(Xeption exception)

        {
            var dependencyValidationException =
                new DependencyValidationException(exception);

            //_loggingService.LogError(dependencyValidationException);

            return dependencyValidationException;
        }

        private DependencyException CreateAndLogDependencyException(Exception exception)
        {
            var dependencyException = new DependencyException(exception);
            //_loggingService.LogError(dependencyException);

            return dependencyException;
        }

        private DependencyException CreateAndLogCriticalDependencyException(Exception exception)
        {
            var dependencyException = new DependencyException(exception);
            //_loggingService.LogCritical(dependencyException);

            return dependencyException;
        }

        private ServiceException CreateAndLogServiceException(Exception exception)
        {
            var serviceException = new ServiceException(exception);
            //_loggingService.LogError(serviceException);

            return serviceException;
        }


    }
}