# Tutorial - Writing an Adapter Extension Feature

_This is part 1 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Getting Started

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-01)._

In addition to the standard set of features that an adapter can implement, it is also possible for an adapter to implement vendor-specific extension features. Like standard features, extension features are identified via a URI. Extension features also define a set of _operations_, which are also identified via a URI that is a child path of the extension feature URI.

Operations can fall into one of the following categories:

- `Invoke` - standard request/response operations.
- `Streaming` - operations that receive a single request but stream multiple responses back to the caller.
- `Duplex Streaming` - operations that receive a stream of requests from the caller, and send a stream of responses back.

> In this part of the tutorial, we will focus on registering the extension, and then add operations in subsequent chapters.

Extension features must meet the following criteria:

1. The extension must be defined as a class or interface that extends or implements the [IAdapterExtensionFeature](/src/DataCore.Adapter.Abstractions/Extensions/IAdapterExtensionFeature.cs) interface.
2. The extension type must be annotated with the [ExtensionFeatureAttribute](/src/DataCore.Adapter.Abstractions/Extensions/ExtensionFeatureAttribute.cs) attribute to supply required extension metadata (such as the extension's URI).

The `IAdapterExtensionFeature` interface defines a number of methods that allow a caller to retrieve metadata about the extension and its available operations, and to call the operations. The [AdapterExtensionFeature](/src/DataCore.Adapter/Extensions/AdapterExtensionFeature.cs) base class helps to simplify a lot of these tasks, and allows extension authors a way of writing strongly-typed methods that can in turn be invoked via the standard methods defined on `IAdapterExtensionFeature`.

To get started, create a new console app in Visual Studio or from the command line using the `dotnet new` command:

```
mkdir MyAdapter
cd MyAdapter
dotnet new console
```

Next, we will add a package references to the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/), [IntelligentPlant.AppStoreConnect.Adapter.Json](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Json/) and [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting/) NuGet packages.

We will use the [.NET Generic Host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host) to run the example console applications in this tutorial. After you have added the above package references, replace the code in your `Program.cs` file with the following:

```csharp
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    class Program {

        public static async Task Main(params string[] args) {
            await CreateHostBuilder(args).RunConsoleAsync().ConfigureAwait(false);
        }


        private static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args).ConfigureServices(services => {
                services.AddHostedService<Runner>();
            });
        }

    }

}
```

Next, add a new file to the project called `Runner.cs` and replace the code with the following:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    internal class Runner : BackgroundService {

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Task.CompletedTask;
        }

    }
}
```

The `Runner` class is a background service run by the .NET Generic Host that we will use to run our application logic.


### Creating the Adapter

> You can learn more about creating adapters in the [Creating an Adapter tutorial](/docs/tutorials/creating-an-adapter).

For the purposes of this tutorial, we will create a very simple adapter that does not implement any of the standard features beyond those we inherit from the `AdapterBase` base class. Create a new class file in your console app called `Adapter.cs` and add the following code:

```csharp
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase {

        public Adapter(
            string id,
            string name,
            string description,
            IBackgroundTaskService backgroundTaskService = null,
            ILogger<Adapter> logger = null
        ) : base(id, new AdapterOptions() { Name = name, Description = description }, backgroundTaskService, logger) { }

        protected override Task StartAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
```

As you can see, this is a very bare-bones adapter, which does nothing other than inheriting from the `AdapterBase` base class. Next, replace the code in your `Runner.cs` file with the following:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    internal class Runner : BackgroundService {

        private const string AdapterId = "example";

        private const string AdapterDisplayName = "Example Adapter";

        private const string AdapterDescription = "Example adapter with an extension feature, built using the tutorial on GitHub";


        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Run(new DefaultAdapterCallContext(), stoppingToken);
        }


        private static async Task Run(IAdapterCallContext context, CancellationToken cancellationToken) {
            await using (IAdapter adapter = new Adapter(AdapterId, AdapterDisplayName, AdapterDescription)) {

                await adapter.StartAsync(cancellationToken);

                var adapterDescriptor = adapter.CreateExtendedAdapterDescriptor();

                Console.WriteLine();
                Console.WriteLine($"[{adapter.Descriptor.Id}]");
                Console.WriteLine($"  Name: {adapter.Descriptor.Name}");
                Console.WriteLine($"  Description: {adapter.Descriptor.Description}");
                Console.WriteLine("  Properties:");
                foreach (var prop in adapter.Properties) {
                    Console.WriteLine($"    - {prop.Name} = {prop.Value}");
                }
                Console.WriteLine("  Features:");
                foreach (var feature in adapterDescriptor.Features) {
                    Console.WriteLine($"    - {feature}");
                }
                Console.WriteLine("  Extensions:");
                foreach (var feature in adapterDescriptor.Extensions) {
                    var extension = adapter.GetFeature<IAdapterExtensionFeature>(feature);
                    var extensionDescriptor = await extension.GetDescriptor(context, feature, cancellationToken);
                    var extensionOps = await extension.GetOperations(context, feature, cancellationToken);
                    Console.WriteLine($"    - {feature}");
                    Console.WriteLine($"      - Name: {extensionDescriptor.DisplayName}");
                    Console.WriteLine($"      - Description: {extensionDescriptor.Description}");
                    Console.WriteLine($"      - Operations:");
                    foreach (var op in extensionOps) {
                        Console.WriteLine($"        - {op.Name} ({op.OperationId})");
                        Console.WriteLine($"          - Description: {op.Description}");
                    }
                }

            }
        }

    }
}
```

If you compile and run the program, you will see output similar to the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter with an extension feature, built using the tutorial on GitHub
  Properties:
  Features:
    - asc:features/diagnostics/health-check/
  Extensions:
```

At present, we don't see anything under the `Extensions` section of the program output, because our adapter does not implement any extension features. Our next step is to start defining our extension.


### Creating the Extension Feature

Our extension feature will be a ping-pong service, allowing callers to send a ping message to the adapter and receive a pong message in response. First, we will define our `PingMessage` and `PongMessage` types. Create a new class file called `Models.cs` and replace the content with the following:

```csharp
using System;

using DataCore.Adapter.Extensions;

namespace MyAdapter {

    [ExtensionFeatureDataType(typeof(PingPongExtension), "ping-message")]
    public class PingMessage {
        public string CorrelationId { get; set; }
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }

    [ExtensionFeatureDataType(typeof(PingPongExtension), "pong-message")]
    public class PongMessage {
        public string CorrelationId { get; set; }
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }

}
```

Our models allow a caller to specify a correlation ID for a ping message, and receive a pong message that contains a matching correlation ID. We also use the `[ExtensionFeatureDataType]` attribute to annotate our messages. This annotation is used when decoding and encoding messages received and returned by our extension. Don't worry about any compiler errors related to `PingPongExtension` being undefined; we will create the missing type below.

Next, we will create the extension itself. Create a new class file, `PingPongExtension.cs`, and replace the code with the following:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

namespace MyAdapter {

    [ExtensionFeature(
        ExtensionUri,
        Name = "Ping Pong",
        Description = "Example extension feature."
    )]
    public class PingPongExtension : AdapterExtensionFeature {

        public const string ExtensionUri = "tutorial/ping-pong/";

        public PingPongExtension(IBackgroundTaskService backgroundTaskService, params IObjectEncoder[] encoders) 
            : base(backgroundTaskService, encoders) { }

    }
}
```

Let's look at some points of interest in the above code: 

Our class inherits from [AdapterExtensionFeature](/src/DataCore.Adapter/Extensions/AdapterExtensionFeature.cs), which takes care of the [IAdapterExtensionFeature](/src/DataCore.Adapter.Abstractions/Extensions/IAdapterExtensionFeature.cs) implementation for us. It is annotated with an [ExtensionFeatureAttribute](/src/DataCore.Adapter.Abstractions/Extensions/ExtensionFeatureAttribute.cs), which is used to specify some additional metadata about the extension. This metadata is used to build the `FeatureDescriptor` returned by a call to the `IAdapterExtensionFeature` interface's `GetDescriptor` method. The metadata includes a URI that is used to identify the extension. We have specified a relative URI, which will be made absolute using the base path defined by `WellKnownFeatures.Extensions.ExtensionFeatureBasePath` ([see here](/src/DataCore.Adapter.Abstractions/WellKnownFeatures.cs)). It is also possible to specify an absolute URI, as long as it is a child path of the `WellKnownFeatures.Extensions.ExtensionFeatureBasePath`.

In the constructor for our feature, we receive an array of [IObjectEncoder](/src/DataCore.Adapter.Core/Common/IObjectEncoder.cs) objects. `IObjectEncoder` is a service that can encode and decode data contained in custom [EncodedObject](/src/DataCore.Adapter.Core/Common/EncodedObject.cs) objects that are received and returned by extension features.

Update the constructor for our adapter as follows so that it registers our extension feature and uses the default [JsonObjectEncoder](/src/DataCore.Adapter.Json/JsonObjectEncoder.cs) `IObjectEncoder` implementation to encode and decode custom object data to/from JSON:

```csharp
public Adapter(
    string id,
    string name,
    string description,
    IBackgroundTaskService backgroundTaskService = null,
    ILogger<Adapter> logger = null
) : base(id, new AdapterOptions() { Name = name, Description = description }, backgroundTaskService, logger) {
    AddExtensionFeatures(new PingPongExtension(backgroundTaskService, DataCore.Adapter.Json.JsonObjectEncoder.Default);
}
```

If you compile and run the program again, you will now see output similar to the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter with an extension feature, built using the tutorial on GitHub
  Properties:
  Features:
    - asc:features/diagnostics/health-check/
  Extensions:
    - asc:extensions/tutorial/ping-pong/
      - Name: Ping Pong
      - Description: Example extension feature.
      - Operations:
```

We can see that our extension is listed in the program output, but we don't yet have any extension operations available for us to call. We'll rectify that in the next chapter.


## Next Steps

In the [next chapter](02-Extension_Methods.md), we will start adding invocable methods to our extension feature.
