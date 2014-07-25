using System;
using System.Diagnostics;
using Microsoft.Owin.Hosting;

namespace MetricsTestConsoleApp
{
    class Program
    {        
        static void Main(string[] args)
        {
            using (var app = WebApp.Start<ConsoleStartup>("http://localhost:12345"))
            {
                Trace.TraceInformation("Started a simple OWIN server");
                Console.ReadLine();
            }
        }
    }
}
