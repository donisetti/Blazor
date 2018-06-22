// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting
{
    internal class BlazorBrowserHostBuilder : IHostBuilder
    {
        private List<Action<IConfigurationBuilder>> _configureHostConfigActions = new List<Action<IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppConfigActions = new List<Action<HostBuilderContext, IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new List<Action<HostBuilderContext, IServiceCollection>>();
        private List<IConfigureContainerAdapter> _configureContainerActions = new List<IConfigureContainerAdapter>();
        private IServiceFactoryAdapter _serviceProviderFactory = new ServiceFactoryAdapter<IServiceCollection>(new DefaultServiceProviderFactory());
        private bool _hostBuilt;
        private IConfiguration _hostConfiguration;
        private IConfiguration _appConfiguration;
        private HostBuilderContext _hostBuilderContext;
        private IHostingEnvironment _hostingEnvironment;
        private IServiceProvider _appServices;

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Set up the configuration for the builder itself. This will be used to initialize the <see cref="IHostingEnvironment"/>
        /// for use later in the build process. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configureHostConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Sets up the configuration for the remainder of the build process and application. This can be called multiple times and
        /// the results will be additive. The results will be available at <see cref="HostBuilderContext.Configuration"/> for
        /// subsequent operations, as well as in <see cref="IHost.Services"/>.
        /// </summary>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configureServicesActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <typeparam name="TContainerBuilder"></typeparam>
        /// <param name="factory"></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            _serviceProviderFactory = new ServiceFactoryAdapter<TContainerBuilder>(factory ?? throw new ArgumentNullException(nameof(factory)));
            return this;
        }

        /// <summary>
        /// Enables configuring the instantiated dependency container. This can be called multiple times and
        /// the results will be additive.
        /// </summary>
        /// <typeparam name="TContainerBuilder"></typeparam>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _configureContainerActions.Add(new ConfigureContainerAdapter<TContainerBuilder>(configureDelegate
                ?? throw new ArgumentNullException(nameof(configureDelegate))));
            return this;
        }

        /// <summary>
        /// Run the given actions to initialize the host. This can only be called once.
        /// </summary>
        /// <returns>An initialized <see cref="IHost"/></returns>
        public IHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException("Build can only be called once.");
            }
            _hostBuilt = true;

            BuildHostConfiguration();
            CreateHostingEnvironment();
            CreateHostBuilderContext();
            BuildAppConfiguration();
            CreateServiceProvider();

            return _appServices.GetRequiredService<IHost>();
        }

        private void BuildHostConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            foreach (var buildAction in _configureHostConfigActions)
            {
                buildAction(configBuilder);
            }
            _hostConfiguration = configBuilder.Build();
        }

        private void CreateHostingEnvironment()
        {
            _hostingEnvironment = new HostingEnvironment()
            {
                ApplicationName = _hostConfiguration[HostDefaults.ApplicationKey],
                EnvironmentName = _hostConfiguration[HostDefaults.EnvironmentKey] ?? EnvironmentName.Production,
                ContentRootPath = null,
            };
            _hostingEnvironment.ContentRootFileProvider = new NullFileProvider();
        }

        private void CreateHostBuilderContext()
        {
            _hostBuilderContext = new HostBuilderContext(Properties)
            {
                HostingEnvironment = _hostingEnvironment,
                Configuration = _hostConfiguration
            };
        }

        private void BuildAppConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddConfiguration(_hostConfiguration);
            foreach (var buildAction in _configureAppConfigActions)
            {
                buildAction(_hostBuilderContext, configBuilder);
            }
            _appConfiguration = configBuilder.Build();
            _hostBuilderContext.Configuration = _appConfiguration;
        }

        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_hostBuilderContext);
            services.AddSingleton(_appConfiguration);
            services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();
            services.AddSingleton<IHostLifetime, BrowserLifetime>();
            services.AddSingleton<IHost, Host>();
            services.AddOptions();
            services.AddLogging();

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_hostBuilderContext, services);
            }

            var containerBuilder = _serviceProviderFactory.CreateBuilder(services);

            foreach (var containerAction in _configureContainerActions)
            {
                containerAction.ConfigureContainer(_hostBuilderContext, containerBuilder);
            }

            _appServices = _serviceProviderFactory.CreateServiceProvider(containerBuilder);

            if (_appServices == null)
            {
                throw new InvalidOperationException($"The IServiceProviderFactory returned a null IServiceProvider.");
            }
        }

        private interface IConfigureContainerAdapter
        {
            void ConfigureContainer(HostBuilderContext hostContext, object containerBuilder);
        }

        private class ConfigureContainerAdapter<TContainerBuilder> : IConfigureContainerAdapter
        {
            private Action<HostBuilderContext, TContainerBuilder> _action;

            public ConfigureContainerAdapter(Action<HostBuilderContext, TContainerBuilder> action)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public void ConfigureContainer(HostBuilderContext hostContext, object containerBuilder)
            {
                _action(hostContext, (TContainerBuilder)containerBuilder);
            }
        }

        private interface IServiceFactoryAdapter
        {
            object CreateBuilder(IServiceCollection services);

            IServiceProvider CreateServiceProvider(object containerBuilder);
        }

        private class ServiceFactoryAdapter<TContainerBuilder> : IServiceFactoryAdapter
        {
            private IServiceProviderFactory<TContainerBuilder> _serviceProviderFactory;

            public ServiceFactoryAdapter(IServiceProviderFactory<TContainerBuilder> serviceProviderFactory)
            {
                _serviceProviderFactory = serviceProviderFactory ?? throw new System.ArgumentNullException(nameof(serviceProviderFactory));
            }

            public object CreateBuilder(IServiceCollection services)
            {
                return _serviceProviderFactory.CreateBuilder(services);
            }

            public IServiceProvider CreateServiceProvider(object containerBuilder)
            {
                return _serviceProviderFactory.CreateServiceProvider((TContainerBuilder)containerBuilder);
            }
        }

        private class HostingEnvironment : IHostingEnvironment
        {
            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; }

            public string ContentRootPath { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }
        }

        private class ApplicationLifetime : IApplicationLifetime
        {
            private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();
            private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();
            private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();
            private readonly ILogger<ApplicationLifetime> _logger;

            public ApplicationLifetime(ILogger<ApplicationLifetime> logger)
            {
                _logger = logger;
            }

            /// <summary>
            /// Triggered when the application host has fully started and is about to wait
            /// for a graceful shutdown.
            /// </summary>
            public CancellationToken ApplicationStarted => _startedSource.Token;

            /// <summary>
            /// Triggered when the application host is performing a graceful shutdown.
            /// Request may still be in flight. Shutdown will block until this event completes.
            /// </summary>
            public CancellationToken ApplicationStopping => _stoppingSource.Token;

            /// <summary>
            /// Triggered when the application host is performing a graceful shutdown.
            /// All requests should be complete at this point. Shutdown will block
            /// until this event completes.
            /// </summary>
            public CancellationToken ApplicationStopped => _stoppedSource.Token;

            /// <summary>
            /// Signals the ApplicationStopping event and blocks until it completes.
            /// </summary>
            public void StopApplication()
            {
                // Lock on CTS to synchronize multiple calls to StopApplication. This guarantees that the first call 
                // to StopApplication and its callbacks run to completion before subsequent calls to StopApplication, 
                // which will no-op since the first call already requested cancellation, get a chance to execute.
                lock (_stoppingSource)
                {
                    try
                    {
                        ExecuteHandlers(_stoppingSource);
                    }
                    catch (Exception ex)
                    {
                        HostingLoggerExtensions.ApplicationError(
                            _logger,
                            LoggerEventIds.ApplicationStoppingException,
                            "An error occurred stopping the application",
                            ex);
                    }
                }
            }

            /// <summary>
            /// Signals the ApplicationStarted event and blocks until it completes.
            /// </summary>
            public void NotifyStarted()
            {
                try
                {
                    ExecuteHandlers(_startedSource);
                }
                catch (Exception ex)
                {
                    HostingLoggerExtensions.ApplicationError(
                        _logger,
                        LoggerEventIds.ApplicationStartupException,
                        "An error occurred starting the application",
                        ex);
                }
            }

            /// <summary>
            /// Signals the ApplicationStopped event and blocks until it completes.
            /// </summary>
            public void NotifyStopped()
            {
                try
                {
                    ExecuteHandlers(_stoppedSource);
                }
                catch (Exception ex)
                {
                    HostingLoggerExtensions.ApplicationError(
                        _logger,
                        LoggerEventIds.ApplicationStoppedException,
                        "An error occurred stopping the application",
                        ex);
                }
            }

            private void ExecuteHandlers(CancellationTokenSource cancel)
            {
                // Noop if this is already cancelled
                if (cancel.IsCancellationRequested)
                {
                    return;
                }

                // Run the cancellation token callbacks
                cancel.Cancel(throwOnFirstException: false);
            }
        }

        private class Host : IHost
        {
            private readonly ILogger<Host> _logger;
            private readonly IHostLifetime _hostLifetime;
            private readonly ApplicationLifetime _applicationLifetime;
            private readonly HostOptions _options;
            private IEnumerable<IHostedService> _hostedServices;

            public Host(
                IServiceProvider services,
                IApplicationLifetime applicationLifetime,
                ILogger<Host> logger,
                IHostLifetime hostLifetime,
                IOptions<HostOptions> options)
            {
                Services = services ?? throw new ArgumentNullException(nameof(services));
                _applicationLifetime = (applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime))) as ApplicationLifetime;
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
                _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            }

            public IServiceProvider Services { get; }

            public async Task StartAsync(CancellationToken cancellationToken = default)
            {
                HostingLoggerExtensions.Starting(_logger);

                await _hostLifetime.WaitForStartAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

                foreach (var hostedService in _hostedServices)
                {
                    // Fire IHostedService.Start
                    await hostedService.StartAsync(cancellationToken).ConfigureAwait(false);
                }

                // Fire IApplicationLifetime.Started
                _applicationLifetime?.NotifyStarted();

                HostingLoggerExtensions.Started(_logger);

                Console.WriteLine("Host started");
            }

            public async Task StopAsync(CancellationToken cancellationToken = default)
            {
                HostingLoggerExtensions.Stopping(_logger);

                using (var cts = new CancellationTokenSource(_options.ShutdownTimeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
                {
                    var token = linkedCts.Token;
                    // Trigger IApplicationLifetime.ApplicationStopping
                    _applicationLifetime?.StopApplication();

                    IList<Exception> exceptions = new List<Exception>();
                    if (_hostedServices != null) // Started?
                    {
                        foreach (var hostedService in _hostedServices.Reverse())
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                await hostedService.StopAsync(token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }

                    token.ThrowIfCancellationRequested();
                    await _hostLifetime.StopAsync(token);

                    // Fire IApplicationLifetime.Stopped
                    _applicationLifetime?.NotifyStopped();

                    if (exceptions.Count > 0)
                    {
                        var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                        HostingLoggerExtensions.StoppedWithException(_logger, ex);
                        throw ex;
                    }
                }

                HostingLoggerExtensions.Stopped(_logger);
            }

            public void Dispose()
            {
                (Services as IDisposable)?.Dispose();
            }
        }

        private class HostOptions
        {
            /// <summary>
            /// The default timeout for <see cref="IHost.StopAsync(System.Threading.CancellationToken)"/>.
            /// </summary>
            public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
        }

        private static class LoggerEventIds
        {
            public const int Starting = 1;
            public const int Started = 2;
            public const int Stopping = 3;
            public const int Stopped = 4;
            public const int StoppedWithException = 5;
            public const int ApplicationStartupException = 6;
            public const int ApplicationStoppingException = 7;
            public const int ApplicationStoppedException = 8;
        }

        private static class HostingLoggerExtensions
        {
            public static void ApplicationError(ILogger logger, EventId eventId, string message, Exception exception)
            {
                var reflectionTypeLoadException = exception as ReflectionTypeLoadException;
                if (reflectionTypeLoadException != null)
                {
                    foreach (var ex in reflectionTypeLoadException.LoaderExceptions)
                    {
                        message = message + Environment.NewLine + ex.Message;
                    }
                }

                logger.LogCritical(
                    eventId: eventId,
                    message: message,
                    exception: exception);
            }

            public static void Starting(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                       eventId: LoggerEventIds.Starting,
                       message: "Hosting starting");
                }
            }

            public static void Started(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        eventId: LoggerEventIds.Started,
                        message: "Hosting started");
                }
            }

            public static void Stopping(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        eventId: LoggerEventIds.Stopping,
                        message: "Hosting stopping");
                }
            }

            public static void Stopped(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        eventId: LoggerEventIds.Stopped,
                        message: "Hosting stopped");
                }
            }

            public static void StoppedWithException(ILogger logger, Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        eventId: LoggerEventIds.StoppedWithException,
                        exception: ex,
                        message: "Hosting shutdown exception");
                }
            }
        }
    }
}