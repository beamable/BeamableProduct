Debug and validate your Microservice

## Dependencies

Before you can debug Beamable Standalone Microservices, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

In order to debug a Microservice, you also need to have a local `.beamable` workspace with a Beamable Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init MyProject
cd MyProject
dotnet beam project new service HelloWorld
```

## Debugging 

Beamable Standalone Microservices run as vanilla dotnet processes on your machine. That means you can use your favorite IDE to debug the service. You can attach breakpoints and then start the service with Debugging, or attach to existing process using your IDE. 

![A breakpoint in a microservice](https://files.readme.io/88da124-image.png)

## Hot Reload

If you run your service with hot reload enabled, then your code will be recompiled automatically as you make changes to your code, and your running service won't need to restart. 

Unfortunately, JetBrains Rider has limited hot reload support out of the box. With that in mind, this guide will assume that if you want to use hot reload, you'll be using the CLI. 

Not all types of source code changes will work with hot reload. Only changes to the `.cs` files will be acceptable. No changes to the `.csproj` or other build-time configuration options will be reloaded.  

### Dotnet CLI

You can run your service with hot reload by using the vanilla dotnet CLI.

```sh
 MyProject % dotnet watch --project ./services/HelloWorld 
dotnet watch 🔥 Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.
  💡 Press "Ctrl + R" to restart.
dotnet watch 🔧 Building...
  Determining projects to restore...
  All projects are up-to-date for restore.
  HelloWorld -> /MyProject/services/HelloWorld/bin/Debug/net6.0/HelloWorld.dll
  Creating beamable version file...
  Generating client files...
dotnet watch 🚀 Started
```
Once you make an edit to your source code, you should see some logs that indicate the change. 

```
dotnet watch ⌚ File changed: ./services/HelloWorld/HelloWorld.cs.
dotnet watch 🔥 Hot reload of changes succeeded.
```
### Beam CLI

The Beam CLI no longer supports hot reload natively. It was supported in 2.0.1, but due to technical limitations, support was removed in 2.1.0 
