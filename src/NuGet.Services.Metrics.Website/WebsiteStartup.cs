using Microsoft.Owin;
using NuGet.Services.Metrics.Core;
using Owin;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Configuration;

[assembly: OwinStartup(typeof(NuGet.Services.Metrics.Website.WebsiteStartup))]

namespace NuGet.Services.Metrics.Website
{
    public class WebsiteStartup
    {
        private PackageStatsHandler _packageStatsHandler;
        public void Configuration(IAppBuilder appBuilder)
        {
            var connectionStringSetting = WebConfigurationManager.ConnectionStrings[MetricsAppSettings.SqlConfigurationKey];
            if (connectionStringSetting == null)
            {
                throw new ArgumentNullException("Connection String '" + MetricsAppSettings.SqlConfigurationKey + "' cannot be found");
            }
            WebConfigurationManager.AppSettings[MetricsAppSettings.SqlConfigurationKey] = connectionStringSetting.ConnectionString;

            var appSettingDictionary = WebConfigurationManager.AppSettings
                                            .Cast<string>()
                                            .Select(s => new { Key = s, Value = WebConfigurationManager.AppSettings[s] })
                                            .ToDictionary(p => p.Key, p => p.Value);
            _packageStatsHandler = new PackageStatsHandler(appSettingDictionary);
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
