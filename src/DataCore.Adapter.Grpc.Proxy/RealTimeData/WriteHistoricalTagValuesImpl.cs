﻿using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IWriteHistoricalTagValues"/> implementation.
    /// </summary>
    internal class WriteHistoricalTagValuesImpl : ProxyAdapterFeature, IWriteHistoricalTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteHistoricalTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public WriteHistoricalTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async Task<ChannelReader<Adapter.RealTimeData.WriteTagValueResult>> WriteHistoricalTagValues(IAdapterCallContext context, ChannelReader<Adapter.RealTimeData.WriteTagValueItem> channel, CancellationToken cancellationToken) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcStream = client.WriteHistoricalTagValues(GetCallOptions(context, cancellationToken));

            // Create the subscription.
            await grpcStream.RequestStream.WriteAsync(new WriteTagValueRequest() {
                Init = new WriteTagValueInitMessage() {
                    AdapterId = AdapterId
                }
            }).ConfigureAwait(false);

            // Stream subscription changes.
            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        while (ch.TryRead(out var update)) {
                            if (update == null) {
                                continue;
                            }

                            await grpcStream.RequestStream.WriteAsync(new WriteTagValueRequest() {
                                Write = update.ToGrpcWriteTagValueItem()
                            }).ConfigureAwait(false);
                        }
                    }
                }
                finally {
                    if (!ct.IsCancellationRequested) {
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }
            }, BackgroundTaskService, cancellationToken);

            // Stream the results.
            var result = ChannelExtensions.CreateTagValueWriteResultChannel(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                // Read tag values.
                while (await grpcStream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                    if (grpcStream.ResponseStream.Current == null) {
                        continue;
                    }

                    await result.Writer.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterWriteTagValueResult(), ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }

}
