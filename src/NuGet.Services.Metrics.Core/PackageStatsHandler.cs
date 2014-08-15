using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;


namespace NuGet.Services.Metrics.Core
{
    public class PackageStatsHandler
    {
        private readonly MetricsStorage _metricsStorage;
        private int _count = 0;
        private const string HTTPPost = "POST";
        private static readonly PathString Root = new PathString("/");
        private static readonly PathString DownloadEvent = new PathString("/DownloadEvent");

        public PackageStatsHandler(string connectionString, int commandTimeout, int commandRetries)
        {
            _metricsStorage = new DatabaseMetricsStorage(connectionString, commandTimeout, commandRetries);
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
            try
            {
                await _metricsStorage.AddPackageDownloadStatistics(jObject);
                Trace.TraceInformation("Package Download Statistics processed successfully");
            }
            catch (Exception ex)
            {
                // Catch all exceptions
                Trace.TraceError(ex.ToString());
            }
            Trace.WriteLine("Completed processing for count: " + count);
        }
    }
}
