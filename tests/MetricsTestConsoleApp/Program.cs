using System;
using System.Diagnostics;
using Microsoft.Owin.Hosting;

namespace MetricsTestConsoleApp
{
    class Program
    {        
        static void Main(string[] args)
        {
            string port = "12345";
            if(args.Length == 1)
            {
                port = args[0];
            }
            using (var app = WebApp.Start<ConsoleStartup>("http://localhost:" + port))
            {
                Trace.TraceInformation("Started a simple OWIN server");
                Console.ReadLine();
            }
        }
    }
}
