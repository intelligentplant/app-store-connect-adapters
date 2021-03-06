﻿# DataCore.Adapter.Grpc.Proxy

Proxy adapter that connects to a remote adapter via gRPC.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Grpc.Proxy](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Grpc.Proxy).


# Creating a Proxy Instance

```csharp
var options = new GrpcAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    RemoteId = "{SOME_ADAPTER_ID}"
};

// OPTION 1: Use Grpc.Core channel
var channel = new Grpc.Core.Channel("localhost:5000", Grpc.Core.ChannelCredentials.Insecure);
var proxy = new GrpcAdapterProxy(channel, Options.Create(options), NullLoggerFactory.Instance);
await proxy.StartAsync(cancellationToken);

// OPTION 2: Use Grpc.Net.Client channel (requires .NET Core 3.0 due to lack of native HTTP/2 support in `HttpClient` in earlier versions).
var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost:5000");
var proxy = new GrpcAdapterProxy(channel, Options.Create(options), NullLoggerFactory.Instance);
await proxy.StartAsync(cancellationToken);
```

## A Note on Self-Signed Certificates and Grpc.Core

If you use the `Grpc.Core.Channel` class to connect to a host that is using SSL and a self-signed certificate, you will have to provide the certificate to the `SslCredentials` constructor as a PEM-encoded string, as the certification path will not exist in the SSL roots provided by `Grpc.Core`. The [DataCore.Adapter.Security.CertificateUtilities](/src/DataCore.Adapter/Security/CertificateUtilities.cs) class contains helper methods that can convert an `X509Certificate2` into the required format, as well as load a certificate from a certificate store:

```csharp
var certPath = "cert:/CurrentUser/My/{some thumbprint or subject}";

var sslCredentials = CertificateUtilities.TryLoadCertificateFromStore(certPath, out var cert)
    ? new SslCredentials(CertificateUtilities.PemEncode(cert))
    : new SslCredentials();

var channel = new Grpc.Core.Channel("localhost:5000", sslCredentials);
```

A `Grpc.Net.Client` channel will happily connect to a host using a self-signed certificate without any additional configuration required, as long as the certificate is trusted by the client machine.


# Using the Proxy

You can use the proxy as you would any other `IAdapter` instance:

```csharp
var readRaw = proxy.Features.Get<IReadRawTagValues>();

var now = DateTime.UtcNow;

var rawChannel = readRaw.ReadRawTagValues(null, new ReadRawTagValuesRequest() {
    Tags = new[] { "Sensor_001", "Sensor_002" },
    UtcStartTime = now.Subtract(TimeSpan.FromDays(7)),
    UtcEndTime = now,
    SampleCount = 0, // i.e. all raw values inside the time range
    BoundaryType = RawDataBoundaryType.Inside
}, cancellationToken);

while (await rawChannel.WaitToReadAsync()) {
    if (rawChannel.TryRead(out var val)) {
        DoSomethingWithValue(val);
    }
}
```


# Implementing Per-Call Authentication

gRPC supports both per-channel and per-call authentication. If the remote host supports (or requires) per-call authentication, you can configure this by setting the `GetCallCredentials` property in the `GrpcAdapterProxyOptions` object you pass to the proxy constructor. The property is a delegate that takes an `IAdapterCallContext` representing the calling user, and returns a collection of [IClientCallCredentials](../DataCore.Adapter.Grpc.Client/Authentication/IClientCallCredentials.cs) objects that will be added to the headers of the outgoing gRPC request:

```csharp
var options = new GrpcAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    RemoteId = "{SOME_ADAPTER_ID}",
    GetCallCredentials = async (IAdapterCallContext context) => {
        var accessToken = await GetAccessToken(context);
        return new IClientCallCredentials[] {
            new BearerTokenCallCredentials(accessToken)
        };
    }
};
```

Note that per-call authentication requires that SSL/TLS authentication is already in place at the channel level.


# Adding Extension Feature Support

You can add support for adapter extension features by providing an `ExtensionFeatureFactory` delegate in the proxy options. This delegate will be invoked for every extension feature that the remote proxy reports that it supports:

```csharp
var options = new GrpcAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    RemoteId = "{SOME_ADAPTER_ID}",
    ExtensionFeatureFactory = (featureName, proxy) => {
        return GetFeatureImplementation(featureName, proxy);
    }
};
```

The `CreateClient<TClient>` method on the proxy can be used to create a gRPC client that uses the channel configured when the proxy was created.
