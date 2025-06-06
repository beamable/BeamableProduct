Update the Beamable CLI and workspaces

## Update the CLI

To install the latest version of the CLI, use the following command. 

```sh
dotnet beam version install latest
```

You can also search for available versions with the [beam version ls](doc:cli-version-ls) command. The [beam version install](doc:cli-version-install) command accepts any valid version instead of the "latest" string in the example above. 

> 📘 .config/dotnet-tools.json
>
> As of CLI 3.0.0, Standalone Microservice Projects use dotnet local tool installations for Beamable. The version number is stored in a special dotnet file, `.config/dotnet-tools.json`. This file should be committed to version control. Please do not edit this file directly, and prefer instead to use the `beam version` command suite. 


## Migration Guide

The Beamable CLI may include changes between versions that require developer intervention. If there are known steps that a developer must perform manually, they are documented below. The following sections describe the required updates from one version to another. 

These are ordered with the latest versions towards the top, and the older versions toward the bottom of the document. **When jumping multiple versions (A.A.A -> C.C.C), apply the migrations without skipping steps (A.A.A -> B.B.B -> C.C.C).**

### From 4.3.x to 5.0.0
The upgrade from 4.x to 5 has a few breaking changes. Once your project is using 
CLI 5+, run the following command to automatically fix known compiler errors, 
```sh
beam checks scan --fix all
```

#### `MongoDb.Driver` Upgrade to 3.0
This fix will be automatically handled by running `beam checks scan --fix all`

The `MongoDb.Diver` package must be updated from the `2.19.2` version to `3.3.0`. 
If your project does not include any storage objects,
you can ignore this step. However, if you do have storage objects, then
those projects likely include this reference in the `.csproj` files,

```xml
<PackageReference Include="MongoDB.Driver" Version="2.15.1"/>
```

If your storage object does not include direct references to the `MongoDB.
Driver`, you can simply delete this line, and the correct version will be
included automatically by referencing the `Beamable.Microservice.Runtime`
package. You can also change the version number to exactly `3.3.0`.

#### Replaced `Serilog` with `ZLogger`
This fix will be automatically handled by running `beam checks scan --fix all`

`Serilog` is a popular logging framework in the dotnet ecosystem, and Beamable used 
it extensively in earlier versions. However, with CLI 5+, Beamable migrated to 
use `ZLogger`, which is a higher performance logging tool. 

If your code uses the following types of log statements, then you will need to make a small
change, 
```csharp
Log.Info("hello world");
```

The `Log` type used to come from `Serilog`'s namespace, but since `Serilog` is no 
longer included in the package, the `Log` type will fail to compile. You should 
see a `using` statement at the top of your file, 
```csharp
using Serilog;
```

This can be replaced with the following line, and the new `Log` type will be referenced. 
```csharp
using Beamable.Server;
```

#### Game Server Federation uses new `Lobby` type
This fix will be automatically handled by running `beam checks scan --fix all`

The `IFederatedGameServer` interface used to use the `Beamable.Experimental.Api.Lobbies.Lobby` 
type, but in version 5+, it uses `Beamable.Api.Autogenerated.Models.Lobby`. 
The newer type is autogenerated, so as the Beamable Lobby API evolves over time, the
federation will receive the latest fields and data. 

The simple upgrade path is to change your `using` statement to reference the new type.
However, the access patterns into the two `Lobby` types are different enough that 
much of your method will likely need to change as well. 

A better approach is to explicitly list the `IFederatedGameServer.CreateGameServer()`'s
parameter with the new namespace, and then use a conversion tool to get back the old type. 

#### Client Generation
In the old CLI and Microservice packages, Unity and Unreal Engine client could we be automatically
generated when the Microservices were built. However, in CLI 5+, the engine integrations themselves
are responsible for generating the client code, and the default behaviour is that a standalone
Microservice project will _no longer generate client code automatically_.

It is possible to generate a unity client by hand using the following command,
```sh
dotnet beam project generate-client-oapi --output-dir ./Path/To/Your/Unity/Folder/To/Put/Clients/In
```

If you want to revert to the old method of producing client-code, you'll need to copy/paste the old target
into your services' `.csproj` files. You can find the target here,
[CLI 4.3.0 .Targets File](https://github.com/beamable/BeamableProduct/blob/cli-4.3.0/microservice/microservice/Targets/Beamable.Microservice.Runtime.targets#L15)

```csharp
public Promise<ServerInfo> CreateGameServer(Beamable.Api.Autogenerated.Models.Lobby autoGeneratedlobby) 
{
	var lobby = autoGeneratedlobby.ConvertLobbyType();
	// rest of method
}
```

#### Content CLI Commands
The old version of the CLI had a set of content commands, and they have been
dramatically refactored and redesigned in CLI 5. If you had been using content 
commands from the CLI, please reach out to Beamable for guidance. 

### From 3.0.1 to 4.0.0
The upgrade from 3.0.x to 4.0.0 is relatively simple compared to other major 
releases. 

#### Removed `net6.0` and `net7.0` support
Unfortunately, `net6.0` and `net7.0` have reached their [End-Of-Life phases](https://devblogs.microsoft.com/dotnet/dotnet-6-end-of-support/). 
The CLI 4.0 release officially drops Beamable support for these EOL dotnet 
versions. As such, when you update your projects, you must update your
`.csproj` files to use `net8.0`. 

In all your `.csproj` files, find the line with the `<TargetFramework>` 
declaration, 

```xml
<TargetFramework>net6.0</TargetFramework>
```

And update it to
```xml
<TargetFramework>net8.0</TargetFramework>
```

> 📘 Update Dotnet SDK
>
> As of CLI 4.0.0, you must have dotnet8 SDK installed on your development 
> machines, instead of the dotnet6 SDK. 

#### `MongoDb.Driver` package vulnerability 
Previous versions of Beamable relied on version 2.15.1 of the `MongoDb.
Driver` nuget package. If your project does not include any storage objects, 
you can ignore this step. However, if you do have storage objects, then 
those projects likely include this reference in the `.csproj` files, 

```xml
<PackageReference Include="MongoDB.Driver" Version="2.15.1"/>
```

If your storage object does not include direct references to the `MongoDB.
Driver`, you can simply delete this line, and the correct version will be 
included automatically by referencing the `Beamable.Microservice.Runtime` 
package. You can also change the version number to exactly `2.19.2`.  

#### Modify the `.dockerignore` file if you have one
In your service projects, if you have a `dockerignore` file, add the 
following line to the end of the file. 
```
!**/beamApp
```

### From 2.0.2 to 3.0.1
The upgrade from 2.0.x to 3.0.1 brings a few critical updates to the `csproj` file, how the Beam CLI tool is managed, and the version of `dotnet`. 

**To start this process, let's open a terminal and navigate to the directory containing your `.beamable` folder. All commands are written as though invoked from this directory.**

```shell
# In this file structure...
SomeDrive
|---ProjectRoot
|---|--- .beamable

# You should make sure you're in the directory containing the .beamable folder
cd SomeDrive/ProjectRoot
```

#### CLI File Structure
Starting with CLI 3.0.1, you should start by updating the CLI's file structure. The steps required are defined below:

1. Install [dotnet 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) in your machine (it is the new recommended version). The old `net6.0` framework's end-of-life arrived on November 12, 2024. 
2. Delete the `.beamable/local-services-manifest.json` file. (It is no longer necessary)

Previous to 3.0.0, the CLI was always installed globally and all Beamable CLI projects on your computer had to share the same CLI version. You could un-install & re-install specific versions when switching projects, but that is a bad workflow --- so... we changed it.

In 3.0.0, the CLI should be installed as a _local dotnet tool_.

The steps to change your project from using the global CLI tool to a *local dotnet tool* are defined below.

Start by verifying you are in the correct location by running the following command.
```sh
cat .beamable/connection-configuration.json 
{
  "host": "https://api.beamable.com",
  "cid": "857238240682",
  "pid": "DE_58923576234234"
}
```

Next, run `dotnet tool install --create-manifest-if-needed beamable.tools --version 3.0.1`. This should create a file in a top level `.config/dotnet-tools.json` with the following contents.
```json
{  
  "version": 1,  
  "isRoot": true,  
  "tools": {  
    "beamable.tools": {  
      "version": "3.0.1",  
      "commands": [  
        "beam"  
      ],  
      "rollForward": false  
    }  
  }}
```


Finally, to verify that the tool is installed locally, run the following, 
```sh
dotnet beam version
 {                                                
    "version": "3.0.1",               
    "location": "/usr/local/share/dotnet/dotnet", 
    "type": "LocalTool",                          
    "templates": "3.0.1"
 }   
```

From here on out, if you want to use the project specific CLI run `dotnet beam` instead of `beam`. 

However, if you run `beam` in the context of a local project _and_ the global version of your CLI is different than the local project version, the command will be automatically forwarded to the local version. This does add some latency so prefer `dotnet beam` whenever you can.

You will see a warning message similar to this when invoking `beam` directly:
```
You tried using a Beamable CLI version=[3.0.1] which is different than the one configured in this project=[3.0.0-PREVIEW.RC2]. We are forwarding the command (beam 
--pretty version) to the version the project is using via dotnet=[dotnet]. Instead of relying on this forwarding, please 'dotnet beam' from inside the project directory.
```

#### Updating the `.csproj` Files
The next step in this migration is to fix up the `.csproj` files for your microservices. The new `.csproj` file structure comes with a few new things:

- It will version-lock the package versions to the currently local CLI version (the one inside `.config/dotnet-tools.json`).
	- This means updating the CLI is just changing that version number.
	- You can manually edit this to dodge the version-lock if you want to risk it.
- It includes a Roslyn Static Analyser to help you out with microservices and federation implementations.
- It'll target `.net8`.

In every `csproj` file for **Microservices**, **MicroStorages**, and **Common Libraries**, follow the steps below:

 First, add a `PropertyGroup` with a `Label` called `Beamable Version` that looks like this:
```xml
<!-- These are special Beamable parameters that we use to keep the beamable packages in-sync to the CLI version your project is using. -->  
<!-- This makes it so your microservices are auto-updated whenever you update the CLI installed in your project. -->  
<PropertyGroup Label="Beamable Version" Condition="$(DOTNET_RUNNING_IN_CONTAINER)!=true">  
  <DotNetConfigPath Condition="'$(DotNetConfigPath)' == ''">$([MSBuild]::GetDirectoryNameOfFileAbove("$(MSBuildProjectDirectory)/..", ".config/dotnet-tools.json"))</DotNetConfigPath>  
  <DotNetConfig Condition="'$(DotNetConfig)' == ''">$([System.IO.File]::ReadAllText("$(DotNetConfigPath)/.config/dotnet-tools.json"))</DotNetConfig>  
  <!-- Extracts the version number from the first tool defined in 'dotnet-tools.json' that starts with "beamable". -->  
  <BeamableVersion Condition="'$(BeamableVersion)' == ''">$([System.Text.RegularExpressions.Regex]::Match("$(DotNetConfig)", "beamable.*?\"([0-9]+\.[0-9]+\.[0-9]+.*?)\",", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace).Groups[1].Value)</BeamableVersion>  
  <!-- When running from inside docker, this gets injected via the Dockerfile at build-time. -->  
</PropertyGroup>
```

If the project is targeting `net6.0` or `net7.0`, then, we recommend you 
upgrade the `TargetFramework` to `.net8.0`.
```xml
<PropertyGroup Label="Dotnet Settings">  
  <!-- net8.0 is the LTS version until 2026. To update your net version, update the <TargetFramework> when Beamable announces support. -->  
  <TargetFramework>net8.0</TargetFramework>  
</PropertyGroup>
```

> 📘 Don't update `netstandard2.1` to `net8.0`
>
> Be careful not to update common projects from `netstandard2.1` to `net8.0`.
> The `netstandard2.1` target produces `.dll` files that can be copied 
> directly into a wide variety of engines, such as Unity. However, `net8.0` 
> binaries are not compatible with Unity. 


##### Microservices
For every Microservice project replace all `PackageReference` elements that reference a `Beamable.` with the following two references:
```xml
<PackageReference Include="Beamable.Microservice.Runtime" Version="$(BeamableVersion)" />  
<PackageReference Include="Beamable.Microservice.SourceGen" Version="$(BeamableVersion)" OutputItemType="Analyzer" />  
```

Then, also add the following to the `Beamable Settings` `Property Group`:
```xml
<PropertyGroup Label="Beamable Settings">  
  <!-- All Microservices must have the value, "service" -->  
  <BeamProjectType>service</BeamProjectType>          
</PropertyGroup>
```
##### MicroStorages
For every MicroStorage project, replace all `PackageReference` elements that reference a `Beamable.` with the following reference:
```xml
<PackageReference Include="Beamable.Microservice.Runtime" Version="$(BeamableVersion)" />
```

Then, add the following to the `Beamable Settings` `Property Group`. Please replace `MyStorage` with your `csproj`'s file name.
```xml
<BeamProjectType>storage</BeamProjectType>  
<!-- When the Storage Object is running locally in Docker, these volume names are used to persist data between container restart events. -->  
<MyStorageDockerDataVolume>beamable_storage_MyStorage_data</MyStorageDockerDataVolume>  
<MyStorageDockerFilesVolume>beamable_storage_MyStorage_files</MyStorageDockerFilesVolume>
```

##### Common Libraries
For every Common Library project, replace all `PackageReference` elements that reference a `Beamable.` with the following reference:
```xml
<PackageReference Include="Beamable.Common" Version="$(BeamableVersion)" />
```

#### Microservice Code Changes - `partial` and `FederationId` 

With the introduction of the `Beamable.Microservice.SourceGen` library, all Microservice classes must be marked with the `partial` keyword. This will allow the source-generator to add custom implementations to Microservices in future releases. 

If you use any Federated endpoints as part of your Microservices, there a few code-changes you'll have to make:

- Replace all `IThirdPartyCloudIdentity` with `IFederationId`.
- Add a `FederationId` attribute to the class `IFederationId` --- the `UniqueName` is the property.
- If you were ever accessing the `UniqueName` property as part of your code, you'll need to replace those calls with `GetUniqueName()`.

Once these are in, try to compile your services. The newly referenced Roslyn Static Analyzer should tell you if you made any mistakes.

Finally, the analyzer will inform you that the federations you have in code are not in the `federation.json` file. The error will look something like this:
```log
Error BEAM_FED_O001 : Missing declared Federation in MicroserviceFederationsConfig. Microservice=SteamDemo, Id=steam, Interface=IFederatedLogin. Please add this Id by running `dotnet beam fed add SteamDemo steam IFederatedLogin` from your project's root directory. Or remove the IFederatedLogin that references steam  interface from the SteamDemo Microservice class.
```

You can run the command described in the error message to register the federation in the code with the `federation.json` file.

> 📘 Why is this needed?
>
> We now support the ability to test federations locally (which was previously impossible due to architecture of 2.0.0). With this new ability, some UX requirements changed for our engine integrations. This change helps the development experience of such cases in the Unity/Unreal editor integrations.

#### Updating the `Dockerfile` Files
This is very simple: simply replace the contents of each Dockerfile with the following. After replacing it, you can re-add any previous modifications you might've had.

Make sure that the version in this line  `ARG BEAM_DOTNET_VERSION="8.0-alpine"` matches the `.net` version in the `.csproj` file.

```Dockerfile
ARG BEAM_DOTNET_VERSION="8.0-alpine"  
FROM mcr.microsoft.com/dotnet/runtime:${BEAM_DOTNET_VERSION}  
  
# These args are provided by the Beam CLI  
  
# Declares the relative path from the beamable workspace to the pre-build support binaries for BeamService  
#  Normally, this will be /services/BeamService/bin/beamApp/support  
ARG BEAM_SUPPORT_SRC_PATH  
  
# Declares the relative path from the beamable workspace to the pre-built binaries for BeamService  
#  Normally, this will be /services/BeamService/bin/beamApp/app  
ARG BEAM_APP_SRC_PATH  
  
# Declares where the built application will exist inside the Docker image.  
#  This value is usually /beamApp/BeamService  
ARG BEAM_APP_DEST  
  
# <beamReserved> Beamable may inject custom settings into this Dockerfile. Please do not remove these lines. # </beamReserved>  
  
# /beamApp is the directory that will hold the application  
WORKDIR /beamApp  
  
# expose the health port  
EXPOSE 6565   
# copy general supporting files  
COPY $BEAM_SUPPORT_SRC_PATH .  
  
# copy specific application code  
COPY $BEAM_APP_SRC_PATH .  
  
# ensure that the application is runnable  
RUN chmod +x $BEAM_APP_DEST  
ENV BEAM_APP=$BEAM_APP_DEST  
  
# when starting the container, run dotnet with the built dll  
ENTRYPOINT "dotnet" $BEAM_APP  
  
# Swap entrypoints if the container is exploding and you want to keep it alive indefinitely so you can go look into it.  
#ENTRYPOINT ["tail", "-f", "/dev/null"]
```


### From 1.19.22 to 2.0.1

#### CLI File Structure

The `.beamable` folder structure changes between the major versions 1, and 2. 
After you upgrade your global CLI to 2.0.1, run the following command in your project. This command should automatically perform some of the required upgrade steps.

```sh
beam config
```

If you forgot to the run the command, or would like to verify that the upgrade happened correctly, follow the bullets below.

- The `.beamable/beamoLocalManifest.json` file should no longer exist. 
- The `.beamable/beamoLocalRuntime.json` file should no longer exist.
- The `.beamable/config-defaults.json` file should no longer exist.
- The `.beamable/user-token.json` file should no longer exist. 

Instead, you should expect to see (at least), 
- `.beamable/connection-configuration.json` _(this replaces the old `config-defaults` file. )_
- `.beamable/temp/connection-auth.json` _(this replaces the old `user-token` file)_

#### `.csproj` Files

##### SDK Version

Unfortunately, the upgrade flow between major version 1 and 2 does not automatically upgrade the nuget dependency on Beamable. All of the `.csproj` files you may have will need to be manually upgraded to Beamable 2.0.1. Remember, every service, common library, and storage have their own `.csproj` files. 

Open each `csproj` file, and find the `<PackageReference>` for Beamable. 
For a service, it will likely look like this, 
```xml
<PackageReference Include="Beamable.Microservice.Runtime" Version="1.19.22" />
```

For a storage, it may (unfortunately) look like this
```xml
<PackageReference Include="Beamable.Microservice.Runtime" Version="1.15.0-PREVIEW.RC1" />
```

And for a common library, 
```xml
<PackageReference Include="Beamable.Common" Version="1.19.22" />
```

In all these cases, please update the `Version` to `2.0.1`. 

##### Structure

In addition, different project types have the following upgrade requirements...

###### Services

For services, the `csproj` file has been simplified between major versions 1 and 2. You can remove all of the tasks and extraneous nuget references. 

This snippet can (and should) be removed from a 1.19.22 service's `csproj` file.
```xml
  
<PackageReference Include="EmbedIO" Version="3.4.*" />  
<PackageReference Include="LoxSmoke.DocXml" Version="3.4.*" />  
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="6.0.0-preview.6.21352.12" />  
<PackageReference Include="Microsoft.OpenApi" Version="1.6.3" />  
<PackageReference Include="Microsoft.OpenApi.Readers" Version="1.3.2" />  
<PackageReference Include="NetMQ" Version="4.0.1.11" />  
<PackageReference Include="Newtonsoft.Json" Version="13.0.*" />  
<PackageReference Include="Serilog" Version="2.10.*" />  
<PackageReference Include="Serilog.Formatting.Compact" Version="1.1.*" />  
<PackageReference Include="Serilog.Sinks.Console" Version="3.1.*" />  
<PackageReference Include="System.CommandLine" Version="2.0.0-beta3.22114.*" />  
<PackageReference Include="System.ServiceModel.Primitives" Version="4.9.*" />  
<PackageReference Include="System.Threading.RateLimiting" Version="7.0.0" />
```

Also, all of these targets can be removed, 
```xml
  
<!-- After the build completes, we can open the local swagger page to make it easy to test endpoints -->  
<Target Name="open-swagger" AfterTargets="Build" Condition="$(OpenLocalSwaggerOnRun)==true AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">  
    <Message Text="Opening local swagger docs..." Importance="high" />  
    <Exec Command="$(BeamableTool) project open-swagger $(AssemblyName)" />  
</Target>  
  
<!-- After the build completes, we should auto-generate client code to any linked projects -->  
<Target Name="generate-client" AfterTargets="Build" Condition="$(GenerateClientCode)==true AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">  
    <Message Text="Generating client files..." Importance="high" />  
    <Exec Command="$(BeamableTool) project generate-client $(OutDir)/$(AssemblyName).dll --output-links" />  
</Target>  
  
<!-- Before starting the build, we need to prepare a few files and an .env file to pass startup information to the service -->  
<Target Name="setup-beamable" BeforeTargets="Build" DependsOnTargets="RunResolvePackageDependencies" Condition="$(DOTNET_RUNNING_IN_CONTAINER)!=true">  
  
    <PropertyGroup>        <BeamableVersion>@(BeamablePackage->'%(Version)')</BeamableVersion>  
    </PropertyGroup>    <!-- We need a file that lets the runtime know what version of Beamable it was built with... -->  
    <Message Text="Creating beamable version file..." Importance="high" />  
    <WriteLinesToFile File="$(OutDir)/.beamablesdkversion" Lines="$(BeamableVersion)" Overwrite="true" />  
</Target>  
  
<!-- When running in a container, before building, we need to prepare a few files -->  
<Target Name="docker-setup-beamable" BeforeTargets="Build" DependsOnTargets="RunResolvePackageDependencies" Condition="$(DOTNET_RUNNING_IN_CONTAINER)==true">  
  
    <PropertyGroup>        <BeamableVersion>@(BeamablePackage->'%(Version)')</BeamableVersion>  
    </PropertyGroup>    <Message Text="Generating files..." Importance="high" />  
    <WriteLinesToFile File="$(PublishDir)/.beamablesdkversion" Lines="$(BeamableVersion)" Overwrite="true" />  
    <WriteLinesToFile File="$(PublishDir)/.env" Lines="BEAMABLE_SDK_VERSION_EXECUTION=$(BeamableVersion)" Overwrite="true" />  
</Target>
```

The following property group section can be dramatically simplified. The only required properties from the following snippet are, 
1. `GenerateClientCode`, and 
2. `TargetFramework`

```xml
<!--  Settings for Beamable Build  -->  
<PropertyGroup>  
    <!-- The tool path for the beamCLI. "dotnet beam" will refer to the local project tool, and "beam" would install to a globally installed tool -->  
    <BeamableTool>beam</BeamableTool>  
  
    <!-- When "true", this will open a website to the local swagger page for the running service -->  
    <OpenLocalSwaggerOnRun>false</OpenLocalSwaggerOnRun>  
  
    <!-- When "true", this will auto-generate client code to any linked unity projects -->  
    <GenerateClientCode>true</GenerateClientCode>  
</PropertyGroup>  
  
<PropertyGroup Condition="$(DOTNET_RUNNING_IN_CONTAINER)!=true">  
    <DefineConstants>$(DefineConstants);BEAMABLE_GENERATE_ENV</DefineConstants>  
</PropertyGroup>  
  
<!-- Standard dotnet settings-->  
<PropertyGroup>  
    <OutputType>Exe</OutputType>  
  
    <!-- As of Beamable 1.0, net6.0 is required. -->  
    <TargetFramework>net6.0</TargetFramework>  
    <!-- Advanced C# Features are disabled by default. The Unity SDK does not support these features.   
         If you enable them, it will be harder to copy/paste code between the service and Unity. -->  
    <ImplicitUsings>disable</ImplicitUsings>  
    <Nullable>disable</Nullable>  
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>  
  
    <!-- Warning 1591 is about missing XML comments on methods. Beamable suggests disabling this warning.   
         https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1591 -->  
    <NoWarn>1591</NoWarn>  
  
    <!-- The autogenerated OpenAPI page will use the generated serviceDocs.xml file to handle   
         API descriptions. The OpenAPI page will break if there is no `serviceDocs.xml` file. -->  
    <GenerateDocumentationFile>true</GenerateDocumentationFile>  
</PropertyGroup>
```

However, there is one *new* property that is **REQUIRED**. You must add the following property, 
```xml
<BeamProjectType>service</BeamProjectType>
```

After all the edits, you should have a `csproj` file that looks similar to the following. This is the `csproj` file that is generated for a new service using CLI 2.0.1.
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
        <PackageReference Include="Beamable.Microservice.Runtime" Version="2.0.1" />  
    </ItemGroup>  
</Project>
```

###### Common Libraries

Similar to services, the `csproj` file for common libraries has been simplified between major versions 1 and 2. 

You should remove this target, 
```xml
<!-- Move the built dll to the linked projects -->  
<Target Name="share-code" AfterTargets="Build" Condition="$(CopyToLinkedProjects)==true AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">  
    <Message Text="Generating code for other projects" Importance="high" />  
    <Exec Command="$(BeamableTool) project share-code $(OutDir)/$(AssemblyName).dll --dep-prefix-blacklist Newtonsoft,Unity.Beamable,UnityEngine,Unity.Addressables,System" />  
</Target>
```

And from the following properties the only two that you need are, 
1. `CopyToLinkedProjects`, and
2. `TargetFramework`

```xml
  
<PropertyGroup>  
    <!-- Unity 2021 can handle netstandard2.1 libraries -->  
    <TargetFramework>netstandard2.1</TargetFramework>  
    <GenerateDocumentationFile>true</GenerateDocumentationFile>  
</PropertyGroup>  
  
<!--  Settings for Beamable Build  -->  
<PropertyGroup>  
    <!-- The tool path for the beamCLI. "dotnet beam" will refer to the local project tool, and "beam" would install to a globally installed tool -->  
    <BeamableTool>beam</BeamableTool>  
  
    <!-- When "true", this will copy the built project and associated dependencies to linked Unity projects -->  
    <CopyToLinkedProjects>true</CopyToLinkedProjects>  
</PropertyGroup>  
  
<!-- Make sure that the built dlls and their dependencies are in the output directory -->  
<PropertyGroup>  
    <ProduceReferenceAssemblyInOutDir>true</ProduceReferenceAssemblyInOutDir>  
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>  
    <PublishDocumentationFile>true</PublishDocumentationFile>  
</PropertyGroup>
```

When you are done with these edits, your `csproj` file should appear similar to the following snippet. Here is the `csproj` file for a common library created with CLI 2.0.1.
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- Unity 2021 can handle netstandard2.1 libraries -->
        <TargetFramework>netstandard2.1</TargetFramework>
    </PropertyGroup>

    <!--  Settings for Beamable Build  -->
    <PropertyGroup>
        <!-- When "true", this will copy the built project and associated dependencies to linked Unity projects -->
        <CopyToLinkedProjects>true</CopyToLinkedProjects>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="Beamable.Common" Version="2.0.1" />
    </ItemGroup>
    
</Project>

```


#### Dockerfiles

Any service you created in 1.19.22 will have a `Dockerfile`. These files need some manual edits to make them compatible with 2.0.1. 

Select all the lines _between_ the first `FROM` command, and the `RUN dotnet publish` command, and _replace_ them with the following.

```Dockerfile  
# <BEAM-CLI-COPY-ENV> this line signals the start of environment variables copies into the built container. Do not remove it. This will be overwritten every time a variable changes in the execution of the CLI.  
  
# </BEAM-CLI-COPY-ENV> this line signals the end of environment variables copies into the built container. Do not remove it.  
  
# <BEAM-CLI-COPY-SRC> this line signals the start of Beamable Project Src copies into the built container. Do not remove it. The content between here and the closing tag will change anytime the Beam CLI modifies dependencies.  
  
# </BEAM-CLI-COPY-SRC> this line signals the end of Beamable Project Src copies. Do not remove it.  
  
# build the dotnet program  
WORKDIR /
```

Next, select the lines starting with (and _including_) `RUN dotnet publish` until the line (but not including) `ENTRYPOINT` , and _replace_ them with the following, 
```Dockerfile
RUN dotnet publish ${BEAM_CSPROJ_PATH} -c release -o /beamApp  
  
# use the dotnet runtime as the final stage  
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine  
WORKDIR /beamApp  
  
# expose the health port  
EXPOSE 6565   
# copy the built program  
COPY --from=build-env /beamApp .
```

Finally, on the last line (the `ENTRYPOINT`), replace the `/subapp` with `/beamApp`

Here is a Dockerfile that was adapted from 2.0.1. There are two important things to note, 
1. this file is for a service called `Example3`, which justifies the `ENTRYPOINT`, and
2. when you run `beam services run`, the CLI will _inject_ content into the file between on the `BEAM-CLI-` tags. After the command runs, you should see `ENV`, `RUN`, and `COPY` statements between the beamable tags. This is how the `${BEAM_CSPROJ_PATH}` reference will be resolved. 

```Dockerfile
# use the dotnet sdk as a build stage  
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build-env  
  
# <BEAM-CLI-COPY-ENV> this line signals the start of environment variables copies into the built container. Do not remove it. This will be overwritten every time a variable changes in the execution of the CLI.  
  
# </BEAM-CLI-COPY-ENV> this line signals the end of environment variables copies into the built container. Do not remove it.  
  
# <BEAM-CLI-COPY-SRC> this line signals the start of Beamable Project Src copies into the built container. Do not remove it. The content between here and the closing tag will change anytime the Beam CLI modifies dependencies.  
  
# </BEAM-CLI-COPY-SRC> this line signals the end of Beamable Project Src copies. Do not remove it.  
  
# build the dotnet program  
WORKDIR /  
  
RUN dotnet publish ${BEAM_CSPROJ_PATH} -c release -o /beamApp  
  
# use the dotnet runtime as the final stage  
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine  
WORKDIR /beamApp  
  
# expose the health port  
EXPOSE 6565   
# copy the built program  
COPY --from=build-env /beamApp .  
  
# when starting the container, run dotnet with the built dll  
ENTRYPOINT ["dotnet", "/beamApp/Example3.dll"]  
```

#### Microservice .config folders

Finally, the `.config` folder under each Microservice folder should be deleted. This file is cruft, and is no longer needed. 

Also, remember that if your Microservice is referencing a Storage object, you must have Docker running; otherwise the Microservice will not start correctly.  