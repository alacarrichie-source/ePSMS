using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Web.Http.Dependencies;

namespace iLgs.Utilities
{
    public class DefaultDependencyResolver : System.Web.Mvc.IDependencyResolver
    {
        private readonly ServiceProvider _serviceProvider;

        public DefaultDependencyResolver(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _serviceProvider.GetServices(serviceType) ?? new List<object>();
        }
    }

    public class DefaultWebApiDependencyResolver : System.Web.Http.Dependencies.IDependencyResolver, IDependencyScope
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultWebApiDependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _serviceProvider.GetServices(serviceType) ?? Array.Empty<object>();
        }

        public IDependencyScope BeginScope()
        {
            var scope = _serviceProvider.CreateScope();
            return new DefaultWebApiDependencyResolver(scope.ServiceProvider);
        }

        public void Dispose()
        {
            // No operation needed here because ASP.NET Web API disposes the scope automatically
        }
    }
}