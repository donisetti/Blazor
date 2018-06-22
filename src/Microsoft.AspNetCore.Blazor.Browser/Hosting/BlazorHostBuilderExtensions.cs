// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides Blazor-specific support for <see cref="IHost"/>.
    /// </summary>
    public static class BlazorHostBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="IHostBuilder"/> to use the provided startup class.
        /// </summary>
        /// <typeparam name="TStartup">A type that configures a Blazor application.</typeparam>
        /// <param name="builder">The <see cref="IHostBuilder"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseBlazorStartup<TStartup>(this IHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var startup = new ConventionBasedStartup(Activator.CreateInstance(typeof(TStartup)));

            builder.ConfigureServices(startup.ConfigureServices);
            builder.ConfigureServices(s => s.AddSingleton<IBlazorStartup>(startup));

            return builder;
        }
    }
}
