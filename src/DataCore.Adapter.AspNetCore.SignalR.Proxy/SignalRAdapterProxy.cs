﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using IntelligentPlant.BackgroundTasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via SignalR.
    /// </summary>
    public class SignalRAdapterProxy : AdapterBase<SignalRAdapterProxyOptions>, IAdapterProxy {

        /// <summary>
        /// Gets the logger for the proxy.
        /// </summary>
        internal new ILogger Logger {
            get { return base.Logger; }
        }

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// Information about the remote host.
        /// </summary>
        private HostInfo _remoteHostInfo;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private AdapterDescriptorExtended _remoteDescriptor;

        /// <summary>
        /// Lock for accessing <see cref="_remoteDescriptor"/>.
        /// </summary>
        private readonly object _remoteInfoLock = new object();

        /// <inheritdoc/>
        public HostInfo RemoteHostInfo {
            get {
                lock (_remoteInfoLock) {
                    return _remoteHostInfo;
                }
            }
            private set {
                lock (_remoteInfoLock) {
                    _remoteHostInfo = value;
                }
            }
        }

        /// <inheritdoc/>
        public AdapterDescriptorExtended RemoteDescriptor {
            get {
                lock (_remoteInfoLock) {
                    return _remoteDescriptor;
                }
            }
            private set {
                lock (_remoteInfoLock) {
                    _remoteDescriptor = value;
                }
            }
        }

        /// <summary>
        /// A factory delegate that can create hub connections.
        /// </summary>
        private readonly ConnectionFactory _connectionFactory;

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory _extensionFeatureFactory;

        /// <summary>
        /// The client used in standard adapter queries.
        /// </summary>
        private readonly Lazy<AdapterSignalRClient> _client;

        /// <summary>
        /// Additional hub connections created for extension features.
        /// </summary>
        private readonly ConcurrentDictionary<string, Lazy<Task<HubConnection>>> _extensionConnections = new ConcurrentDictionary<string, Lazy<Task<HubConnection>>>();


        /// <summary>
        /// Creates a new <see cref="SignalRAdapterProxy"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="taskScheduler">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        public SignalRAdapterProxy(
            string id,
            SignalRAdapterProxyOptions options, 
            IBackgroundTaskService taskScheduler, 
            ILogger<SignalRAdapterProxy> logger
        ) : base(
            id,
            options, 
            taskScheduler, 
            logger
        ) {
            _remoteAdapterId = Options?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _connectionFactory = Options?.ConnectionFactory ?? throw new ArgumentException(Resources.Error_ConnectionFactoryIsRequired, nameof(options));
            _extensionFeatureFactory = Options?.ExtensionFeatureFactory;
            _client = new Lazy<AdapterSignalRClient>(() => {
                return new AdapterSignalRClient(_connectionFactory.Invoke(null), true);
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }


        /// <summary>
        /// Gets the strongly-typed SignalR client for the proxy. This client can be used to query 
        /// standard adapter features (if supported by the remote adapter).
        /// </summary>
        /// <returns>
        ///   An <see cref="AdapterSignalRClient"/> object.
        /// </returns>
        public AdapterSignalRClient GetClient() {
            return _client.Value;
        }


        /// <summary>
        /// Gets or creates an active hub connection for use with an adapter extension feature.
        /// </summary>
        /// <param name="key">
        ///   The key for the extension hub. This cannot be <see langword="null"/> and will be 
        ///   vendor-specific.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The hub connection.
        /// </returns>
        /// <remarks>
        ///   The connection lifetime is managed by the proxy.
        /// </remarks>
        public Task<HubConnection> GetOrCreateExtensionHubConnection(string key, CancellationToken cancellationToken = default) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            return _extensionConnections.GetOrAdd(key, k => new Lazy<Task<HubConnection>>(() => Task.Run(async () => {
                var conn = _connectionFactory.Invoke(k);
                await conn.StartAsync(StopToken).ConfigureAwait(false);
                return conn;
            }), LazyThreadSafetyMode.ExecutionAndPublication)).Value.WithCancellation(cancellationToken);
        }


        /// <summary>
        /// Initialises the proxy.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform the initialisation.
        /// </returns>
        private async Task Init(CancellationToken cancellationToken = default) {
            var client = GetClient();
            RemoteHostInfo = await client.HostInfo.GetHostInfoAsync(cancellationToken).ConfigureAwait(false);
            var descriptor = await client.Adapters.GetAdapterAsync(_remoteAdapterId, cancellationToken).ConfigureAwait(false);

            RemoteDescriptor = descriptor;

            ProxyAdapterFeature.AddFeaturesToProxy(this, descriptor.Features);

            if (_extensionFeatureFactory != null) {
                foreach (var extensionFeature in descriptor.Extensions) {
                    if (string.IsNullOrWhiteSpace(extensionFeature)) {
                        continue;
                    }

                    try {
                        var impl = _extensionFeatureFactory.Invoke(extensionFeature, this);
                        if (impl == null) {
                            Logger.LogWarning(Resources.Log_NoExtensionImplementationAvailable, extensionFeature);
                            continue;
                        }
                        AddFeatures(impl, addStandardFeatures: false);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Logger.LogError(e, Resources.Log_ExtensionFeatureRegistrationError, extensionFeature);
                    }
                }
            }
        }


        /// <inheritdoc/>
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await Init(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async Task StopAsync(CancellationToken cancellationToken) {
            if (_client.IsValueCreated) {
                var connection = await _client.Value.GetHubConnection(false, cancellationToken).ConfigureAwait(false);
                await connection.StopAsync(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Checks the health of the remote adapter.
        /// </summary>
        /// <param name="context">
        ///   The context for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the health check result.
        /// </returns>
        private async Task<HealthCheckResult> CheckRemoteHealthAsync(
            IAdapterCallContext context,
            CancellationToken cancellationToken
        ) {
            if (!RemoteDescriptor.HasFeature<IHealthCheck>()) {
                return HealthCheckResult.Healthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter, 
                    Resources.HealthCheck_RemoteAdapterHealthNotSupported
                );
            }

            try {
                var result = await _client
                    .Value
                    .Adapters
                    .CheckAdapterHealthAsync(RemoteDescriptor.Id, cancellationToken)
                    .ConfigureAwait(false);

                return new HealthCheckResult(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    result.Status,
                    result.Description,
                    result.Error,
                    result.Data,
                    result.InnerResults
                );
            }
            catch (Exception e) {
                return HealthCheckResult.Unhealthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    error: e.Message
                );
            }
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var results = new List<HealthCheckResult>(await base.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false));
            if (!IsRunning) {
                return results;
            }

            if (_client.IsValueCreated) {
                var hubConnection = await _client.Value.GetHubConnection(false, cancellationToken).ConfigureAwait(false);
                var state = hubConnection.State;
               
                switch (state) {
                    case HubConnectionState.Connected:
                        results.Add(
                            HealthCheckResult.Composite(
                                Resources.HealthCheck_DisplayName_Connection,
                                new[] {
                                    await CheckRemoteHealthAsync(context, cancellationToken).ConfigureAwait(false)
                                },
                                string.Format(context?.CultureInfo, Resources.HealthCheck_HubConnectionStatusDescription, state.ToString())
                            )
                        );
                        break;
                    case HubConnectionState.Disconnected:
                        results.Add(HealthCheckResult.Unhealthy(
                            Resources.HealthCheck_DisplayName_Connection,
                            string.Format(context?.CultureInfo, Resources.HealthCheck_HubConnectionStatusDescriptionNoInnerResults, state.ToString())
                        ));
                        break;
                    default:
                        results.Add(HealthCheckResult.Degraded(
                            Resources.HealthCheck_DisplayName_Connection,
                            string.Format(context?.CultureInfo, Resources.HealthCheck_HubConnectionStatusDescriptionNoInnerResults, state.ToString())
                        ));
                        break;
                }
            }

            foreach (var item in _extensionConnections) {
                var healthCheckName = string.Format(context?.CultureInfo, Resources.HeathCheck_DisplayName_ExtensionConnection, item.Key);
                var format = Resources.HealthCheck_ExtensionHubConnectionStatusDescription;

                if (!item.Value.IsValueCreated || !item.Value.Value.IsCompleted) {
                    var description = string.Format(
                        context?.CultureInfo, 
                        format, 
                        Resources.HealthCheck_UnknownConnectionState
                    );
                    results.Add(HealthCheckResult.Degraded(healthCheckName, description));
                    continue;
                }

                try {
                    var hubConnection = await item.Value.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

                    var state = hubConnection.State;
                    var description = string.Format(context?.CultureInfo, format, state.ToString());

                    switch (state) {
                        case HubConnectionState.Connected:
                            results.Add(HealthCheckResult.Healthy(healthCheckName, description));
                            break;
                        case HubConnectionState.Disconnected:
                            results.Add(HealthCheckResult.Unhealthy(healthCheckName, description));
                            break;
                        default:
                            results.Add(HealthCheckResult.Degraded(healthCheckName, description));
                            break;
                    }
                }
                catch (Exception e) {
                    var description = string.Format(context?.CultureInfo, format, Resources.HealthCheck_UnknownConnectionState);
                    results.Add(HealthCheckResult.Unhealthy(healthCheckName, description, e.Message));
                }
            }

            return results;
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing && _client.IsValueCreated) {
                _client.Value.Dispose();
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask DisposeAsync(bool disposing) {
            await base.DisposeAsync(disposing).ConfigureAwait(false);
            if (disposing && _client.IsValueCreated) {
                await _client.Value.DisposeAsync().ConfigureAwait(false);
            }
        }

    }
}
