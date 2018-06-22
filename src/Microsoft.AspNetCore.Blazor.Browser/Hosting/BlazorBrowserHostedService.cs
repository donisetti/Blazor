// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    internal class BlazorBrowserHostedService : IHostedService
    {
        private readonly IHost _host;
        private readonly IBlazorStartup _startup;

        private IServiceScope _scope;
        private BrowserRenderer _renderer;

        public BlazorBrowserHostedService(IHost host, IBlazorStartup startup)
        {
            _host = host;
            _startup = startup;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
            _scope = scopeFactory.CreateScope();

            try
            {
                var builder = new BrowserBlazorApplicationBuilder(_scope.ServiceProvider);
                _startup.Configure(builder);

                _renderer = new BrowserRenderer(_scope.ServiceProvider);

                for (var i = 0; i < builder.Entries.Count; i++)
                {
                    var (componentType, domElementSelector) = builder.Entries[i];
                    _renderer.AddComponent(componentType, domElementSelector);
                }
            }
            catch
            {
                _scope.Dispose();
                _scope = null;

                if (_renderer != null)
                {
                    _renderer.Dispose();
                    _renderer = null;
                }

                throw;
            }


            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scope != null)
            {
                _scope.Dispose();
                _scope = null;
            }

            if (_renderer != null)
            {
                _renderer.Dispose();
                _renderer = null;
            }

            return Task.CompletedTask;
        }
    }
}
