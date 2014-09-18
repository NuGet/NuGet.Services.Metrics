using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;


namespace NuGet.Services.Metrics.Core
{
    public static class MetricsAppSettings
    {
        public const string SqlConfigurationKey = "Metrics.SqlServer";
        public const string CommandTimeoutKey = "Metrics.CommandTimeout";
        public const string CatalogStorageAccountKey = "Metrics.CatalogStorageAccount";
        public const string CatalogPathKey = "Metrics.CatalogPath";
        public const string CatalogIndexUrlKey = "Metrics.CatalogIndexUrl";
        public const string IsLocalCatalogKey = "Metrics.IsLocalCatalog";
        public const string CatalogPageSizeKey = "Metrics.CatalogPageSize";
        // TODO : public const string CatalogCommitSizeKey = "Metrics.CatalogCommitSize";
        public const string CatalogItemPackageStatsCountKey = "Metrics.CatalogItemPackageStatsCount";
        public const string ShouldUseDBAndCatalog = "Metrics.ShouldUseDBAndCatalog";

        /// <summary>
        /// Returns the value from the dictionary if found. Otherwise, null
        /// </summary>
        public static string TryGetSetting(IDictionary<string, string> appSettingDictionary, string key)
        {
            string value = null;
            appSettingDictionary.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Returns the value as int from the dictionary if found and parsable. Otherwise, null
        /// </summary>
        public static int? TryGetIntSetting(IDictionary<string, string> appSettingDictionary, string key)
        {
            string intString = TryGetSetting(appSettingDictionary, key);
            int intValue;
            if (!String.IsNullOrEmpty(intString) && Int32.TryParse(intString, out intValue))
            {
                return intValue;
            }

            return null;
        }

        /// <summary>
        /// Returns the value as bool from the dictionary if found and parsable. Otherwise, false
        /// </summary>
        public static bool TryGetBooleanSetting(IDictionary<string, string> appSettingDictionary, string key)
        {
            string booleanString = TryGetSetting(appSettingDictionary, key);
            bool booleanValue = false;
            if (!String.IsNullOrEmpty(booleanString) && Boolean.TryParse(booleanString, out booleanValue))
            {
                return booleanValue;
            }

            return false;
        }
    }
    public class PackageStatsHandler
    {
        private readonly List<MetricsStorage> _metricsStorageList;
        private int _count = 0;
        private const string HTTPPost = "POST";
        private static readonly PathString Root = new PathString("/");
        private static readonly PathString DownloadEvent = new PathString("/DownloadEvent");

        public PackageStatsHandler(IDictionary<string, string> appSettingDictionary)
        {
            string connectionString = MetricsAppSettings.TryGetSetting(appSettingDictionary, MetricsAppSettings.SqlConfigurationKey);
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Metrics.SqlServer is not present in the configuration");
            }

            int commandTimeout = MetricsAppSettings.TryGetIntSetting(appSettingDictionary, MetricsAppSettings.CommandTimeoutKey) ?? 0;
            string catalogIndexUrl = MetricsAppSettings.TryGetSetting(appSettingDictionary, MetricsAppSettings.CatalogIndexUrlKey);
            string catalogStorageAccount = MetricsAppSettings.TryGetSetting(appSettingDictionary, MetricsAppSettings.CatalogStorageAccountKey);

            _metricsStorageList = new List<MetricsStorage>();
            if(String.IsNullOrEmpty(catalogIndexUrl) && String.IsNullOrEmpty(catalogStorageAccount))
            {
                // CatalogIndexUrl is not provided. Only database should be used for storing package statistics
                _metricsStorageList.Add(new DatabaseMetricsStorage(connectionString, commandTimeout));
            }
            else
            {
                // CatalogIndexUrl is provided. Clearly, Catalog should be used. Check if DB should be used as well
                bool shouldUseDBAndCatalog = MetricsAppSettings.TryGetBooleanSetting(appSettingDictionary, MetricsAppSettings.ShouldUseDBAndCatalog);
                if(shouldUseDBAndCatalog)
                {
                    // DB should be used too. Add it to the list of Metrics Storage
                    _metricsStorageList.Add(new DatabaseMetricsStorage(connectionString, commandTimeout));
                }

                // Since CatalogIndexUrl is provided, Catalog should be used to store Metrics
                _metricsStorageList.Add(new CatalogMetricsStorage(connectionString, commandTimeout, appSettingDictionary));
            }
        }

        public async Task Invoke(IOwinContext context)
        {
            if (context.Request.Path.StartsWithSegments(Root))
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("NuGet Metrics Service: OK");
            }
            else if (context.Request.Path.StartsWithSegments(DownloadEvent))
            {
                if (context.Request.Method != HTTPPost)
                {
                    context.Response.Headers.Add("Allow", new[] { "POST" });
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await context.Response.WriteAsync("Only HTTP POST requests are accepted");
                    return;
                }

                // TODO: NEED TO ADD CHECK TO ENSURE THAT THE STREAM IS NOT TOO LONG
                //       Note that Stream 'IOwinRequest.Body' does not support length
                using (var streamReader = new StreamReader(context.Request.Body))
                {
                    try
                    {
                        var jsonString = await streamReader .ReadToEndAsync();
                        var jToken = JToken.Parse(jsonString);
                        Task.Run(() => ProcessJToken(jToken));
                        context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("Page is not found");                
            }
        }

        private async Task ProcessJToken(JToken jToken)
        {
            if (jToken is JObject)
            {
                await ProcessJObject((JObject)jToken);
            }
            else if (jToken is JArray)
            {
                await ProcessJArray((JArray)jToken);
            }
        }

        private async Task ProcessJArray(JArray jArray)
        {
            foreach (var item in jArray)
            {
                if (item is JObject)
                {
                    await ProcessJObject((JObject)item);
                }
            }
        }

        private async Task ProcessJObject(JObject jObject)
        {
            Interlocked.Increment(ref _count);
            int count = _count;
            Trace.WriteLine("Processing count : " + count);
            foreach(var metricsStorage in _metricsStorageList)
            {
                try
                {
                    await metricsStorage.AddPackageDownloadStatistics(jObject);
                    Trace.TraceInformation("Package Download Statistics processed successfully");
                }
                catch (Exception ex)
                {
                    // Catch all exceptions
                    Trace.TraceError(ex.ToString());
                }
            }
            Trace.WriteLine("Completed processing for count: " + count);
        }
    }
}
