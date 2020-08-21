﻿using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading visualization-friendly tag values from an adapter.
    /// </summary>
    [AdapterFeature(WellKnownFeatures.RealTimeData.ReadPlotTagValues)]
    public interface IReadPlotTagValues : IAdapterFeature {

        /// <summary>
        /// Reads plot data from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The data query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel containing the values for the requested tags.
        /// </returns>
        Task<ChannelReader<TagValueQueryResult>> ReadPlotTagValues(
            IAdapterCallContext context, 
            ReadPlotTagValuesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
