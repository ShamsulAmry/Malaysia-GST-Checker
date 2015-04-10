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

            builder.RegisterType<GstWebScraper>()
                .Named<IGstDataSource>("kastam");
            builder.RegisterDecorator<IGstDataSource>(source => new GstAzureStorage(source), fromKey: "kastam");

            var container = builder.Build();
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}