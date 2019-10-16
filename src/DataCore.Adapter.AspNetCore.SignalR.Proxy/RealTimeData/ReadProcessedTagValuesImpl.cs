﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IReadProcessedTagValues"/>.
    /// </summary>
    internal class ReadProcessedTagValuesImpl : ProxyAdapterFeature, IReadProcessedTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadProcessedTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadProcessedTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagValues.GetSupportedDataFunctionsAsync(AdapterId, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ChannelReader<ProcessedTagValueQueryResult> ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<ProcessedTagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var hubChannel = await client.TagValues.ReadProcessedTagValuesAsync(AdapterId, request, ct).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }
        
    }

}