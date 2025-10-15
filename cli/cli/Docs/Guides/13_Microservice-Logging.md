Logging from a Microservice
## Dependencies

This guide assumes you have an existing Microservice. You need to complete the 
[Getting-Started Guide]
(doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools).

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

In order to configure a Microservice, you also need to have a local `.beamable` workspace with a Beamable Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init MyProject
cd MyProject
dotnet beam project new service HelloWorld
```

## Logs

In CLI 6.0+, Microservices use the standard [Microservice Logging Tools](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line) for logging. To log something, use the following example: 

```csharp
[ClientCallable]  
public void SampleLog()  
{  
    Log.Information("Hello");  
}
```

The static functions on the `Log` type will reference a default `ILogger`. If you want to use custom logging functions, access the `ILogger` via `Log.Default`. Beamable Microservices use _ZLogger_ under the hood, so you can access those methods directly if you want to. 

```csharp
[ClientCallable]  
public void SampleLog()  
{  
    Log.Default.ZLogInformation($"Hello");  
}
```

> 📘 Local Logs Do Not Appear In Portal By Default
> Local microservice logs will not appear in Portal unless the `BEAM_LOCAL_OTEL` environment variable is set.

### Log Level

Each log has a _level_, or an _importance_ rating. Logs are one of the following, 
- _Critical_
- _Error_
- _Warning_
- _Information_
- _Debug_
- _Verbose_

When you run a Microservice locally, you will see log messages at the _Debug_ level and above. However, in a deployed Microservice, you will only see **Information** and above. This is called the _Log Level_. It is considered best practice to avoid logging _Debug_ and _Verbose_ logs in production, because they negatively impact performance. 

The `Log` type has methods for each type of log level.

> 📘 Default Log Level
> The default log level is `DEBUG` when you are running a service locally in development. However, deployed services use the `INFO` level by default. 

#### Request Dynamic Log Levels

In version 6.0+, you can override the log level per request based on the player calling your Microservice, or the path being invoked on the service. By default, the log level for the entire service is _Information_, but if there was an error-prone route, or a specific user was experiencing issues, you could set the log level to _Debug_ for that use case, without affecting the log level for anything else. 

To set a dynamic log level, go to the Portal's microservice page, and create a new _Log Config Rule_. Once you create a rule, the Microservice will automatically update to emit logs at the new level. It may take a few seconds for the log level to change. 

### Attributes

In version 6.0+, Microservice logs include attributes that can be explored in Portal. 
#### Single Custom Attributes

You can add custom attributes per log message by using the standard string formatting approach: 
```csharp
[ClientCallable]  
public void CustomAttribute(int a)  
{  
    Log.Information("attribute {a}", a);  
}
```

If the method was invoked with `a` equal to `42`, then the rendered log message would appear as:
```
attribute 42
```

However, the attribute, `a`, is available for querying in Portal. Use a custom search expression for `a:42` to find any log messages with the `a` attribute value of `42`. 

#### Scoped Custom Attributes

It is possible to automatically add attributes to an entire sequence of logs. For example, imagine you wanted to tag a series of log lines as being part of an algorithm.

```csharp
[ClientCallable]
public void CustomAttribute(int a)
{
	var isOdd = CheckIfNumberIsOdd(a);
	Log.Information("{a} is odd={isOdd}", a, isOdd);
}

public bool CheckIfNumberIsOdd(int number)
{
	using var _ = Log.Default.BeginScope(new Dictionary<string, object>
	{
		["operation"] = nameof(CheckIfNumberIsOdd),
		["number"] = number
	});
	
	Log.Debug("Checking number");

	var isOdd = number % 2 == 1;
	if (isOdd)
	{
		Log.Debug("number is odd!");
	}
	else
	{
		Log.Debug("number is even!");
	}

	return isOdd;
}
```

The log lines will be rendered as follows:

```
Checking number
number is even!
42 is odd=False
```

However, the _attributes_ available on the log lines will include the `operation` and `number`, while the attributes on the last log will not. 

#### Defined Custom Attributes

In the previous section, the custom log attributes are localized to specific log events. The attributes are searchable in Portal, but they will not appear as _known_ attributes, because they are not declared at any top level location. In order to define known attributes, you need to use a custom `ITelemetryAttributeProvider`. 

```csharp
public class CustomAttributes : ITelemetryAttributeProvider
{
	public List<TelemetryAttributeDescriptor> GetDescriptors()
	{
		throw new System.NotImplementedException();
	}

	public void CreateDefaultAttributes(IDefaultAttributeContext ctx)
	{
		throw new System.NotImplementedException();
	}

	public void CreateConnectionAttributes(IConnectionAttributeContext ctx)
	{
		throw new System.NotImplementedException();
	}

	public void CreateRequestAttributes(IRequestAttributeContext ctx)
	{
		throw new System.NotImplementedException();
	}
}
```

And register the `CustomAttributes` in the dependency scope of your service. 

```csharp
public static async Task Main()  
{  
    await BeamServer  
        .Create()  
        .IncludeRoutes<YourService>(routePrefix: "")  
        .ConfigureServices(builder =>  
        {  
            builder.AddSingleton<CustomAttributes>();  
        })        .RunForever();  
}
```

The various _CreateAttribute_ functions should add attributes to the current context of a request. 
- `DefaultAttributes` allow you to add an attribute to _every_ log line.
- `ConnectionAttributes` allow you to add an attribute to every log line that is part of a specific _connection_ to Beamable. When a Microservice runs locally, there is only a single connection, but in a deployed environment, there are _10_ connections. 
- `RequestAttributes` allow you add attributes to every log line per _request_. 

The `GetDescriptors()` function must return a description for all attributes you want to be defined. When an attribute is described from the return value, the title and description will appear in Portal. 

### Standard Log Attributes

There are several standard log attributes that will be included automatically. Some of these are from the Open Telemetry (OTEL) standard, and others are from Beamable's logging middleware. 

| Name                                    | Description                                                                                    | Conditions                                             | Standard Set | LEVEL   |
| --------------------------------------- | ---------------------------------------------------------------------------------------------- | ------------------------------------------------------ | ------------ | ------- |
| id                                      | A unique id for the log message                                                                |                                                        | OTEL         |         |
| service.instance.id                     | A unique id for the process sending data                                                       |                                                        | OTEL         |         |
| service.name                            | The fully qualified name of the service                                                        |                                                        | OTEL         |         |
| service.namespace                       | The beamoId of the service                                                                     |                                                        | OTEL         |         |
| beam.cid                                | The customer id                                                                                |                                                        | BEAM         |         |
| beam.pid                                | The realm id                                                                                   |                                                        | BEAM         |         |
| beam.owner_id                           | The player id of the person that started the process, or 0 if the process was started remotely | Only included when the service is running locally      | BEAM         |         |
| beam.owner_email                        | The email address of the person that started the process                                       | Only included when the service is running locally      | BEAM         |         |
| beam.routing_key                        | The routing key for the service                                                                | Only included when the service is running locally      | BEAM         |         |
| beam.sdk_version                        | The semantic SDK version of the beamable SDK reporting data                                    |                                                        | BEAM         |         |
| beam.connection.id                      | A UUID for the connection sending data. There may be multiple connectionIds per instanceId     | Only included after a connection has been established  | BEAM         |         |
| beam.connection.request.request_id      | The UUID of the request.                                                                       | Only included when the service is processing a request | BEAM         |         |
| beam.connection.request.conn_request_id | The id of the request. Only unique per beam.connection.id                                      | Only included when the service is processing a request | BEAM         | VERBOSE |
| beam.connection.request.player_id       | The player id of the person that started the request, or 0 if there is no associated user      | Only included when the service is processing a request | BEAM         |         |
| beam.connection.request.path            | The relative path of the `[Callable]` being invoked through a request                          | Only included when the service is processing a request | BEAM         |         |
| beam.connection.request.root_trace_id   | The top level trace id from Beamable's internal observability stack                            | Only included when the service is processing a request | BEAM         | VERBOSE |
| beam.connection.request.parent_trace_id | The most recent parent trace id from Beamable's internal observability stack                   | Only included when the service is processing a request | BEAM         | VERBOSE |

### Third Party Log Hosting

Starting with version 6.0, it is possible to send Microservice logs to a third parties. In this example, we will use  BetterStack.

This section will assume you have set up a BetterStack account, and created a _Source_ such that you have a _source token_ and an _ingesting host_. 

To start, we will configure locally running Microservices to send data to BetterStack. To start, create this file called `config.yaml` next to your `BeamableServices.sln` file.

```yml
receivers:  
  otlp:  
    protocols:  
      grpc:  
        endpoint: 0.0.0.0:4317  
      http:  
        endpoint: 0.0.0.0:4318  
  
processors:  
  batch:  
  
exporters:  
  otlphttp/betterstack:  
    endpoint: "${env:BETTERSTACK_ENDPOINT}"  
    headers:  
      Authorization: "${env:BETTERSTACK_AUTH}"  
  
service:  
  pipelines:  
    metrics/betterstack:  
      receivers: [otlp]  
      processors: [batch]  
      exporters: [otlphttp/betterstack]  
    logs/betterstack:  
      receivers: [otlp]  
      processors: [batch]  
      exporters: [otlphttp/betterstack]
```

> [!TIP]
> Learn more about this file by reading the [documentation](https://opentelemetry.io/docs/collector/configuration/) 

Prepare the following environment variables. 
```sh
# Send local log data to Portal for debug purposes
export BEAM_LOCAL_OTEL=true

# Use the tokenSource from better stack instead of 123
export BETTERSTACK_AUTH="Bearer 123"

# Use the ingestingHost 
export BETTERSTACK_ENDPOINT="https://123.eu-nbg-2.betterstackdata.com"
```

Run the standard Open Telemetry collector in Docker, 
```sh
docker run -p 4317:4317 -p 4318:4318 \  
	-v $(pwd)/config.yaml:/etc/otelcol-contrib/config.yaml \  
	-e BETTERSTACK_ENDPOINT=$BETTERSTACK_ENDPOINT \  
	-e BETTERSTACK_AUTH=$BETTERSTACK_AUTH \  
	ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.125.0
```

Now you have a locally running telemetry collector. We need to configure your local Microservice to _use_ the collector. Prepare the following environment variables, 

```sh
export BEAM_DISABLE_STANDARD_OTEL=1  
export BEAM_OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:4318  
export BEAM_OTEL_EXPORTER_OTLP_PROTOCOL=HttpProtobuf
```

Run the Microservice under the above environment, which will disable the standard Beamable collector, and route log traffic to the collector you just created in the previous step.

```sh
dotnet run
```

In a few moments, you should see log data appear in BetterStack.

----

To configure a deployed Microservice to report log data to BetterStack, we need to start the collector in the deployed environment. The easiest way to do this is to run the collector as a local process.

The `Dockerfile` needs to be modified to include the collector in the built image. Add these lines right below the `WORKDIR /beamApp` line,

```dockerfile
# Install utilities
RUN apk add --no-cache curl tar

# Download and install OpenTelemetry Collector Contrib
RUN curl -fL -o /tmp/otelcol.tar.gz \
        https://github.com/open-telemetry/opentelemetry-collector-releases/releases/download/v0.125.0/otelcol-contrib_0.125.0_linux_amd64.tar.gz && \
    mkdir -p /otel-contrib && \
    tar -xzf /tmp/otelcol.tar.gz -C /otel-contrib && \
    rm /tmp/otelcol.tar.gz 

# Copy the local config yaml file into the image
COPY config.yaml /otel-contrib/config.yaml
```

Copy the `config.yaml` file from before and paste it next to the `Dockerfile`. 

Add the following functions to your `Program.cs` 

> [!WARNING]
> This sample hard-codes the BetterStack auth for simplicity. We are working on better solutions for secret management. Please check in soon.

```csharp

public static void SetupTelemetry()
{
	Environment.SetEnvironmentVariable("BEAM_DISABLE_STANDARD_OTEL", "1");
	Environment.SetEnvironmentVariable("BEAM_OTEL_EXPORTER_OTLP_ENDPOINT", "http://127.0.0.1:4318");
	Environment.SetEnvironmentVariable("BEAM_OTEL_EXPORTER_OTLP_PROTOCOL", "HttpProtobuf");
}

public static void SetupCollector()
{
	var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
	if (!isDocker) return;

	
	Environment.SetEnvironmentVariable("BETTERSTACK_AUTH", "Bearer REDACTED");
	Environment.SetEnvironmentVariable("BETTERSTACK_ENDPOINT", "https://REDACTED.eu-nbg-2.betterstackdata.com");

	var startInfo = new ProcessStartInfo("/otel-contrib/otelcol-contrib")
	{
		Arguments = "--config /otel-contrib/config.yaml"
	};

	Console.WriteLine("Starting collector...");
	var p = Process.Start(startInfo);

	p.Exited += (sender, args) =>
	{
		// panic! If the collector has stopped, then logs aren't getting sent anywhere. 
		//  and all we can do is explode so that AWS will re-place the task.
		Environment.Exit(1);
	};

	if (!p.Start())
	{
		// panic! If the collector did not start, then logs aren't getting sent anywhere. 
		//  and all we can do is explode so that AWS will re-place the task.
		Environment.Exit(1);

	}

	Console.WriteLine("started collector...");

}
```

And invoke those methods before starting the service.
```csharp
/// <summary>  
/// The entry point for the <see cref="BeamService"/> service.  
/// </summary>  
public static async Task Main()  
{  
    SetupTelemetry();  
    SetupCollector();  
    await BeamServer  
        .Create()  
        .IncludeRoutes<Pasta.Pasta>(routePrefix: "")  
        .RunForever();  
}
```

Deploy the service
```sh
dotnet beam deploy release 
```

> 🚧 Larger Docker Image
>
> The Docker image size will be larger than normal, because you are including the collector application. This will increase the upload time, especially the first time.

As you interact with the service, you should see logs appear in BetterStack.

This section dealt with exporting logs to BetterStack, but the situation is similar for other log providers, like DataDog. The part that needs to change is how the collector is configured.

> 👍 Replace Default Logs
>
> When you do this, you will not see logs in Beamable. This demonstration _replaces_ the Beamable logs for BetterStack. 