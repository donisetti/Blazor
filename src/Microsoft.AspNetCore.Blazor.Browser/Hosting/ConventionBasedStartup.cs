// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    internal class ConventionBasedStartup : IBlazorStartup
    {
        private readonly object _instance;

        public ConventionBasedStartup(object instance)
        {
            _instance = instance;
        }

        public void Configure(IBlazorApplicationBuilder app)
        {
            try
            {
                var method = _instance.GetType().GetMethod(
                    "Configure",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(IBlazorApplicationBuilder), },
                    Array.Empty<ParameterModifier>());
                
                if (method != null)
                {
                    method.Invoke(_instance, new object[] { app });
                }
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }

                throw;
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                var method = _instance.GetType().GetMethod(
                    "ConfigureServices",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(IServiceCollection), },
                    Array.Empty<ParameterModifier>());
                
                if (method != null)
                {
                     method.Invoke(_instance, new object[] { services });
                }
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }

                throw;
            }
        }
    }
}