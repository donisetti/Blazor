// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Blazor.Browser.Http;
using Microsoft.AspNetCore.Blazor.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering browser-related services.
    /// </summary>
    public static class BlazorBrowserServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="HttpClient"/> configured to work in the browser.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddBrowserHttpClient(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(s =>
            {
                var uriHelper = s.GetRequiredService<IUriHelper>();
                return new HttpClient(new BrowserHttpMessageHandler())
                {
                    BaseAddress = new Uri(uriHelper.GetBaseUri())
                };
            });

            return services;
        }
    }
}
