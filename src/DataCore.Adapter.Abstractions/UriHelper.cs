﻿using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Helper methods for working with <see cref="Uri"/> instances.
    /// </summary>
    public static class UriHelper {

        /// <summary>
        /// Ensures that the specified absolute URI has a trailing slash at the end of its path.
        /// </summary>
        /// <param name="uri">
        ///   The absolute URI.
        /// </param>
        /// <returns>
        ///   A URI that has a trailing slash at the end of its path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uri"/> is not an absolute URI.
        /// </exception>
        public static Uri EnsurePathHasTrailingSlash(Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            if (!uri.IsAbsoluteUri) {
                throw new ArgumentException(DataCoreAdapterAbstractionsResources.Error_RelativeUrisAreNotSupported, nameof(uri));
            }

            if (uri.LocalPath.EndsWith("/", StringComparison.Ordinal)) {
                return uri;
            }

            return new Uri(string.Concat(uri.GetLeftPart(UriPartial.Path), "/", uri.Query, uri.Fragment));
        }



        /// <summary>
        /// Creates an absolute <see cref="Uri"/> from the specified URI string, ensuring that the 
        /// <see cref="Uri"/> is created with a trailing forwards slash.
        /// </summary>
        /// <param name="uriString">
        ///   The URI string.
        /// </param>
        /// <param name="uri">
        ///   The created <see cref="Uri"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if a URI could be created, or <see langword="false"/> otherwise.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "Method is for URI parsing")]
        public static bool TryCreateUriWithTrailingSlash(string uriString, out Uri uri) {
            if (uriString == null) {
                uri = null;
                return false;
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out uri)) {
                return false;
            }

            uri = EnsurePathHasTrailingSlash(uri);
            return true;
        }

    }
}