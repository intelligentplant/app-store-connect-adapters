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
    public class EventMessagePush : SubscriptionManager<EventMessagePushOptions, string, EventMessage, EventSubscriptionChannel>, IEventMessagePush {

        /// <summary>
        /// Indicates if the subscription manager holds any active subscriptions. If your adapter uses 
        /// a forward-only cursor that you do not want to advance when only passive listeners are 
        /// attached to the adapter, you can use this property to identify if any active listeners are 
        /// attached.
        /// </summary>
        protected bool HasActiveSubscriptions { get; private set; }


        /// <summary>
        /// Creates a new <see cref="EventMessagePush"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        public EventMessagePush(EventMessagePushOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger? logger) 
            : base(options, backgroundTaskService, logger) { }


        /// <inheritdoc/>
        protected override EventSubscriptionChannel CreateSubscriptionChannel(
            IAdapterCallContext context, 
            int id, 
            int channelCapacity,
            CancellationToken[] cancellationTokens, 
            Action cleanup, 
            object? state
        ) {
            var request = (CreateEventMessageSubscriptionRequest) state!;
            return new EventSubscriptionChannel(
                id, 
                context, 
                BackgroundTaskService, 
                request?.SubscriptionType ?? EventMessageSubscriptionType.Active, 
                TimeSpan.Zero, 
                cancellationTokens, 
                cleanup, 
                channelCapacity
            );
        }


        /// <inheritdoc/>
        public Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            ValidationExtensions.ValidateObject(request);

            var subscription = CreateSubscription(context, request, cancellationToken);
            return Task.FromResult(subscription.Reader);
        }


        /// <inheritdoc/>
        protected override void OnSubscriptionAdded(EventSubscriptionChannel subscription) {
            HasActiveSubscriptions = HasSubscriptions && GetSubscriptions().Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
        }


        /// <inheritdoc/>
        protected override void OnSubscriptionCancelled(EventSubscriptionChannel subscription) { 
            HasActiveSubscriptions = HasSubscriptions && GetSubscriptions().Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
        }


        /// <inheritdoc/>
        protected override IDictionary<string, string> GetHealthCheckProperties(IAdapterCallContext context) {
            var result = base.GetHealthCheckProperties(context);

            var subscriptions = GetSubscriptions();

            result[Resources.HealthChecks_Data_ActiveSubscriberCount] = subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Active).ToString(context?.CultureInfo);
            result[Resources.HealthChecks_Data_PassiveSubscriberCount] = subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Passive).ToString(context?.CultureInfo);

            return result;
        }

    }


    /// <summary>
    /// Options for <see cref="EventMessagePush"/>
    /// </summary>
    public class EventMessagePushOptions : SubscriptionManagerOptions { }
}
