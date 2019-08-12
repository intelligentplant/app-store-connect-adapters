﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter alarm and event messages.
    /// </summary>
    public class EventsClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private const string UrlPrefix = "api/data-core/v1.0/events";

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="EventsClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public EventsClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Reads historical event messages from an adapter using a time range.
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
        ///   A task that will return the results back to the caller.
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
        public async Task<IEnumerable<EventMessage>> ReadEventMessagesAsync(string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/by-time-range";

            using (var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false)) {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<IEnumerable<EventMessage>>(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Reads historical event messages from an adapter using an initial cursor position.
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
        ///   A task that will return the results back to the caller.
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
        public async Task<IEnumerable<EventMessageWithCursorPosition>> ReadEventMessagesAsync(string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/by-cursor";

            using (var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false)) {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<IEnumerable<EventMessageWithCursorPosition>>(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Writes event messages to an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="events">
        ///   The events to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the results back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="events"/> is <see langword="null"/>.
        /// </exception>
        public async Task<IEnumerable<WriteEventMessageResult>> WriteEventMessagesAsync(string adapterId, IEnumerable<WriteEventMessageItem> events, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }

            events = events.Where(x => x != null).ToArray();
            if (!events.Any()) {
                return new WriteEventMessageResult[0];
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write";

            using (var response = await _client.HttpClient.PostAsJsonAsync(url, events, cancellationToken).ConfigureAwait(false)) {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<IEnumerable<WriteEventMessageResult>>(cancellationToken).ConfigureAwait(false);
            }
        }

    }
}