﻿# DataCore.Adapter.AspNetCore.Grpc

This project provides helper methods to add gRPC adapter services to an ASP.NET Core application.


# Registering Adapters

In most cases, adapters should be registered as singleton services with the ASP.NET Core service collection:

```csharp
services.AddSingleton<IAdapter, MyAdapter>();
```

If your application can dynamically add or remove adapters at runtime, you must handle the adapter lifetimes yourself.


# Registering Adapter Services

Adapter services must be added to the application in the `Startup.cs` file's `ConfigureServices` method. For example:

```csharp
// Configure adapter services
services.AddDataCoreAdapterServices(options => {
    // Host information metadata.
    options.HostInfo = new Common.Models.HostInfo(
        "My Host",
        "A brief description of the hosting application",
        "0.9.0-alpha", // SemVer v2
        new VendorInfo("Intelligent Plant", new Uri("https://appstore.intelligentplant.com")),
        new Dictionary<string, string>() {
            { "Project URL", "https://github.com/intelligentplant/app-store-connect-adapters" }
        }
    );

    // Register our feature authorization handler.
    options.UseFeatureAuthorizationHandler<MyFeatureAuthorizationHandler>();
});
```


# Registering gRPC Services

gRPC services must be added to the application's endpoints in the `Startup.cs` file's `Configure` method:

```csharp
app.UseRouting();

app.UseEndpoints(endpoints => {
    endpoints.MapDataCoreGrpcServices();
});
```