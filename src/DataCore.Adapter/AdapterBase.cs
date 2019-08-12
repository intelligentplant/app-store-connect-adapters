﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class that adapter implementations can inherit from.
    /// </summary>
    public abstract class AdapterBase : IAdapter, IAsyncDisposable {

        /// <summary>
        /// Indicates if the adapter has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Indicates if the adapter is being disposed.
        /// </summary>
        private bool _isDisposing;

        /// <summary>
        /// Logging.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Indicates if the adapter has been started.
        /// </summary>
        protected bool IsStarted { get; private set; }

        /// <summary>
        /// Ensures that only one startup attempt can occur at a time.
        /// </summary>
        private readonly SemaphoreSlim _startupLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Fires when <see cref="IAdapter.StopAsync(CancellationToken)"/> is called.
        /// </summary>
        private CancellationTokenSource _stopTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Gets a cancellation token that will fire when the adapter is stopped.
        /// </summary>
        protected CancellationToken StopToken => _stopTokenSource.Token;

        /// <summary>
        /// The adapter descriptor.
        /// </summary>
        private AdapterDescriptor _descriptor;

        /// <summary>
        /// The adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection();

        /// <inheritdoc/>
        AdapterDescriptor IAdapter.Descriptor {
            get {
                CheckDisposed();
                lock (_descriptor) {
                    return _descriptor;
                }
            }
        }

        /// <inheritdoc/>
        IAdapterFeaturesCollection IAdapter.Features {
            get {
                CheckDisposed();
                return _features;
            }
        }

        /// <summary>
        /// Adapter properties.
        /// </summary>
        private ConcurrentDictionary<string, string> _properties = new ConcurrentDictionary<string, string>();

        /// <inheritdoc/>
        IDictionary<string, string> IAdapter.Properties {
            get {
                CheckDisposed();
                return new ReadOnlyDictionary<string, string>(_properties);
            }
        }


        /// <summary>
        /// Creates a new <see cref="Adapter"/> object.
        /// </summary>
        /// <param name="descriptor">
        ///   The adapter descriptor.
        /// </param>
        /// <param name="logger">
        ///   The logger for the adapter. If a <see langword="null"/> value is provided, 
        ///   <see cref="Logger"/> will be set to an instance of <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        protected AdapterBase(AdapterDescriptor descriptor, ILogger logger) {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }


        /// <inheritdoc/>
        async Task IAdapter.StartAsync(CancellationToken cancellationToken) {
            CheckDisposed();
            await _startupLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (IsStarted) {
                    throw new InvalidOperationException(Resources.Error_AdapterIsAlreadyStarted);
                }
                if (StopToken.IsCancellationRequested) {
                    throw new InvalidOperationException(Resources.Error_AdapterIsStopping);
                }

                try {
                    Logger.LogInformation(Resources.Log_StartingAdapter, _descriptor.Id);
                    using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopTokenSource.Token)) {
                        await StartAsync(ctSource.Token).ConfigureAwait(false);
                    }
                    IsStarted = true;
                    Logger.LogInformation(Resources.Log_StartedAdapter, _descriptor.Id);
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_AdapterStartupError, _descriptor.Id);
                    throw;
                }
            }
            finally {
                _startupLock.Release();
            }
        }


        /// <inheritdoc/>
        async Task IAdapter.StopAsync(CancellationToken cancellationToken) {
            CheckDisposed();
            CheckStarted();

            try {
                Logger.LogInformation(Resources.Log_StoppingAdapter, _descriptor.Id);
                _stopTokenSource.Cancel();
                await StopAsync(false, cancellationToken).ConfigureAwait(false);
                Logger.LogInformation(Resources.Log_StoppedAdapter, _descriptor.Id);
            }
            catch (Exception e) {
                Logger.LogError(e, Resources.Log_AdapterStopError, _descriptor.Id);
                throw;
            }
            finally {
                _stopTokenSource = new CancellationTokenSource();
                IsStarted = false;
            }
        }


        /// <summary>
        /// Disposes of the adapter.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that represents the dispose operation.
        /// </returns>
        async ValueTask IAsyncDisposable.DisposeAsync() {
            if (_isDisposed || _isDisposing) {
                return;
            }
            try {
                _isDisposing = true;
                Logger.LogInformation(Resources.Log_DisposingAdapter, _descriptor.Id);
                _stopTokenSource.Dispose();
                await StopAsync(true, default).ConfigureAwait(false);
            }
            finally {
                await _features.DisposeAsync().ConfigureAwait(false);
                _properties.Clear();
                _isDisposed = true;
                _isDisposing = false;
                IsStarted = false;
            }
        }


        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the adapter has not been started.
        /// </summary>
        public void CheckStarted() {
            if (!IsStarted) {
                throw new InvalidOperationException(Resources.Error_AdapterIsNotStarted);
            }
        }


        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the adapter has been disposed.
        /// </summary>
        public void CheckDisposed() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }


        /// <summary>
        /// Starts the adapter.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that represents the start operation.
        /// </returns>
        protected abstract Task StartAsync(CancellationToken cancellationToken);


        /// <summary>
        /// Stops the adapter.
        /// </summary>
        /// <param name="disposing">
        ///   A flag that indicates if the adapter is being stopped because it is being disposed, 
        ///   or if <see cref="IAdapter.StopAsync(CancellationToken)"/> was explicitly called.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that represents the stop operation.
        /// </returns>
        protected abstract Task StopAsync(bool disposing, CancellationToken cancellationToken);


        /// <summary>
        /// Updates the adapter descriptor.
        /// </summary>
        /// <param name="descriptor">
        ///   The updated descriptor.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        protected void UpdateDescriptor(AdapterDescriptor descriptor) {
            CheckDisposed();
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            lock (_descriptor) {
                _descriptor = descriptor;
            }
        }


        /// <summary>
        /// Adds a feature to the adapter.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature. This must be an interface derived from <see cref="IAdapterFeature"/>.
        /// </typeparam>
        /// <typeparam name="TFeatureImpl">
        ///   The feature implementation type. This must be a concrete class that implements 
        ///   <typeparamref name="TFeature"/>.
        /// </typeparam>
        /// <param name="feature">
        ///   The implementation object.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not an interface, or it does not interit from 
        ///   <see cref="IAdapterFeature"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <typeparamref name="TFeature"/> has already been registered.
        /// </exception>
        public void AddFeature<TFeature, TFeatureImpl>(TFeatureImpl feature) where TFeature : IAdapterFeature where TFeatureImpl : class, TFeature {
            CheckDisposed();
            _features.Add<TFeature, TFeatureImpl>(feature ?? throw new ArgumentNullException(nameof(feature)));
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <param name="featureType">
        ///   The feature type. This must be an interface derived from <see cref="IAdapterFeature"/>.
        /// </param>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="featureType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="featureType"/> is not an adapter feature type.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="feature"/> is not an instance of <paramref name="featureType"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <paramref name="featureType"/> has already been registered.
        /// </exception>
        public void AddFeature(Type featureType, object feature) {
            CheckDisposed();
            _features.Add(featureType, feature);
        }


        /// <summary>
        /// Adds all adapter features implemented by the specified feature provider.
        /// </summary>
        /// <param name="provider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        /// <param name="addStandardFeatures">
        ///   Specifies if standard adapter feature implementations should be added to the 
        ///   collection. Standard feature types can be obtained by calling 
        ///   <see cref="TypeExtensions.GetStandardAdapterFeatureTypes"/>.
        /// </param>
        /// <param name="addExtensionFeatures">
        ///   Specifies if extension adapter feature implementations should be added to the 
        ///   collection. Extension features must derive from <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <remarks>
        ///   All interfaces implemented by the <paramref name="provider"/> that extend 
        ///   <see cref="IAdapterFeature"/> will be registered with the <see cref="Adapter"/> 
        ///   (assuming that they meet the <paramref name="addStandardFeatures"/> and 
        ///   <paramref name="addExtensionFeatures"/> constraints).
        /// </remarks>
        public void AddFeatures(object provider, bool addStandardFeatures = true, bool addExtensionFeatures = true) {
            CheckDisposed();
            _features.AddFromProvider(provider ?? throw new ArgumentNullException(nameof(provider)), addStandardFeatures, addExtensionFeatures);
        }


        /// <summary>
        /// Removes a registered feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type to remove.
        /// </typeparam>
        /// <returns>
        ///   <see langword="true"/> if the feature was removed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        public bool RemoveFeature<TFeature>() where TFeature : IAdapterFeature {
            CheckDisposed();
            return _features.Remove<TFeature>();
        }


        /// <summary>
        /// Removes all features.
        /// </summary>
        public void RemoveAllFeatures() {
            CheckDisposed();
            _features.Clear();
        }


        /// <summary>
        /// Adds a bespoke adapter property.
        /// </summary>
        /// <param name="key">
        ///   The property key.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> i <see langword="null"/>.
        /// </exception>
        protected void AddProperty(string key, string value) {
            CheckDisposed();
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            _properties[key] = value;
        }


        /// <summary>
        /// Removes a bespoke adapter property.
        /// </summary>
        /// <param name="key">
        ///   The property key.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the property was removed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> i <see langword="null"/>.
        /// </exception>
        protected bool RemoveProperty(string key) {
            CheckDisposed();
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return _properties.TryRemove(key, out var _);
        }


        /// <summary>
        /// Removes all bespoke adapter properties.
        /// </summary>
        protected void RemoveAllProperties() {
            _properties.Clear();
        }

    }
}