using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.Metrics.Core.Tests
{
    public class MetricsTests
    {
        const string IdKey = "id";
        const string VersionKey = "version";
        const string IPAddressKey = "ipAddress";
        const string UserAgentKey = "userAgent";
        const string OperationKey = "operation";
        const string DependentPackageKey = "dependentPackage";
        const string ProjectGuidsKey = "projectGuids";
        const string MetricsService = @"http://localhost:12345";
        private Uri ServiceRoot = null;

        public MetricsTests()
        {
            var root = Environment.GetEnvironmentVariable("NUGET_TEST_SERVICEROOT");
            if(String.IsNullOrEmpty(root))
            {
                root = MetricsService;
            }

            ServiceRoot = new Uri(root);
        }

        JObject GetJObject(string id, string version, string ipAddress, string userAgent, string operation, string dependentPackage, string projectGuids)
        {
            var jObject = new JObject();
            jObject.Add(IdKey, id);
            jObject.Add(VersionKey, version);
            if (!String.IsNullOrEmpty(ipAddress)) jObject.Add(IPAddressKey, ipAddress);
            if (!String.IsNullOrEmpty(userAgent)) jObject.Add(UserAgentKey, userAgent);
            if (!String.IsNullOrEmpty(operation)) jObject.Add(OperationKey, operation);
            if (!String.IsNullOrEmpty(dependentPackage)) jObject.Add(DependentPackageKey, dependentPackage);
            if (!String.IsNullOrEmpty(projectGuids)) jObject.Add(ProjectGuidsKey, projectGuids);

            return jObject;
        }

        async Task<HttpStatusCode> RunScenario(string id, string version, string ipAddress, string userAgent, string operation, string dependentPackage, string projectGuids, string uri,
            bool nonJSONRequest = false, bool nonPostRequest = false)
        {
            var jObject = GetJObject(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids);
            Console.WriteLine("Requesting response...");
            HttpResponseMessage response;
            using (var httpClient = new HttpClient()
                                    {
                                        BaseAddress = ServiceRoot
                                    })
            {
                if (nonPostRequest)
                {
                    response = await httpClient.GetAsync(uri);
                }
                else
                {
                    response = await httpClient.PostAsync(uri, new StringContent(nonJSONRequest ? "blah" : jObject.ToString(), Encoding.Default, "application/json"));
                } 
            }
            Console.WriteLine(response.StatusCode);
            Console.WriteLine("Received response");
            return response.StatusCode;
        }

        [Theory]
        [InlineData("Root endpoint", "/", HttpStatusCode.OK, false, false)]
        [InlineData("Non existent endpoint", "/DoesNotExist", HttpStatusCode.NotFound, false, false)]
        [InlineData("Request is not JSON", "/DownloadEvent", HttpStatusCode.BadRequest, true, false)]
        [InlineData("Request is not a POST", "/DownloadEvent", HttpStatusCode.BadRequest, false, true)]
        [InlineData("Valid DownloadEvent Request", "/DownloadEvent", HttpStatusCode.Accepted, false, false)]
        public async Task RunScenario(string scenario, string uri, HttpStatusCode expected, bool nonJSONRequest, bool nonPostRequest)
        {
            var id = "EntityFramework";
            var version = "5.0.0";
            string ipAddress = null;
            string userAgent = "Functional Tests Runner";
            string operation = "Install";
            string dependentPackage = null;
            string projectGuids = null;

            Console.WriteLine("Running scenario {0} on host: {1}", scenario, ServiceRoot.AbsoluteUri);
            Assert.Equal(expected, await RunScenario(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, uri, nonJSONRequest: nonJSONRequest, nonPostRequest: nonPostRequest)) ;
        }
    }
}
