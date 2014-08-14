using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Configuration;
using Microsoft.Owin;
using NuGet.Services.Metrics.Core;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Services.Metrics.Website.WebsiteStartup))]

namespace NuGet.Services.Metrics.Website
{
    public class WebsiteStartup
    {
        private PackageStatsHandler _packageStatsHandler;
        private const string SqlConfigurationKey = "Metrics.SqlServer";
        private const string CommandTimeoutKey = "Metrics.CommandTimeout";
        private const string CommandRetriesKey = "Metrics.CommandRetries";
        public void Configuration(IAppBuilder appBuilder)
        {
            Trace.TraceInformation("Starting Metrics Service Website...");
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

            var commandRetriesString = WebConfigurationManager.AppSettings[CommandRetriesKey];
            int commandRetries = 0;
            if (!String.IsNullOrEmpty(commandRetriesString))
            {
                Int32.TryParse(commandRetriesString, out commandRetries);
            }

            _packageStatsHandler = new PackageStatsHandler(connectionStringSetting.ConnectionString, commandTimeout, commandRetries);
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
