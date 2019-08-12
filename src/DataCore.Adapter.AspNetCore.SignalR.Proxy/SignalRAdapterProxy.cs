﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via SignalR.
    /// </summary>
    public class SignalRAdapterProxy : AdapterBase, IAdapterProxy {

        /// <summary>
        /// The relative SignalR hub route.
        /// </summary>
        public const string HubRoute = "/signalr/data-core/v1.0";

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private AdapterDescriptorExtended _remoteDescriptor;

        /// <summary>
        /// Lock for accessing <see cref="_remoteDescriptor"/>.
        /// </summary>
        private readonly object _remoteDescriptorLock = new object();

        /// <inheritdoc/>
        public AdapterDescriptorExtended RemoteDescriptor {
            get {
                lock (_remoteDescriptorLock) {
                    return _remoteDescriptor;
                }
            }
            private set {
                lock (_remoteDescriptorLock) {
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
        /// <param name="descriptor">
        ///   The descriptor for the local proxy. This is not the descriptor for the remote 
        ///   adapter that the proxy will connect to.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        public SignalRAdapterProxy(AdapterDescriptor descriptor, SignalRAdapterProxyOptions options, ILogger<SignalRAdapterProxy> logger) 
            : base(descriptor, logger) {
            _remoteAdapterId = options?.AdapterId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _connectionFactory = options?.ConnectionFactory ?? throw new ArgumentException(Resources.Error_ConnectionFactoryIsRequired, nameof(options));
            _extensionFeatureFactory = options?.ExtensionFeatureFactory;
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
                    catch (Exception e) {
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
        protected override async Task StopAsync(bool disposing, CancellationToken cancellationToken) {
            if (_client.IsValueCreated) {
                if (disposing) {
                    await _client.Value.DisposeAsync().ConfigureAwait(false);
                }
                else {
                    var connection = await _client.Value.GetHubConnection(false, cancellationToken).ConfigureAwait(false);
                    await connection.StopAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}