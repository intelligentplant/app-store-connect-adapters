﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Events.Features {

    /// <summary>
    /// Feature for querying historical event messages using a time range.
    /// </summary>
    public interface IReadEventMessagesForTimeRange : IAdapterFeature {

        /// <summary>
        /// Reads historical event messages from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The event message query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The event messages that occurred during the time range.
        /// </returns>
        Task<TimeBasedEventMessageCollection> ReadEventMessages(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken);

    }
}
