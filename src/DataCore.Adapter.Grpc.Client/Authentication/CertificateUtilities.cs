﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Utilities for working with certificates (e.g. to assist with loading PEM-encoded roots for 
    /// use with gRPC clients that use Grpc.Core).
    /// </summary>
    public static class CertificateUtilities {

        /// <summary>
        /// Maximum line width for PEM encoding.
        /// </summary>
        private const int PemLineWidth = 64;

        /// <summary>
        /// PEM header/footer label for a certificate.
        /// </summary>
        private const string CertificateLabel = "CERTIFICATE";

        /// <summary>
        /// Regex for matching the full path to a certificate in a certificate store e.g. 
        /// <c>cert:\CurrentUser\My\1234567890abcdef1234567890abcdef123456789</c>
        /// </summary>
        private static readonly Regex s_certPathWithThumbprintRegex = new Regex(@"^CERT\:\\(?<location>.+?)\\(?<store>.+)\\(?<thumbprint>.+)$", RegexOptions.IgnoreCase);


        /// <summary>
        /// PEM-encodes the specified bytes.
        /// </summary>
        /// <param name="bytes">
        ///   The bytes.
        /// </param>
        /// <param name="label">
        ///   The PEM header/footer label for the item.
        /// </param>
        /// <param name="explanatoryText">
        ///   Additional explanatory text to accompany the item.
        /// </param>
        /// <param name="builder">
        ///   The string builder to write the output to.
        /// </param>
        private static void PemEncode(byte[] bytes, string label, IEnumerable<string> explanatoryText, StringBuilder builder) {
            if (bytes == null) {
                throw new ArgumentNullException(nameof(bytes));
            }
            if (label == null) {
                throw new ArgumentNullException(nameof(label));
            }

            if (explanatoryText != null) {
                foreach (var item in explanatoryText) {
                    builder.Append(item);
                    builder.Append('\n');
                }
            }

            var base64String = Convert.ToBase64String(bytes);

            builder.Append("-----BEGIN ");
            builder.Append(label);
            builder.Append("-----\n");

            var i = 0;
            var @continue = false;

            do {
                ++i;
                var chars = base64String.Skip((i - 1) * PemLineWidth).Take(PemLineWidth);
                var lengthBefore = builder.Length;
                foreach (var c in chars) {
                    builder.Append(c);
                }
                @continue = builder.Length == lengthBefore + PemLineWidth;
                builder.Append('\n');
            } while (@continue);

            builder.Append("-----END ");
            builder.Append(label);
            builder.Append("-----\n");
        }


        /// <summary>
        /// PEM-encodes a certificate, and optionally includes the full certificate chain.
        /// </summary>
        /// <param name="certificate">
        ///   The certificate to encode.
        /// </param>
        /// <param name="encodeChain">
        ///   <see langword="true"/> to encode the full certificate chain, or <see langword="false"/> 
        ///   to encode the specified certificate only.
        /// </param>
        /// <returns>
        ///   The encoded certificate(s).
        /// </returns>
        public static string PemEncode(X509Certificate2 certificate, bool encodeChain = false) {
            if (certificate == null) {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (!encodeChain) {
                return PemEncode(new[] { certificate });
            }

            using (var chain = new X509Chain()) {
                chain.Build(certificate);

                try {
                    return PemEncode(chain.ChainElements.Cast<X509ChainElement>().Select(x => x.Certificate));
                }
                finally {
                    chain.Reset();
                }
            }
        }


        /// <summary>
        /// PEM-encodes the specified X.509 certificates.
        /// </summary>
        /// <param name="certificates">
        ///   The certificates to encode.
        /// </param>
        /// <returns>
        ///   The encoded certificates.
        /// </returns>
        public static string PemEncode(params X509Certificate2[] certificates) {
            return PemEncode((IEnumerable<X509Certificate2>) certificates);
        }


        /// <summary>
        /// PEM-encodes the specified X.509 certificates.
        /// </summary>
        /// <param name="certificates">
        ///   The certificates to encode.
        /// </param>
        /// <returns>
        ///   The encoded certificates.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificates"/> is <see langword="null"/>.
        /// </exception>
        public static string PemEncode(IEnumerable<X509Certificate2> certificates) {
            if (certificates == null) {
                throw new ArgumentNullException(nameof(certificates));
            }

            var builder = new StringBuilder();

            foreach (var certificate in certificates) {
                if (certificate == null) {
                    continue;
                }

                PemEncode(certificate.Export(X509ContentType.Cert), CertificateLabel, new[] { 
                    $"# Subject: {certificate.Subject}",
                    $"# Issuer: {certificate.Issuer}",
                    $"# Friendly Name: {(certificate.FriendlyName ?? "<UNSPECIFIED>")}",
                    $"# Thumbprint: {certificate.Thumbprint}",
                    $"# Validity: from {certificate.NotBefore:yyyy-MM-ddTHH:mm:ss} UTC to {certificate.NotAfter:yyyy-MM-ddTHH:mm:ss} UTC"
                }, builder);

                builder.Append('\n');
            }

            return builder.ToString();
        }


        /// <summary>
        /// Loads certificates from the specified store that match the provided thumbprint or 
        /// subject name.
        /// </summary>
        /// <param name="store">
        ///   The certificate store.
        /// </param>
        /// <param name="thumbprintOrSubjectName">
        ///   The thumbprint or subject name to match.
        /// </param>
        /// <returns>
        ///   The matching certificates.
        /// </returns>
        private static IEnumerable<X509Certificate2> LoadCertificates(X509Store store, string thumbprintOrSubjectName) {
            var certificatesByThumbprint = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                thumbprintOrSubjectName,
                true
            );

            var certificatesBySubjectName = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                thumbprintOrSubjectName,
                true
            );

            return certificatesByThumbprint
                .Cast<X509Certificate2>()
                .Concat(certificatesBySubjectName.Cast<X509Certificate2>());
        }


        /// <summary>
        /// Tries to parse a certificate path string into the corresponding 
        /// <see cref="StoreLocation"/>, store name, and thumbprint or subject.
        /// </summary>
        /// <param name="path">
        ///   The certificate store path, in the format <c>cert:\{location}\{name}\{thumbprint_or_subject}</c>.
        /// </param>
        /// <param name="location">
        ///   The store location.
        /// </param>
        /// <param name="name">
        ///   The store name.
        /// </param>
        /// <param name="thumbprintOrSubjectName">
        ///   The thumbprint or subject name for the certificate.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the path could be parsed, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryParseCertificateStorePath(string path, out StoreLocation location, out string name, out string thumbprintOrSubjectName) {
            if (path == null) {
                location = default;
                name = null;
                thumbprintOrSubjectName = null;
                return false;
            }

            var m = s_certPathWithThumbprintRegex.Match(path);
            if (!m.Success) {
                location = default;
                name = null;
                thumbprintOrSubjectName = null;
                return false;
            }

            var loc = m.Groups["location"].Value;
            if (!Enum.TryParse(loc, out location)) {
                location = default;
                name = null;
                thumbprintOrSubjectName = null;
                return false;
            }

            name = m.Groups["store"].Value;
            thumbprintOrSubjectName = m.Groups["thumbprint"].Value;
            return true;
        }


        /// <summary>
        /// Loads the certificate from the specified certificate store path.
        /// </summary>
        /// <param name="path">
        ///   The certificate store path, in the format <c>cert:\{location}\{name}\{thumbprint_or_subject}</c>.
        /// </param>
        /// <param name="certificate">
        ///   The matching certificate.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the certificate could be loaded, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryLoadCertificateFromStore(string path, out X509Certificate2 certificate) {
            if (path == null) {
                certificate = null;
                return false;
            }

            if (!TryParseCertificateStorePath(path, out var location, out var name, out var thumbprintOrSubjectName)) {
                certificate = null;
                return false;
            }

            using (var store = new X509Store(name, location)) {
                store.Open(OpenFlags.ReadOnly);
                certificate = LoadCertificates(store, thumbprintOrSubjectName).FirstOrDefault(x => x.Verify());
                return true;
            }
        }

    }
}
