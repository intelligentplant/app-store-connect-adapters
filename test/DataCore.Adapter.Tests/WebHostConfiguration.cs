﻿using System;

#if NETCOREAPP
using GrpcNet = Grpc.Net;
#endif

using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Tests {
    internal static class WebHostConfiguration {

        public const string DefaultHostName = "localhost";

        public const int DefaultPortNumber = 31415;

        public static string DefaultUrl { get; } = string.Concat("https://", DefaultHostName, ":", DefaultPortNumber);

        public const string AdapterId = "sensor-csv";

        public const string TestTagId = "Sensor_001";

        public const string HttpClientName = "AdapterHttpClient";


        internal static void AllowUntrustedCertificates(HttpMessageHandler handler) {
            // For unit test purposes, allow all SSL certificates.
#if NETCOREAPP
            if (handler is SocketsHttpHandler socketsHandler) {
                socketsHandler.SslOptions.RemoteCertificateValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                return;
            }
#endif
            if (handler is HttpClientHandler clientHandler) {
                clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
            }
        }


        public static void ConfigureDefaultServices(IServiceCollection services) {
            services.AddLogging(options => {
                options.AddConsole();
                options.AddDebug();
                options.SetMinimumLevel(LogLevel.Trace);
            });

            services.AddHttpClient(HttpClientName).ConfigureHttpMessageHandlerBuilder(builder => {
                AllowUntrustedCertificates(builder.PrimaryHandler);
            }).ConfigureHttpClient(client => {
                client.BaseAddress = new Uri(DefaultUrl + "/");
            });
            services.AddHttpClient<Http.Client.AdapterHttpClient>(HttpClientName);

#if NETCOREAPP
            services.AddTransient(sp => {
                return GrpcNet.Client.GrpcChannel.ForAddress(DefaultUrl, new GrpcNet.Client.GrpcChannelOptions() {
                    HttpClient = sp.GetService<IHttpClientFactory>().CreateClient(HttpClientName)
                });
            });
#endif
        }

    }
}
