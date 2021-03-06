﻿using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Helper methods for working with <see cref="Uri"/> instances.
    /// </summary>
    public static class UriExtensions {

        /// <summary>
        /// Ensures that the specified absolute URI has a trailing slash at the end of its path.
        /// </summary>
        /// <param name="uri">
        ///   The URI.
        /// </param>
        /// <returns>
        ///   A URI that has a trailing slash at the end of its path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="uri"/> is not an absolute URI.
        /// </exception>
        public static Uri EnsurePathHasTrailingSlash(this Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            if (!uri.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(uri), uri, SharedResources.Error_AbsoluteUriRequired);
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
        public static bool TryCreateUriWithTrailingSlash(this string uriString, out Uri? uri) {
            if (uriString == null) {
                uri = null!;
                return false;
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out uri)) {
                return false;
            }

            uri = EnsurePathHasTrailingSlash(uri);
            return true;
        }


        /// <summary>
        /// Creates an absolute <see cref="Uri"/> from the specified URI string, ensuring that the 
        /// <see cref="Uri"/> is created with a trailing forwards slash.
        /// </summary>
        /// <param name="uriString">
        ///   The URI string.
        /// </param>
        /// <returns>
        ///   The URI.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static Uri CreateUriWithTrailingSlash(this string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            uri = EnsurePathHasTrailingSlash(uri);
            return uri;
        }


        /// <summary>
        /// Tests if the URI is a child path of the specified <paramref name="parentUri"/>.
        /// </summary>
        /// <param name="uri">
        ///   The URI.
        /// </param>
        /// <param name="parentUri">
        ///   The parent URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the URI is a child of the <paramref name="parentUri"/>, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parentUri"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsChildOf(this Uri uri, Uri parentUri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            if (parentUri == null) {
                throw new ArgumentNullException(nameof(parentUri));
            }

            if (!uri.IsAbsoluteUri || !parentUri.IsAbsoluteUri || uri.Equals(parentUri)) {
                return false;
            }

            return parentUri.IsBaseOf(uri);
        }


        /// <summary>
        /// Tests if the URI is a child path of the specified <paramref name="parentUriString"/>.
        /// </summary>
        /// <param name="uri">
        ///   The URI.
        /// </param>
        /// <param name="parentUriString">
        ///   The parent URI string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the URI is a child of the <paramref name="parentUriString"/>, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parentUriString"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsChildOf(this Uri uri, string parentUriString) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            if (parentUriString == null) {
                throw new ArgumentNullException(nameof(parentUriString));
            }

            if (!Uri.TryCreate(parentUriString, UriKind.Absolute, out var parentUri)) {
                return false;
            }

            return IsChildOf(uri, parentUri);
        }

    }
}
