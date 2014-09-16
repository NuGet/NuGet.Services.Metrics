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
        public void Configuration(IAppBuilder appBuilder)
        {
            var connectionStringSetting = WebConfigurationManager.ConnectionStrings[PackageStatsHandler.SqlConfigurationKey];
            if (connectionStringSetting == null)
            {
                throw new ArgumentNullException("Connection String '" + PackageStatsHandler.SqlConfigurationKey + "' cannot be found");
            }
            WebConfigurationManager.AppSettings[PackageStatsHandler.SqlConfigurationKey] = connectionStringSetting.ConnectionString;

            _packageStatsHandler = new PackageStatsHandler(WebConfigurationManager.AppSettings);
            appBuilder.Run(Invoke);
        }

        private async Task Invoke(IOwinContext context)
        {
            var requestUri = context.Request.Uri;
            Trace.WriteLine("Request received : " + requestUri.AbsoluteUri);

            await _packageStatsHandler.Invoke(context);
            Trace.WriteLine("Request accepted. Processing...");
        }
    }
}
