﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Diagnostics;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Default implementation of the <see cref="IEventMessagePush"/> feature.
    /// </summary>
    /// <remarks>
    ///   This implementation pushes ephemeral event messages to subscribers. To maintain an 
    ///   in-memory buffer of historical events, use <see cref="InMemoryEventMessageStore"/>.
    /// </remarks>
    public class EventMessagePush : IEventMessagePush, IFeatureHealthCheck, IDisposable {

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
        /// Feature options.
        /// </summary>
        private readonly EventMessagePushOptions _options;

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new ConcurrentDictionary<string, Subscription>(StringComparer.Ordinal);

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
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// Emits all messages that are published to the internal master channel.
        /// </summary>
        public event Action<EventMessage> Publish;


        /// <summary>
        /// Creates a new <see cref="EventMessagePush"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="scheduler">
        ///   The task scheduler to use when running background operations.
        /// </param>
        /// <param name="logger">
        ///   The logger for the subscription manager.
        /// </param>
        public EventMessagePush(EventMessagePushOptions options, IBackgroundTaskService scheduler, ILogger logger) {
            _options = options ?? new EventMessagePushOptions();
            Scheduler = scheduler ?? BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            Scheduler.QueueBackgroundWorkItem(PublishToSubscribers, _disposedTokenSource.Token);
        }


        /// <inheritdoc/>
        public async Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            Subscription subscription;

            var subscriptionId = Guid.NewGuid().ToString();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(DisposedToken, cancellationToken);
            subscription = new Subscription(
                subscriptionId,
                ChannelExtensions.CreateEventMessageChannel<EventMessage>(BoundedChannelFullMode.DropOldest), 
                request.SubscriptionType, 
                cts,
                () => OnSubscriptionCancelled(subscriptionId)
            );
            _subscriptions[subscriptionId] = subscription;

            HasSubscriptions = _subscriptions.Count > 0;
            HasActiveSubscriptions = _subscriptions.Values.Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
            OnSubscriptionAdded();

            return subscription.Channel.Reader;
        }



        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        protected virtual void OnSubscriptionAdded() { }


        /// <summary>
        /// Invoked when a subscription is removed.
        /// </summary>
        /// <returns>
        ///   A task that will perform subscription-related activities.
        /// </returns>
        protected virtual void OnSubscriptionRemoved() { }


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
        /// Notifies the <see cref="EventMessagePush"/> that a subscription was cancelled.
        /// </summary>
        /// <param name="id">
        ///   The cancelled subscription.
        /// </param>
        private void OnSubscriptionCancelled(string id) {
            if (_isDisposed) {
                return;
            }

            if (_subscriptions.TryRemove(id, out var subscription)) {
                subscription.Dispose();
                HasSubscriptions = _subscriptions.Count > 0;
                HasActiveSubscriptions = _subscriptions.Values.Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
                OnSubscriptionRemoved();
            }
        }


        /// <inheritdoc/>
        public Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var subscriptions = _subscriptions.Values.ToArray();

            var result = HealthCheckResult.Healthy(nameof(EventMessagePush), data: new Dictionary<string, string>() {
                { Resources.HealthChecks_Data_ActiveSubscriberCount, subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Active).ToString(context?.CultureInfo) },
                { Resources.HealthChecks_Data_PassiveSubscriberCount, subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Passive).ToString(context?.CultureInfo) }
            });

            return Task.FromResult(result);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
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
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _masterChannel.Writer.TryComplete();

                foreach (var item in _subscriptions.Values.ToArray()) {
                    item.Dispose();
                }

                _subscriptions.Clear();
            }

            _isDisposed = true;
        }


        /// <summary>
        /// Long-running task that sends event messages to subscribers whenever they are added to 
        /// the <see cref="_masterChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ensures recovery from errors occurring when publishing messages to subscribers")]
        private async Task PublishToSubscribers(CancellationToken cancellationToken) {
            try {
                while (await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (_masterChannel.Reader.TryRead(out var message)) {
                        if (message == null) {
                            continue;
                        }

                        var subscribers = _subscriptions.Values.ToArray();
                        foreach (var subscriber in subscribers) {
                            if (cancellationToken.IsCancellationRequested) {
                                break;
                            }
                            subscriber.Channel.Writer.TryWrite(message);
                        }
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


        private struct Subscription : IDisposable {

            public string Id { get; }

            public Channel<EventMessage> Channel { get; }

            public EventMessageSubscriptionType SubscriptionType { get; }

            public CancellationTokenSource CancellationTokenSource { get; }

            private readonly IDisposable _ctRegistration;


            public Subscription(string id, Channel<EventMessage> channel, EventMessageSubscriptionType subscriptionType, CancellationTokenSource cancellationTokenSource, Action onDisposed) {
                Id = id;
                Channel = channel;
                SubscriptionType = subscriptionType;
                CancellationTokenSource = cancellationTokenSource;
                _ctRegistration = CancellationTokenSource.Token.Register(onDisposed);
            }


            public void Dispose() {
                Channel.Writer.TryComplete();
                _ctRegistration.Dispose();
                CancellationTokenSource?.Cancel();
                CancellationTokenSource?.Dispose();
            }
        }

    }


    /// <summary>
    /// Options for <see cref="EventMessagePushOptions"/>
    /// </summary>
    public class EventMessagePushOptions {

        /// <summary>
        /// The adapter name to use when creating subscription IDs.
        /// </summary>
        public string AdapterId { get; set; }

    }
}
