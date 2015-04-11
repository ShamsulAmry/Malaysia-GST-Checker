using System.Reflection;
using System.Web.Http;
using Amry.Gst.Web.Models;
using Autofac;
using Autofac.Integration.WebApi;

namespace Amry.Gst.Web
{
    public static class AutofacConfig
    {
        public static void Register()
        {
            var builder = new ContainerBuilder();
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            builder.RegisterType<GstWebScraperPool>()
                .Named<IGstDataSource>("kastam-pool");
            builder.RegisterDecorator<IGstDataSource>(source => new GstAzureStorage(source), fromKey: "kastam-pool")
                .SingleInstance();

            var container = builder.Build();
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}