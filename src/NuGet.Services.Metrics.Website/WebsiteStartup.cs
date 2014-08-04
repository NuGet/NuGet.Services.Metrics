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
        private const string CommandTimeoutKey = "Metrics.CommandTimeout";
        public void Configuration(IAppBuilder appBuilder)
        {
            var connectionStringSetting = WebConfigurationManager.ConnectionStrings[SqlConfigurationKey];
            if (connectionStringSetting == null)
            {
                throw new ArgumentNullException("Connection String '" + SqlConfigurationKey + "' cannot be found");
            }

            var commandTimeoutString = WebConfigurationManager.AppSettings[CommandTimeoutKey];
            int commandTimeout = 0;
            if (!String.IsNullOrEmpty(commandTimeoutString))
            {
                Int32.TryParse(commandTimeoutString, out commandTimeout);
            }

            _packageStatsHandler = new PackageStatsHandler(connectionStringSetting.ConnectionString, commandTimeout);
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
