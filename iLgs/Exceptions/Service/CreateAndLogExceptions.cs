using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Exceptions
{
    public interface ICreateAndLogExceptions
    {
        ServiceException CreateAndLogServiceException(Exception exception);
        DependencyException CreateAndLogDependencyException(Exception exception);
        DependencyException CreateAndLogCriticalDependencyException(Exception exception);
        ValidationException CreateAndLogValidationException(Xeption exception);
    }

    public class CreateAndLogExceptions : ICreateAndLogExceptions
    {
        public ServiceException CreateAndLogServiceException(Exception exception)
        {
            var serviceException = new ServiceException(exception);
            //_loggingAgent.LogError(serviceException);

            return serviceException;
        }

        public DependencyException CreateAndLogDependencyException(Exception exception)
        {
            var dependencyException = new DependencyException(exception);
            //_loggingAgent.LogError(dependencyException);

            return dependencyException;
        }

        public DependencyException CreateAndLogCriticalDependencyException(Exception exception)
        {
            var dependencyException = new DependencyException(exception);
            //_loggingAgent.LogCritical(dependencyException);

            return dependencyException;
        }

        public ValidationException CreateAndLogValidationException(Xeption exception)
        {
            var validationException = new ValidationException(exception);
            //_loggingAgent.LogError(validationException);

            return validationException;
        }        
    }
}