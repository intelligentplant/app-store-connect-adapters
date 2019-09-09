﻿using System;
using System.Threading.Tasks;
using System.Threading;

#if NETSTANDARD2_1
using GrpcNet = Grpc.Net;
#endif

using GrpcCore = Grpc.Core;
using Microsoft.Extensions.Logging;
using DataCore.Adapter.Grpc.Client.Authentication;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via gRPC.
    /// </summary>
    public class GrpcAdapterProxy : AdapterBase<GrpcAdapterProxyOptions>, IAdapterProxy {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private Adapter.Common.Models.AdapterDescriptorExtended _remoteDescriptor;

        /// <summary>
        /// Lock for accessing <see cref="_remoteDescriptor"/>.
        /// </summary>
        private readonly object _remoteDescriptorLock = new object();

        /// <inheritdoc/>
        public Adapter.Common.Models.AdapterDescriptorExtended RemoteDescriptor {
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
        /// Gets per-call credentials.
        /// </summary>
        private readonly GetGrpcCallCredentials _getCallCredentials;

        /// <summary>
        /// gRPC channel (when using Grpc.Core for HTTP/2 support).
        /// </summary>
        private readonly GrpcCore.Channel _coreChannel;

#if NETSTANDARD2_1
        /// <summary>
        /// gRPC channe; (when using Grpc.Net.Client HTTP/2 support in .NET Core 3.0+).
        /// </summary>
        private readonly GrpcNet.Client.GrpcChannel _netChannel;
#endif

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory _extensionFeatureFactory;


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified <see cref="GrpcCore.Channel"/>.
        /// </summary>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory for the proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="loggerFactory"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        public GrpcAdapterProxy(GrpcCore.Channel channel, IOptions<GrpcAdapterProxyOptions> options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory) {
            _coreChannel = channel ?? throw new ArgumentNullException(nameof(channel));
            _remoteAdapterId = options?.Value?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _getCallCredentials = options?.Value?.GetCallCredentials;
            _extensionFeatureFactory = options?.Value?.ExtensionFeatureFactory;
        }

#if NETSTANDARD2_1

        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified <see cref="GrpcNet.Client.GrpcChannel"/>.
        /// </summary>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory for the proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="loggerFactory"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        public GrpcAdapterProxy(GrpcNet.Client.GrpcChannel channel, IOptions<GrpcAdapterProxyOptions> options, ILoggerFactory loggerFactory) 
            : base(options, loggerFactory) {

            _remoteAdapterId = options?.Value?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _netChannel = channel ?? throw new ArgumentNullException(nameof(channel));
            _getCallCredentials = options?.Value?.GetCallCredentials;
        }

#endif


        /// <summary>
        /// Creates a client for a gRPC service using the proxy's gRPC channel.
        /// </summary>
        /// <typeparam name="TClient">
        ///   The gRPC client type.
        /// </typeparam>
        /// <returns>
        ///   A new gRPC client instance.
        /// </returns>
        public TClient CreateClient<TClient>() where TClient : GrpcCore.ClientBase<TClient> {

#if NETSTANDARD2_1
            if (_netChannel != null) {
                return (TClient) Activator.CreateInstance(typeof(TClient), _netChannel);
            }
#endif

            return (TClient) Activator.CreateInstance(typeof(TClient), _coreChannel);
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
        private async Task Init(CancellationToken cancellationToken) {
            var client = CreateClient<AdaptersService.AdaptersServiceClient>();
            var callOptions = new GrpcCore.CallOptions(
                cancellationToken: cancellationToken,
                credentials: GetCallCredentials(null)
            );

            var response = await client.GetAdapterAsync(
                new GetAdapterRequest() {
                    AdapterId = _remoteAdapterId
                }, 
                callOptions
            ).ResponseAsync.ConfigureAwait(false);

            RemoteDescriptor = new Adapter.Common.Models.AdapterDescriptorExtended(
                response.Adapter.AdapterDescriptor.Id,
                response.Adapter.AdapterDescriptor.Name,
                response.Adapter.AdapterDescriptor.Description,
                response.Adapter.Features,
                response.Adapter.Extensions,
                response.Adapter.Properties
            );

            ProxyAdapterFeature.AddFeaturesToProxy(this, response.Adapter.Features);

            if (_extensionFeatureFactory != null) {
                foreach (var extensionFeature in response.Adapter.Extensions) {
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
        protected override async Task StopAsync(bool disposing, CancellationToken cancellationToken) {
            if (_coreChannel != null) {
                await _coreChannel.ShutdownAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Gets per-call gRPC credentials for the specified adapter call context.
        /// </summary>
        /// <param name="context">
        ///   The adapter call context.
        /// </param>
        /// <returns>
        ///   The call credentials to use. If <paramref name="context"/> is <see langword="null"/> 
        ///   or no <see cref="GrpcAdapterProxyOptions.GetCallCredentials"/> delegate was supplied 
        ///   when creating the proxy, the result will be <see langword="null"/>.
        /// </returns>
        public GrpcCore.CallCredentials GetCallCredentials(IAdapterCallContext context) {
            if (_getCallCredentials == null) {
                return null;
            }

            return GrpcCore.CallCredentials.FromInterceptor(new GrpcCore.AsyncAuthInterceptor(async (authContext, metadata) => {
                var credentials = await _getCallCredentials(context).ConfigureAwait(false);
                credentials.AddMetadataEntries(metadata);
            }));
        }

    }
}