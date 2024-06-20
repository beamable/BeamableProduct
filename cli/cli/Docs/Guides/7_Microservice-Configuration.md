Configuring a Standalone Microservice

## Dependencies

Before you can configure Beamable Standalone Microservices, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

In order to configure a Microservice, you also need to have a local `.beamable` workspace with a Beamable Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init MyProject
cd MyProject
beam project new service HelloWorld
```

## Project Configuration 

Each Standalone Microservice is a dotnet project, and can be configured through the dotnet `.csproj`. In most IDEs, the `.csproj` file will be hidden automatically, but you can open it by right-clicking on the project in the IDE and opening the `.csproj` file. As of Beam CLI 2.0, the starting `.csproj` has the following structure. 

```xml
<Project Sdk="Microsoft.NET.Sdk">  
    <PropertyGroup Label="Beamable Settings">  
        <!-- All Microservices must have the value, "service" -->  
        <BeamProjectType>service</BeamProjectType>  
  
        <!-- When "true", this will auto-generate client code to any linked unity projects -->  
        <GenerateClientCode>true</GenerateClientCode>  
    </PropertyGroup>  
    <PropertyGroup Label="Dotnet Settings">  
        <!-- net8.0 is the LTS version until 2026. To update your net version, update the <TargetFramework> when Beamable announces support. -->  
        <TargetFramework>net6.0</TargetFramework>  
    </PropertyGroup>  
    <ItemGroup Label="Nuget References">  
        <PackageReference Include="Beamable.Microservice.Runtime" Version="2.0.0" />  
    </ItemGroup>  
</Project>
```

### Code Dependencies

Dotnet projects use a tool called _Nuget_ to manage dependencies on other code libraries. Each `<PackageReference>` node within an `<ItemGroup>` element declares a Nuget dependency. By default, every Standalone Microservice requires the [Beamable Microservice Nuget Package](https://www.nuget.org/packages/Beamable.Microservice.Runtime). However, you can add whatever packages you require as well. 


### Beamable Properties

Dotnet uses a tool called `msbuild` to compile your code into executable files. When the build happens, `msbuild` accesses various XML based properties within the `<PropertyGroup>` elements of the `.csproj` file. 

#### BeamProjectType

The `<BeamProjectType>` is an enum string that declares the containing `.csproj` to be either a Microservice, or a Storage object. If a `.csproj` does not declare the `<BeamProjectType>` property, then it will not be detected by the beam CLI as a valid Microservice or Storage object. 

| Property Name       | Default Value               |
| ------------------- | --------------------------- |
| `<BeamProjectType>` | _there is no default value_ |

Valid values include, 

| Value     | Description                                          |
| --------- | ---------------------------------------------------- |
| `service` | Declares the project to be a Standalone Microservice |
| `storage` | Declares the project to be a Storage Object          |

#### BeamId

The `<BeamId>` controls the name of the Beamable project. 

| Property Name | Default Value             |
| ------------- | ------------------------- |
| `<BeamId>`    | _the name of the .csproj_ |

#### BeamEnabled

The `<BeamEnabled>` is a boolean property. When `false`, when services are [deployed](doc:cli-guide-microservice-deployment) , the service will not be enabled, and will not cost Beamable Cloud resources.

| Property Name   | Default Value |
| --------------- | ------------- |
| `<BeamEnabled>` | true          |

#### GenerateClientCode 

The `<GenerateClientCode>` property is boolean property, only valid on Standalone Microservice projects. When the project is built, if there are any linked Unity or Unreal projects, client code may be generated for the engine client and placed in the linked project folders. In order to link a project, use the [add-unity-project](doc:cli-add-unity-project) command, or the [add-unreal-project](doc:cli-add-unreal-project) command. 

| Property Name          | Default Value |
| ---------------------- | ------------- |
| `<GenerateClientCode>` | true          |

#### BeamableTool

The `<BeamableTool>` property is the path to the Beam CLI program that the Standalone Microservice will use to do various tasking. For most cases, this is configured to be the globally installed `beam` tool. However, if any dotnet build tasks run with a `BEAM_PATH` environment variable, then the `BEAM_PATH` environment variable will set the `<BeamableTool>` value. 

Generally, it is not advised to overwrite this setting. However, it you install the beam CLI as a local dotnet tool in the project, it would be valid to overwrite the `<BeamableTool>` property as `dotnet beam`. 

This property is accessible to the dotnet build targets, including any custom targets you create. 

| Property Name    | Default Value                                |
| ---------------- | -------------------------------------------- |
| `<BeamableTool>` | the value of env var, `BEAM_PATH`, or `beam` |

#### BeamServiceGroup
The `<BeamServiceGroup>` property is a comma or semi-colon separated list of tags that logically group services together. 
If you need to specify multiple values, use a `,` or a `;` to separate values. You can also redefine the property in terms of itself.

```xml
<BeamServiceGroup>firstTag</BeamServiceGroup>
<BeamServiceGroup>$(BeamServiceGroup);secondTag</BeamServiceGroup>
```

| Property Name        | Default Value |
|----------------------|---------------|
| `<BeamServiceGroup>` | empty         |

### Dotnet Properties

Common Dotnet properties may be explored through [Dotnet's Documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties?view=vs-2022) . However, there are a few properties that are set automatically through the usage of the [Beamable Microservice Nuget Package](https://www.nuget.org/packages/Beamable.Microservice.Runtime). To view these default settings, you should view the package source code's `.props` file, located here, 

[https://github.com/beamable/BeamableProduct/blob/cli-2.0.0/microservice/microservice/Targets/Beamable.Microservice.Runtime.props](https://github.com/beamable/BeamableProduct/blob/cli-2.0.0/microservice/microservice/Targets/Beamable.Microservice.Runtime.props)

> 📘 Make sure to reference the right version!
>
> The link above points to the cli-2.0.0 release tag version of the source code. Make sure that you are looking the same version as your `Beamable.Microservice.Runtime` nuget version is using in the `.csproj`. 
 
Other than the default properties set in the `.props` file, a major requirement of Beamable Standalone Microservices is that they build for `net6.0`. This is because `net6.0` was the last major version of dotnet that supported native cross compilation for x86 and ARM CPU architectures. As of Beam CLI 2.0, the `<TargetFramework>` should be `net6.0`. 

