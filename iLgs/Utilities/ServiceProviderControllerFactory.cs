using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace iLgs.Utilities
{
    public class ServiceProviderControllerFactory : DefaultControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderControllerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            return (IController)_serviceProvider.GetService(controllerType);
        }
    }
}