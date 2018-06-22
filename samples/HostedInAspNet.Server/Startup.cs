// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using HostedInAspNet.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HostedInAspNet.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // This is an example of what a the startup code for a SERVER SIDE Blazor app might look like
            //
            // The signature of this lambda is identical to Startup::Configure method.
            app.UseServerSideBlazor<Client.Program>(blazor =>
            {
                blazor.AddComponent<Home>("app");
            });
        }
    }
}
