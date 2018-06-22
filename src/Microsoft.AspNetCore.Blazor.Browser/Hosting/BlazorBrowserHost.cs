// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    /// <summary>
    /// Used to to create instances a Blazor host builder for a Browser application.
    /// </summary>
    public static class BlazorBrowserHost
    {
        /// <summary>
        /// Creates a an instance of <see cref="IHostBuilder"/>.
        /// </summary>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateDefaultBuilder()
        {
            var builder = new BlazorBrowserHostBuilder();

            builder.ConfigureServices((c, services) =>
            {
                // Configuration of the 'default' Blazor services available
                // in the environment goes here.
                services.AddSingleton<IUriHelper, BrowserUriHelper>();

                // This is the thing that bootstraps the Blazor application.
                services.AddHostedService<BlazorBrowserHostedService>();
            });

            return builder;
        }
    }
}
