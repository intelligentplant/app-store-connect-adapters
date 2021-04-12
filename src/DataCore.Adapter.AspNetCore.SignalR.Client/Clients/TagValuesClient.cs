﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for querying adapter tag values.
    /// </summary>
    public class TagValuesClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="TagValuesClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public TagValuesClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Creates a snapshot tag value subscription.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to subscribe to.
        /// </param>
        /// <param name="request">
        ///   The subscription request.
        /// </param>
        /// <param name="channel">
        ///   A channel that can be used to add tags to or remove tags from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that wil emit values for the subscription.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannelAsync(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request,
            ChannelReader<TagValueSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);

            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                    "CreateSnapshotTagValueChannel",
                    adapterId,
                    request,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false);
            }

            // We are using ASP.NET Core 2.x, so we cannot use client-to-server streaming. Instead, 
            // we will make a separate streaming call for each topic, and cancel it when we detect 
            // that the topic has been unsubscribed from.

            // This is our single output channel.
            var result = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                SingleWriter = false
            });

            // Cancellation token source for each subscribed topic, indexed by topic name.
            var subscriptions = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

            // Task completion source that we will complete when the cancellation token passed to 
            // the method fires.
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ctReg = cancellationToken.Register(() => {
                result.Writer.TryComplete();
                tcs.TrySetResult(true);
            });

            // Local function that will run the subscription for a topic until the cancellation 
            // token fires.
            async Task RunTopicSubscription(string topic, CancellationToken ct) {
                var req = new CreateSnapshotTagValueSubscriptionRequest() {
                    PublishInterval = request.PublishInterval,
                    Properties = request.Properties,
                    Tags = new[] { topic }
                };
                var ch = await connection.StreamAsChannelAsync<TagValueQueryResult>(
                    "CreateSnapshotTagValueChannel",
                    adapterId,
                    req,
                    ct
                ).ConfigureAwait(false);

                while (!ct.IsCancellationRequested) {
                    var val = await ch.ReadAsync(ct).ConfigureAwait(false);
                    await result!.Writer.WriteAsync(val, ct).ConfigureAwait(false);
                }
            }

            // Local function that will add or remove subscriptions for individual topics.
            void ProcessTopicSubscriptionChange(IEnumerable<string> topics, bool added) {
                foreach (var topic in topics) {
                    if (topic == null) {
                        continue;
                    }

                    if (added) {
                        if (subscriptions!.ContainsKey(topic)) {
                            continue;
                        }

                        var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        var ct = ctSource.Token;
                        subscriptions[topic] = ctSource;
                        _ = Task.Run(async () => {
                            try {
                                await RunTopicSubscription(topic, ct).ConfigureAwait(false);
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                            finally {
                                if (subscriptions!.TryRemove(topic, out var cts)) {
                                    cts.Cancel();
                                    cts.Dispose();
                                }
                            }
                        }, ct);
                    }
                    else {
                        if (subscriptions!.TryRemove(topic, out var ctSource)) {
                            ctSource.Cancel();
                            ctSource.Dispose();
                        }
                    }
                }
            }

            // Background task that will add/remove subscriptions to indivudual tags as subscription 
            // changes occur.
            _ = Task.Run(async () => {
                try {
                    if (!cancellationToken.IsCancellationRequested && request.Tags.Any()) {
                        ProcessTopicSubscriptionChange(request.Tags, true);
                    }

                    while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        while (channel.TryRead(out var update)) {
                            if (update?.Tags == null || !update.Tags.Any()) {
                                continue;
                            }

                            ProcessTopicSubscriptionChange(update.Tags, update.Action == SubscriptionUpdateAction.Subscribe);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    result.Writer.TryComplete(e);
                }
                finally {
                    // Ensure that we wait until the cancellation token for the overall subscription has 
                    // actually fired before we dispose of any remaining subscriptions.
                    await tcs.Task.ConfigureAwait(false);
                    ctReg.Dispose();
                    result.Writer.TryComplete();
                    foreach (var topic in subscriptions.Keys.ToArray()) {
                        if (subscriptions!.TryRemove(topic, out var ctSource)) {
                            ctSource.Cancel();
                            ctSource.Dispose();
                        }
                    }
                }
            }, cancellationToken);

            return result;
        }


        /// <summary>
        /// Polls an adapter for snapshot tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValuesAsync(
            string adapterId, 
            ReadSnapshotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<TagValueQueryResult>(
                "ReadSnapshotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Polls an adapter for raw tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<ChannelReader<TagValueQueryResult>> ReadRawTagValuesAsync(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadRawTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Polls an adapter for visualisation-friendly tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<ChannelReader<TagValueQueryResult>> ReadPlotTagValuesAsync(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadPlotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Polls an adapter for tag values at specific timestamps.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimesAsync(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadTagValuesAtTimes",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the data functions that an adapter supports when calling 
        /// <see cref="ReadProcessedTagValuesAsync(string, ReadProcessedTagValuesRequest, CancellationToken)"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return descriptors for the supported data functions.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<ChannelReader<DataFunctionDescriptor>> GetSupportedDataFunctionsAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<DataFunctionDescriptor>(
                "GetSupportedDataFunctions",
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Polls an adapter for processed/aggregated tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<ChannelReader<ProcessedTagValueQueryResult>> ReadProcessedTagValuesAsync(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<ProcessedTagValueQueryResult>(
                "ReadProcessedTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="channel">
        ///   The channel that will emit the items to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the write result for each 
        ///   tag value back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async Task<ChannelReader<WriteTagValueResult>> WriteSnapshotTagValuesAsync(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                return await connection.StreamAsChannelAsync<WriteTagValueResult>(
                    "WriteSnapshotTagValues",
                    adapterId,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false);
            }

            // We are using ASP.NET Core 2.x, so we cannot use bidirectional streaming. Instead, 
            // we will read the channel ourselves and make an invocation call for every value.
            var result = Channel.CreateUnbounded<WriteTagValueResult>();

            _ = Task.Run(async () => {
                try {
                    while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        while (channel.TryRead(out var val)) {
                            if (cancellationToken.IsCancellationRequested) {
                                break;
                            }
                            if (val == null) {
                                continue;
                            }

                            var writeResult = await connection.InvokeAsync<WriteTagValueResult>(
                                "WriteSnapshotTagValue",
                                adapterId,
                                val,
                                cancellationToken
                            ).ConfigureAwait(false);

                            await result.Writer.WriteAsync(writeResult, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, cancellationToken);

            return result.Reader;
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's history archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="channel">
        ///   The channel that will emit the items to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the write result for each 
        ///   tag value back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValuesAsync(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                return await connection.StreamAsChannelAsync<WriteTagValueResult>(
                    "WriteHistoricalTagValues",
                    adapterId,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false);
            }

            // We are using ASP.NET Core 2.x, so we cannot use bidirectional streaming. Instead, 
            // we will read the channel ourselves and make an invocation call for every value.
            var result = Channel.CreateUnbounded<WriteTagValueResult>();

            _ = Task.Run(async () => {
                try {
                    while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        while (channel.TryRead(out var val)) {
                            if (cancellationToken.IsCancellationRequested) {
                                break;
                            }
                            if (val == null) {
                                continue;
                            }

                            var writeResult = await connection.InvokeAsync<WriteTagValueResult>(
                                "WriteHistoricalTagValue",
                                adapterId,
                                val,
                                cancellationToken
                            ).ConfigureAwait(false);

                            await result.Writer.WriteAsync(writeResult, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, cancellationToken);

            return result.Reader;
        }

    }
}
