using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(iLgs.Startup))]
namespace iLgs
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
