﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for reading raw tag values from an adapter.
    /// </summary>
    public interface IReadRawTagValues : IAdapterFeature {

        /// <summary>
        /// Reads raw data from the adapter.
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
        ///   The raw tag values for the requested tags.
        /// </returns>
        /// <remarks>
        ///   If the <see cref="ReadRawTagValuesRequest.SampleCount"/> is less than one, this should be 
        ///   interpreted as meaning that the caller is requesting as many samples inside the time 
        ///   range as possible. The adapter can apply its own maximum sample count to the queries it 
        ///   receives.
        /// </remarks>
        Task<IEnumerable<HistoricalTagValues>> ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken);

    }
}
