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

        public PackageStatsHandler(string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Metrics.SqlServer is not present in the configuration");
            }
            _metricsStorage = new DatabaseMetricsStorage(connectionString);
        }

        public async Task Invoke(IOwinContext context)
        {
            if (context.Request.Path.StartsWithSegments(Root))
            {
                await context.Response.WriteAsync("NuGet Metrics Service: OK");
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else if (context.Request.Path.StartsWithSegments(DownloadEvent))
            {
                if (context.Request.Method != HTTPPost)
                {
                    await context.Response.WriteAsync("Only HTTP POST requests are accepted");
                    context.Response.Headers.Add("Allow", new[] { "POST" });
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }

                using (var streamReader = new StreamReader(context.Request.Body))
                {
                    try
                    {
                        var jsonString = await streamReader.ReadToEndAsync();
                        var jObject = JObject.Parse(jsonString);
                        Task.Run(() => Process(jObject));
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
                await context.Response.WriteAsync("Page is not found");
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }

        private async Task Process(JObject jObject)
        {
            Interlocked.Increment(ref _count);
            int count = _count;
            Trace.TraceInformation("Processing count : {0}", count, jObject.ToString());
            try
            {
                await _metricsStorage.AddPackageDownloadStatistics(jObject);
            }
            catch (Exception ex)
            {
                // Catch all exceptions
                Trace.TraceError(ex.ToString());
            }
            Trace.TraceInformation("Completed processing for {0}", count);
        }
    }
}
