﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base class for simplifying implementation of the <see cref="IEventMessagePush"/> feature.
    /// </summary>
    public abstract class EventMessagePush : IEventMessagePush, IDisposable {

        /// <summary>
        /// The scheduler to use when running background tasks.
        /// </summary>
        protected IBackgroundTaskService Scheduler { get; }

        /// <summary>
        /// Logging.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Fires when then object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A cancellation token that will fire when the object is disposed.
        /// </summary>
        protected CancellationToken DisposedToken => _disposedTokenSource.Token;

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();

        /// <summary>
        /// For protecting access to <see cref="_subscriptions"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Indicates if the subscription manager currently holds any subscriptions.
        /// </summary>
        protected bool HasSubscriptions { get; private set; }

        /// <summary>
        /// Indicates if the subscription manager holds any active subscriptions. If your adapter uses 
        /// a forward-only cursor that you do not want to advance when only passive listeners are 
        /// attached to the adapter, you can use this property to identify if any active listeners are 
        /// attached.
        /// </summary>
        protected bool HasActiveSubscriptions { get; private set; }

        /// <summary>
        /// Channel that is used to publish new event messages. This is a single-consumer channel; the 
        /// consumer thread will then re-publish to subscribers as required.
        /// </summary>
        private readonly Channel<EventMessage> _masterChannel = Channel.CreateUnbounded<EventMessage>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true
        });


        /// <summary>
        /// Creates a new <see cref="EventMessagePush"/> object.
        /// </summary>
        /// <param name="scheduler">
        ///   The task scheduler to use when running background operations.
        /// </param>
        /// <param name="logger">
        ///   The logger for the subscription manager.
        /// </param>
        protected EventMessagePush(IBackgroundTaskService scheduler, ILogger logger) {
            Scheduler = scheduler ?? BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            Scheduler.QueueBackgroundWorkItem(PublishToSubscribers, _disposedTokenSource.Token);
        }


        /// <inheritdoc/>
        public IEventMessageSubscription Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType) {
            var subscription = new Subscription(context, this, subscriptionType);

            bool added;
            lock (_subscriptions) {
                added = _subscriptions.Add(subscription);
                if (added) {
                    HasSubscriptions = _subscriptions.Count > 0;
                    HasActiveSubscriptions = _subscriptions.Any(x => x.IsActive);
                }
            }

            if (added) {
                subscription.Start();
                OnSubscriptionAdded();
            }

            return subscription;
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        protected abstract void OnSubscriptionAdded();


        /// <summary>
        /// Invoked when a subscription is removed.
        /// </summary>
        /// <returns>
        ///   A task that will perform subscription-related activities.
        /// </returns>
        protected abstract void OnSubscriptionRemoved();


        /// <summary>
        /// Sends an event message to subscribers.
        /// </summary>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the value was published to subscribers.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> ValueReceived(EventMessage message, CancellationToken cancellationToken = default) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            try {
                using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token)) {
                    await _masterChannel.Writer.WaitToWriteAsync(ctSource.Token).ConfigureAwait(false);
                    return _masterChannel.Writer.TryWrite(message);
                }
            }
            catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested) {
                    // Cancellation token provided by the caller has fired; rethrow the exception.
                    throw;
                }

                // The stream manager is being disposed.
                return false;
            }
        }


        /// <summary>
        /// Notifies the <see cref="EventMessagePush"/> that a subscription was disposed.
        /// </summary>
        /// <param name="subscription">
        ///   The disposed subscription.
        /// </param>
        private void OnSubscriptionDisposed(Subscription subscription) {
            bool removed;
            lock (_subscriptions) {
                removed = _subscriptions.Remove(subscription);
                if (removed) {
                    HasSubscriptions = _subscriptions.Count > 0;
                    HasActiveSubscriptions = _subscriptions.Any(x => x.IsActive);
                }
            }

            if (removed && !_isDisposed && !_disposedTokenSource.IsCancellationRequested) {
                OnSubscriptionRemoved();
            }
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            DisposeInternal(true);

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~EventMessagePush() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="EventMessagePush"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        private void DisposeInternal(bool disposing) {
            try {
                Dispose(disposing);
            }
            finally {
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _masterChannel.Writer.TryComplete();
                _subscriptionsLock.EnterWriteLock();
                try {
                    foreach (var item in _subscriptions.ToArray()) {
                        item.Dispose();
                    }
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                }
                _subscriptionsLock.Dispose();
                _subscriptions.Clear();
            }
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="EventMessagePush"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        protected abstract void Dispose(bool disposing);


        /// <summary>
        /// Long-running task that sends tag values to subscribers whenever they are added to the 
        /// <see cref="_masterChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires
        /// </returns>
        private async Task PublishToSubscribers(CancellationToken cancellationToken) {
            try {
                while (await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (_masterChannel.Reader.TryRead(out var message)) {
                        if (message == null) {
                            continue;
                        }

                        Subscription[] subscribers;

                        _subscriptionsLock.EnterReadLock();
                        try {
                            subscribers = _subscriptions.ToArray();
                        }
                        finally {
                            _subscriptionsLock.ExitReadLock();
                        }

                        await Task.WhenAll(subscribers.Select(x => x.OnMessageReceived(message))).WithCancellation(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) {
                // Cancellation token fired
            }
            catch (ChannelClosedException) {
                // Channel was closed
            }
        }


        /// <summary>
        /// <see cref="IEventMessageSubscription"/> implementation.
        /// </summary>
        private class Subscription : EventMessageSubscriptionBase {

            /// <summary>
            /// The subscription manager that the subscription is attached to.
            /// </summary>
            private readonly EventMessagePush _subscriptionManager;

            /// <summary>
            /// Indicates if the subscription is an active or passive event listener.
            /// </summary>
            internal bool IsActive { get; }


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="context">
            ///   The adapter call context for the subscriber.
            /// </param>
            /// <param name="subscriptionManager">
            ///   The subscription manager that the subscription is attached to.
            /// </param>
            /// <param name="subscriptionType">
            ///   Indicates if the subscription is an active or passive event listener.
            /// </param>
            internal Subscription(
                IAdapterCallContext context,
                EventMessagePush subscriptionManager, 
                EventMessageSubscriptionType subscriptionType
            ) : base(context) {
                _subscriptionManager = subscriptionManager;
                IsActive = subscriptionType == EventMessageSubscriptionType.Active;
            }


            /// <summary>
            /// Writes an event message to the subscription channel.
            /// </summary>
            /// <param name="message">
            ///   The event message.
            /// </param>
            /// <returns>
            ///   A task that will write the message to the event channel.
            /// </returns>
            internal async Task OnMessageReceived(EventMessage message) {
                await ValueReceived(message, false, CancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            protected override void Dispose(bool disposing) {
                base.Dispose(disposing);
                if (disposing) {
                    _subscriptionManager.OnSubscriptionDisposed(this);
                }
            }

        }

    }
}
