## Regular flow for updating 

By default, in order to update dotnet cli tool it is normal to use command like:

```
dotnet tool update {name_of_tool} --global --version {version_here}
```

Which for this tool could be something like:

```
dotnet tool update Beamable.Tools --global --version 1.17.0
```

### Simpler way

In cli tool, as of 1.16.2, it is possible to use `beam version` tools to get the list of available versions:

```
beam version ls
```

The `beam version ls` command can also have arguments like  `--include-rc {true/false}` or `--include-release {true/false}` so it is possible to specify if it should print out RC versions, release ones or both of them.

Then in order to install specific version you can use the `beam version` command:

```
beam version install <version?>
```

That command will install the specified version OR the latest one if no version as argument is provided.

### Upgrading Microservices created with CLI versions <2.1.0 to 2.1.0
In order to make microservices created with versions prior to 2.1.0, you'll need to manually modify the `csproj` file.
Here's the list of things required.

#### Microservices / MicroStorages

- Ensure that the property group with a Label called `Beamable Settings` looks like this:
```xml
<PropertyGroup Label="Beamable Settings">
    <!-- All Microservices must have the value, "service" -->
    <BeamProjectType>service</BeamProjectType>
</PropertyGroup>
```
- Add a property group with a Label called `Beamable Version` that looks like this:
```xml
<!-- These are special Beamable parameters that we use to keep the beamable packages in-sync to the CLI version your project is using. -->
<!-- This makes it so your microservices are auto-updated whenever you update the CLI installed in your project. -->
<PropertyGroup Label="Beamable Version">
    <DotNetConfigPath Condition="'$(DotNetConfigPath)' == '' AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">$([MSBuild]::GetDirectoryNameOfFileAbove("$(MSBuildProjectDirectory)/..", ".config/dotnet-tools.json"))</DotNetConfigPath>
    <DotNetConfig Condition="'$(DotNetConfig)' == ''">$([System.IO.File]::ReadAllText("$(DotNetConfigPath)/.config/dotnet-tools.json"))</DotNetConfig>
    <!-- Extracts the version number from the first tool defined in 'dotnet-tools.json' that starts with "beamable". -->
    <BeamableVersion Condition="'$(BeamableVersion)' == '' AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">$([System.Text.RegularExpressions.Regex]::Match("$(DotNetConfig)", "beamable.*?\"([0-9]+\.[0-9]+\.[0-9]+)\",", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace).Groups[1].Value)</BeamableVersion>
    <!-- When running from inside docker, this gets injected via the Dockerfile at build-time. -->
</PropertyGroup>
```
- Ensure your `TargetFramework` is `net6.0`.
```xml
<!-- Standard dotnet settings-->
<PropertyGroup Label="Dotnet Settings">
    <!-- As of Beamable 1.0, net6.0 is required. -->
    <TargetFramework>net6.0</TargetFramework>
</PropertyGroup>
```
- Add an empty `PropertyGroup` with the Label `User Settings`.
```xml
<!-- Put your stuff here OR override our stuff here (some docs): https://docs.beamable.com/docs/cli-guide-microservice-configuration#beamable-properties -->
<PropertyGroup Label="User Settings">
</PropertyGroup>
```
- Update your `Beamable.Microservice.Runtime` `PackageReference`'s `Version` attribute to `$(BeamableVersion)`:
```xml
<!-- Nuget references -->
<ItemGroup>
    <!-- 
    BEAMABLE DEVELOPERS: This is how we reference samples when we are developing them. 
    This makes it so that, if we have a locally built package set up, we use that one. Otherwise, we use the version below.
    
    GAME-MAKERS: You don't need to care about this and if you want to reference packages in your own projects, you can just do it normally.
    <PackageReference Include="Beamable.Common" Version="2.0.0" />     
    -->
    <PackageReference Include="Beamable.Microservice.Runtime" Version="$(BeamableVersion)"/>

</ItemGroup>
```

- For MicroStorages, the same steps are needed except the `BeamProjectType` should be `<BeamProjectType>storage</BeamProjectType>`.

#### Common Libraries
For common libraries, you can modify your `csproj` file to look like this:
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup Label="Dotnet Settings">
        <TargetFramework>netstandard2.1</TargetFramework>
    </PropertyGroup>

    <PropertyGroup Label="Beamable Settings">
        <!-- When "true", this will copy the built project and associated dependencies to linked Unity projects -->
        <CopyToLinkedProjects>true</CopyToLinkedProjects>
    </PropertyGroup>

    <!-- These are special Beamable parameters that we use to keep the beamable packages in-sync to the CLI version your project is using. -->
    <!-- This makes it so your microservices are auto-updated whenever you update the CLI installed in your project. -->
    <PropertyGroup Label="Beamable Version">
        <DotNetConfigPath Condition="'$(DotNetConfigPath)' == '' AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">$([MSBuild]::GetDirectoryNameOfFileAbove("$(MSBuildProjectDirectory)/..", ".config/dotnet-tools.json"))</DotNetConfigPath>
        <DotNetConfig Condition="'$(DotNetConfig)' == '' AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">$([System.IO.File]::ReadAllText("$(DotNetConfigPath)/.config/dotnet-tools.json"))</DotNetConfig>
        <!-- Extracts the version number from the first tool defined in 'dotnet-tools.json' that starts with "beamable". -->
        <BeamableVersion Condition="'$(BeamableVersion)' == '' AND $(DOTNET_RUNNING_IN_CONTAINER)!=true">$([System.Text.RegularExpressions.Regex]::Match("$(DotNetConfig)", "beamable.*?\"([0-9]+\.[0-9]+\.[0-9]+)\",", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace).Groups[1].Value)</BeamableVersion>
        <!-- When running from inside docker, this gets injected via the Dockerfile at build-time. -->
    </PropertyGroup>
    
    <ItemGroup Label="Nuget References">
        <PackageReference Include="Beamable.Common" Version="$(BeamableVersion)"/>
    </ItemGroup>
</Project>
```
