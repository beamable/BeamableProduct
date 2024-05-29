Using Storage Objects with Microservices

## Dependencies

Before you can use Beamable Storage Objects, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

In order to use a Storage Object, you also need to have a local `.beamable` workspace with a Beamable Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init MyProject
cd MyProject
beam project new service HelloWorld
```

## Storage Objects

A Storage Object is a Mongo database. Beamable will host and manage a database on your behalf when you deploy your project. Locally, the Beam CLI creates a local mongo database inside a Docker container. Beamable never installs mongo directly on your host machine. 

To create a Storage Object, use the [project new storage](doc:cli-project-new-storage) command. 

```sh
beam project new storage
```

This creates a new `.csproj` in your existing `.sln`. You will also be prompted to assign a reference between the Storage Object and some Microservices. A Storage Object cannot exist independently of a Microservice. When you specify a dependency between a Storage Object and a Microservice, Beamable knows to pass the connection credentials to the Microservice. 

When developing locally, anytime a Microservice that depends on a Storage Object starts, it will make sure the Docker container for the Storage Object is running. If there is no running container, it will start a container. This can take several seconds and it may appear the service is failing to start. 

The [code documentation](doc:microservice-storage-code) explains how to interact with a Storage Object. 

You can also use the [project open-mongo](doc:cli-project-open-mongo) command to open a local instance of Mongo Express to inspect your local developer data. 