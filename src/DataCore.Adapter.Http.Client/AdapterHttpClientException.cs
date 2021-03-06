﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataCore.Adapter.Http.Client {

    /// <summary>
    /// Exception raised when an adapter HTTP request returns a result with a non-good status code.
    /// </summary>
    public class AdapterHttpClientException : HttpRequestException {

        /// <summary>
        /// Gets the HTTP verb that was used in the request.
        /// </summary>
        public string Verb { get; }

        /// <summary>
        /// Gets the URL that was used in the request.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Gets the HTTP status code that was returned.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The HTTP request headers.
        /// </summary>
        public IDictionary<string, string[]> RequestHeaders { get; }

        /// <summary>
        /// The HTTP response headers.
        /// </summary>
        public IDictionary<string, string[]> ResponseHeaders { get; }

        /// <summary>
        /// Gets the response content that was returned.
        /// </summary>
        public string? Content { get; }

        /// <summary>
        /// The RFC 7807 problem details object that was returned in the response body, if 
        /// available.
        /// </summary>
        public ProblemDetails? ProblemDetails { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterHttpClientException"/> object.
        /// </summary>
        /// <param name="message">
        ///   The error message.
        /// </param>
        /// <param name="verb">
        ///   The HTTP verb.
        /// </param>
        /// <param name="url">
        ///   The request URL.
        /// </param>
        /// <param name="statusCode">
        ///   The HTTP status code.
        /// </param>
        /// <param name="requestHeaders">
        ///   The HTTP request headers.
        /// </param>
        /// <param name="responseHeaders">
        ///   The HTTP response headers.
        /// </param>
        /// <param name="responseContent">
        ///   The response content.
        /// </param>
        /// <param name="problemDetails">
        ///   The RFC 7807 problem details object describing the error.
        /// </param>
        public AdapterHttpClientException(string message, string verb, string url, HttpStatusCode statusCode, IDictionary<string, string[]> requestHeaders, IDictionary<string, string[]> responseHeaders, string? responseContent, ProblemDetails? problemDetails)
            : this(message, verb, url, statusCode, requestHeaders, responseHeaders, responseContent, problemDetails, null) { }


        /// <summary>
        /// Creates a new <see cref="AdapterHttpClientException"/> object with an inner exception.
        /// </summary>
        /// <param name="message">
        ///   The error message.
        /// </param>
        /// <param name="verb">
        ///   The HTTP verb.
        /// </param>
        /// <param name="url">
        ///   The request URL.
        /// </param>
        /// <param name="statusCode">
        ///   The HTTP status code.
        /// </param>
        /// <param name="requestHeaders">
        ///   The HTTP request headers.
        /// </param>
        /// <param name="responseHeaders">
        ///   The HTTP response headers.
        /// </param>
        /// <param name="responseContent">
        ///   The response content.
        /// </param>
        /// <param name="problemDetails">
        ///   The RFC 7807 problem details object describing the error.
        /// </param>
        /// <param name="inner">
        ///   The inner exception.
        /// </param>
        public AdapterHttpClientException(string message, string verb, string url, HttpStatusCode statusCode, IDictionary<string, string[]> requestHeaders, IDictionary<string, string[]> responseHeaders, string? responseContent, ProblemDetails? problemDetails, Exception? inner)
            : base(message, inner) {
            Verb = verb;
            Url = url;
            StatusCode = statusCode;
            RequestHeaders = new ReadOnlyDictionary<string, string[]>(requestHeaders ?? new Dictionary<string, string[]>());
            ResponseHeaders = new ReadOnlyDictionary<string, string[]>(responseHeaders ?? new Dictionary<string, string[]>());
            Content = responseContent;
            ProblemDetails = problemDetails;
        }


        /// <summary>
        /// Creates an <see cref="AdapterHttpClientException"/> using the specified HTTP response 
        /// message.
        /// </summary>
        /// <param name="errorMessage">
        ///   The error message.
        /// </param>
        /// <param name="response">
        ///   The HTTP response.
        /// </param>
        /// <returns>
        /// A task that will return the exception.
        /// </returns>
        /// <remarks>
        ///   If the response contains an RFC 7807 problem details object, the <see cref="ProblemDetails"/> 
        ///   property of the exception will be set.
        /// </remarks>
        internal static Task<AdapterHttpClientException> FromHttpResponseMessage(string errorMessage, HttpResponseMessage response) {
            return FromHttpResponseMessage(errorMessage, response, null);
        }


        /// <summary>
        /// Creates an <see cref="AdapterHttpClientException"/> with an inner exception, using 
        /// the specified HTTP response message.
        /// </summary>
        /// <param name="errorMessage">
        ///   The error message.
        /// </param>
        /// <param name="response">
        ///   The HTTP response.
        /// </param>
        /// <param name="inner">
        ///   The inner exception.
        /// </param>
        /// <returns>
        /// A task that will return the exception.
        /// </returns>
        /// <remarks>
        ///   If the response contains an RFC 7807 problem details object, the <see cref="ProblemDetails"/> 
        ///   property of the exception will be set.
        /// </remarks>
        internal static async Task<AdapterHttpClientException> FromHttpResponseMessage(string errorMessage, HttpResponseMessage response, Exception? inner) {
            if (response == null) {
                throw new ArgumentNullException(nameof(response));
            }

            var requestHeaders = response.RequestMessage.Content == null
                ? response.RequestMessage.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())
                : response.RequestMessage.Headers.Concat(response.RequestMessage.Content.Headers).ToDictionary(x => x.Key, x => x.Value.ToArray());

            var responseHeaders = response.Content == null
                ? response.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())
                : response.Headers.Concat(response.Content.Headers).ToDictionary(x => x.Key, x => x.Value.ToArray());

            string? content = null;
            ProblemDetails? problemDetails = null;

            if (response.Content != null) {
                var includeContent = responseHeaders.TryGetValue("Content-Type", out var contentTypes) && contentTypes != null
                    ? contentTypes.Any(CanIncludeContent)
                    : false;

                if (includeContent) {
                    content = await response.Content!.ReadAsStringAsync().ConfigureAwait(false);
                }

                if (content != null && includeContent && contentTypes.Any(IsProblemDetailsResponse)) {
                    problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(content);
                };
            }

            return new AdapterHttpClientException(
                problemDetails == null
                    ? errorMessage
                    : string.Concat(errorMessage, " ", Resources.Error_SeeProblemDetails),
                response.RequestMessage.Method.Method,
                response.RequestMessage.RequestUri.ToString(),
                response.StatusCode,
                requestHeaders,
                responseHeaders,
                content,
                problemDetails,
                inner
            );
        }


        /// <summary>
        /// Tests if the specified content type represents an RFC 7807 response.
        /// </summary>
        /// <param name="contentType">
        ///   The content type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the content type is an RFC 7807 object, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        private static bool IsProblemDetailsResponse(string contentType) {
            return contentType.StartsWith(ProblemDetails.JsonMediaType);
        }


        /// <summary>
        /// Tests if the content of an HTTP response can be included in a 
        /// <see cref="AdapterHttpClientException"/>.
        /// </summary>
        /// <param name="contentType">
        ///   The content type of the response.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the content can be included; otherwise, 
        ///   <see langword="false"/>.
        /// </returns>
        private static bool CanIncludeContent(string contentType) {
            if (string.IsNullOrWhiteSpace(contentType)) {
                return false;
            }
            if (IsProblemDetailsResponse(contentType)) {
                return true;
            }
            if (contentType.StartsWith("application/json")) {
                return true;
            }
            if (contentType.StartsWith("text/")) {
                return true;
            }

            return false;
        }

    }

}
