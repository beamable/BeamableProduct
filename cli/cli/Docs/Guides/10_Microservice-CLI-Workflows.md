Managing Microservices from the CLI

## Dependencies

Before you can manage Beamable Standalone Microservices, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

## Creating New Projects

New Microservices, Storages, and Common projects can be created within an existing `.beamable` folder workspace. 

Use the `beam project new` commands to create new projects. 

```sh
beam project new service <name> # create a new Microservice
beam project new storage <name> # create a new Storage
beam project new common-lib <name> # create a new Common Library
```

All of these commands will create a new `.csproj` project and configure it to work with Beamable. The new `.csproj` will be referenced in the `.sln` file. If there is already a `.sln` file, then the first `.sln` file detected in the `.beamable` workspace will be modified to include the `.csproj` reference. If there is no `.sln`, then a file called `BeamableServices.sln` will be created. However, the `--sln` option may be given to override this behavior and specify a .`sln` file to use.

Projects will be created in the `/services` directory by default. 

## Finding Microservices

Once there is a Microservice in your `.beamable` workspace, you can check for its existence by running the [project list](doc:cli-project-list) command. It will return services detected in your workspace.

```sh
MyProject % beam project list
 {                                             
    "localServices": [                         
       {                                       
          "name": "HelloWorld",                
          "projectPath": "services/HelloWorld" 
       }                                       
    ],                                         
    "localStorages": [                         
    ]                                          
 } 
```

## Running Microservices

Microservices can be run in several ways, 
1. using the IDE,
2. using `dotnet` commands directly, or
3. using `beam` commands. 

The [project run](doc:cli-project-run) command will turn on a service.

```sh
beam project run --ids HelloWorld
```

Optionally, you can enable hot-reload by passing the `-w` flag.

## Checking Running Microservices

You can use the [project ps](doc:cli-project-ps) command to check for _running_ services. If you run the command while no services are running, the output will be empty. However, if the command is executed while a Microservice is running, then it will be displayed.

```sh
MyProject % beam project ps
HelloWorld is available prefix=[Chriss-MacBook-Pro-2] docker=[False]
```

The `prefix` in the log declares that the service is running locally, and the `docker` log declares that the service is running through dotnet, and not inside a docker container. 

Optionally, you can pass the `-w` flag to watch for changes to running services. 

## Stopping Services

If a service is running, then the [project stop](doc:cli-project-stop) command may be used to stop the program. If the service is not running, then the command will have no output. However, if the service is running, it will log a stop message. 

```sh
MyProject % beam project stop --ids HelloWorld
stopped HelloWorld.
```

When a service is stopped this way, you should expect to see a log in the Microservice itself, 

```
[Info] Stopping service through debug-server due to reason=[cli-request]
```

## Observing Logs

When a service is run, the process that starts the service should receive the log outputs. For example, if the service is run through the IDE, then the IDE should emit the logs from the service. However, it is possible to attach to the logs of a running service from a separate process. 

For example, imagine that a service is run through `dotnet` directly,

```sh
dotnet run --project services/HelloWorld
```

Then, in a separate terminal window, the [project logs](doc:cli-project-logs) command can be used to tail the logs.

```sh
MyProject % beam project logs HelloWorld
[Debug] Skipped get_ServiceProvider
[Debug] Skipped ProvideContext
...
```

