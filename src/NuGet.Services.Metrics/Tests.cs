using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Metrics
{
    class Tests
    {
        static string IdKey = "id";
        static string VersionKey = "version";
        static string IPAddressKey = "ipAddress";
        static string UserAgentKey = "userAgent";
        static string OperationKey = "operation";
        static string DependentPackageKey = "dependentPackage";
        static string ProjectGuidsKey = "projectGuids";

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

        async Task<HttpStatusCode> Post(string id, string version, string ipAddress, string userAgent, string operation, string dependentPackage, string projectGuids, string uri)
        {
            Console.WriteLine("Posting");
            var jObject = GetJObject(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids);

            using (var client = new HttpClient())
            {
                Console.WriteLine("Using HttpClient");
                Console.WriteLine("Requesting response...");
                var response = await client.PostAsync(uri, new StringContent(jObject.ToString(), Encoding.Default, "application/json"));
                Console.WriteLine(response.StatusCode);
                Console.WriteLine("Received response");
                return response.StatusCode;
            }
        }

        async Task RunScenario(string scenario, HttpStatusCode expected, string id, string version, string ipAddress, string userAgent, string operation, string dependentPackage, string projectGuids, string uri)
        {
            Console.WriteLine("Running scenario: " + scenario);
            Console.WriteLine("On " + uri);
            if (expected != await Post(id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, uri))
            {
                throw new InvalidOperationException("Response for " + scenario + " is not the expected status code" + expected);
            }
            Console.WriteLine("Successfully ran scenario: " + scenario);
        }

        async Task RunTestsForHost(string host)
        {
            var rootEndpoint = @"http://" + host + "/";
            var nonExistentEndpoint = @"http://" + host + "/DoesNotExist";
            var downloadEventEndpoint = @"http://" + host + "/DownloadEvent";

            var id = "DependentPackage";
            var version = "3.0.0";
            string ipAddress = null;
            string userAgent = "Console Test";
            string operation = "Install";
            string dependentPackage = null;
            string projectGuids = null;

            // Root endpoint
            var scenario = "Root endpoint";
            await RunScenario(scenario, HttpStatusCode.OK, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, rootEndpoint);

            // Non existent endpoint
            scenario = "Non existent endpoint";
            await RunScenario(scenario, HttpStatusCode.NotFound, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, nonExistentEndpoint);

            // Non existent endpoint
            scenario = "Download event endpoint";
            await RunScenario(scenario, HttpStatusCode.Accepted, id, version, ipAddress, userAgent, operation, dependentPackage, projectGuids, downloadEventEndpoint);
        }

        async Task RunAllTests()
        {
            // LOCALHOST:12345
            //var host = @"localhost:12345";

            // INT
            var host = @"nuget-int-0-metrics.cloudapp.net";

            // PROD
            //var hostName = @"nuget-prod-0-metrics.cloudapp.net";
            await RunTestsForHost(host);
        }

        void Main()
        {
            RunAllTests().Wait();
        }

        // Define other methods and classes here
    }
}
