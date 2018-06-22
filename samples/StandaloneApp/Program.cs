// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;

namespace StandaloneApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            JSRuntime.SetCurrentJSRuntime(new MonoWebAssemblyJSRuntime());
            CreateHostBuilder(args).Build().Start();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            BlazorBrowserHost.CreateDefaultBuilder()
                .UseBlazorStartup<Startup>();
    }
}
