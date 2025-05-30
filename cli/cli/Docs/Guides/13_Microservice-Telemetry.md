Calling a function on a Microservice
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

### Standard Log Attributes

| Name                                    | Description                                                                                    | Conditions                                             | Standard Set | LEVEL   | 
|-----------------------------------------|------------------------------------------------------------------------------------------------|--------------------------------------------------------|--------------|---------|
| id                                      | A unique id for the log message                                                                |                                                        | OTEL         |         |
| service.instance.id                     | A unique id for the process sending data                                                       |                                                        | OTEL         |         |
| service.name                            | The fully qualified name of the service                                                        |                                                        | OTEL         |         |
| service.namespace                       | The beamoId of the service                                                                     |                                                        | OTEL         |         |
| body                                    | The rendered contents of the log message                                                       |                                                        | OTEL         |         |
| timestamp                               | The time of the log message                                                                    |                                                        | OTEL         |         |
| severity_number                         | The numeric value for the log level. Higher numbers have more importance.                      |                                                        | OTEL         |         |
| severity_text                           | The log level for the message. "Debug", "Information", "Warning", "Error", "Critical"          |                                                        | OTEL         |         |
| span_id                                 | The id of the active span when the log was created                                             | Only included when a trace is being recorded           | OTEL         |         |
| trace_id                                | The id of the parent span when the log was created                                             | Only included when a trace is being recorded           | OTEL         |         |
| beam.source                             | "microservice" or "cli" or "unityEditor" or "unrealEditor"                                     |                                                        | BEAM         |         |
| beam.cid                                | The customer id                                                                                |                                                        | BEAM         |         |
| beam.pid                                | The realm id                                                                                   |                                                        | BEAM         |         |
| beam.owner_player_id                    | The player id of the person that started the process, or 0 if the process was started remotely | Only included when the service is running locally      | BEAM         |         |
| beam.routing_key                        | The routing key for the service                                                                | Only included when the service is running locally      | BEAM         |         |
| beam.sdk_version                        | The semantic SDK version of the beamable SDK reporting data                                    |                                                        | BEAM         |         |
| beam.connection.id                      | A UUID for the connection sending data. There may be multiple connectionIds per instanceId     | Only included after a connection has been established  | BEAM         |         |
| beam.connection.request.request_id      | The UUID of the request.                                                                       | Only included when the service is processing a request | BEAM         |         |
| beam.connection.request.conn_request_id | The id of the request. Only unique per beam.connection.id                                      | Only included when the service is processing a request | BEAM         | VERBOSE |
| beam.connection.request.player_id       | The player id of the person that started the request, or 0 if there is no associated user      | Only included when the service is processing a request | BEAM         |         |
| beam.connection.request.path            | The relative path of the `[Callable]` being invoked through a request                          | Only included when the service is processing a request | BEAM         |         |
| beam.connection.request.root_trace_id   | The top level trace id from Beamable's internal observability stack                            | Only included when the service is processing a request | BEAM         | VERBOSE |
| beam.connection.request.parent_trace_id | The most recent parent trace id from Beamable's internal observability stack                   | Only included when the service is processing a request | BEAM         | VERBOSE |

