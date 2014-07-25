using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using NuGet.Services.Metrics.Core;
using System.Web.Configuration;
using System.Diagnostics;

[assembly: OwinStartup(typeof(NuGet.Services.Metrics.Website.WebsiteStartup))]

namespace NuGet.Services.Metrics.Website
{
    public class WebsiteStartup
    {
        private PackageStatsHandler _packageStatsHandler;
        private const string SqlConfigurationKey = "Metrics.SqlServer";
        public void Configuration(IAppBuilder appBuilder)
        {
            var connectionStringSetting = WebConfigurationManager.ConnectionStrings[SqlConfigurationKey];
            _packageStatsHandler = new PackageStatsHandler(connectionStringSetting.ConnectionString);
            appBuilder.Run(Invoke);
        }

        private async Task Invoke(IOwinContext context)
        {
            var requestUri = context.Request.Uri;
            Trace.TraceInformation("Request received : {0}", requestUri.AbsoluteUri);

            await _packageStatsHandler.Invoke(context);
            Trace.TraceInformation("Request accepted. Processing...");
        }
    }
}
