// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
