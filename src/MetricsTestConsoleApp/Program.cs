using NuGet.Services.Metrics;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsTestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var app = WebApp.Start<Startup>("http://localhost:12345"))
            {
                Trace.TraceInformation("Started a simple OWIN server");
                Console.ReadLine();
            }
        }
    }
}
