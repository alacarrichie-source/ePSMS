using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using iLgs.Exceptions;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace iLgs.Services
{
    public class ExceptionService<T> : IExceptionService<T> where T : class
    {
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();

        public ExceptionService()
        {
        }

        public ValueTask TryCatch(Func<ValueTask> nonReturningFunction)
        {
            try
            {
                return nonReturningFunction();
            }
            catch (RecordNotFoundException notFoundException)
            {
                throw notFoundException;
            }
            catch (RecordAlreadyExistsException recordAlreadyExistsException)
            {
                throw recordAlreadyExistsException;
            }
            catch (InvalidValueException invalidValueException)
            {
                throw invalidValueException;
            }
            catch (RecordAlreadyPostedException recordAlreadyPostedException)
            {
                throw recordAlreadyPostedException;
            }
            catch (RecordRelationshipException recordRelationshipExistsException)
            {
                throw recordRelationshipExistsException;
            }
            catch (SqlException sqlException)
            {
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

                throw exceptions.CreateAndLogDependencyException(recordLockedException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                throw exceptions.CreateAndLogDependencyException(dbUpdateException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
            }
        }

        public async ValueTask TryCatchAsync(Func<ValueTask> nonReturningFunction)
        {
            try
            {
                await nonReturningFunction();
            }
            catch (RecordNotFoundException notFoundException)
            {
                throw notFoundException;
            }
            catch (RecordAlreadyExistsException recordAlreadyExistsException)
            {
                throw recordAlreadyExistsException;
            }
            catch (InvalidValueException invalidValueException)
            {
                throw invalidValueException;
            }
            catch (RecordAlreadyPostedException recordAlreadyPostedException)
            {
                throw recordAlreadyPostedException;
            }
            catch (RecordRelationshipException recordRelationshipExistsException)
            {
                throw recordRelationshipExistsException;
            }
            catch (SqlException sqlException)
            {
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

                throw exceptions.CreateAndLogDependencyException(recordLockedException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                throw exceptions.CreateAndLogDependencyException(dbUpdateException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
            }
        }

        public ValueTask<T> TryCatch(Func<ValueTask<T>> returningFunction)
        {
            try
            {
                return returningFunction();
            }
            catch (RecordNotFoundException notFoundException)
            {
                throw notFoundException;
            }
            catch (RecordAlreadyExistsException recordAlreadyExistsException)
            {
                throw recordAlreadyExistsException;
            }
            catch (InvalidValueException invalidValueException)
            {
                throw invalidValueException;
            }
            catch (RecordAlreadyPostedException recordAlreadyPostedException)
            {
                throw recordAlreadyPostedException;
            }
            catch (RecordRelationshipException recordRelationshipExistsException)
            {
                throw recordRelationshipExistsException;
            }
            catch (SqlException sqlException)
            {
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

                throw exceptions.CreateAndLogDependencyException(recordLockedException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                throw exceptions.CreateAndLogDependencyException(dbUpdateException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
            }
        }

        public async ValueTask<T> TryCatchAsync(Func<ValueTask<T>> returningFunction)
        {
            try
            {
                return await returningFunction();
            }
            catch (RecordNotFoundException notFoundException)
            {
                throw notFoundException;
            }
            catch (RecordAlreadyExistsException recordAlreadyExistsException)
            {
                throw recordAlreadyExistsException;
            }
            catch (InvalidValueException invalidValueException)
            {
                throw invalidValueException;
            }
            catch (RecordAlreadyPostedException recordAlreadyPostedException)
            {
                throw recordAlreadyPostedException;
            }
            catch (RecordRelationshipException recordRelationshipExistsException)
            {
                throw recordRelationshipExistsException;
            }
            catch (SqlException sqlException)
            {
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

                throw exceptions.CreateAndLogDependencyException(recordLockedException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                throw exceptions.CreateAndLogDependencyException(dbUpdateException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
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
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
            }
        }
    }
}