[Beamable Docs](https://docs.beamable.com/docs/beamable-overview)

# Beamable CLI
The Beamable CLI is a dotnet 6+ tool that manages Content, Microservices, and other Beamable systems. 

## Getting Started
To use the CLI, see our [documentation](https://docs.beamable.com/docs/cli-guide-getting-started). The tool is available as a Nuget Tool, [https://www.nuget.org/packages/Beamable.Tools](https://www.nuget.org/packages/Beamable.Tools).


# dev
You'll need dotnet core and docker installed locally.

In case you want to debug changes made in this project inside a microservice, you can run (inside the `client/` folder):
```
./setup.sh
./dev.sh
```

This will create a local feed source for the beamable nuget packages, so you can change the version of the package
reference inside your microservice's `.csproj` files and they will start using your local projects instead of the ones
published to `NuGet.Org`.

**Warning**
Changes made to the `beamable.tooling.common` are not being reflected by the use of this script. When debugging this
package, please reference the projects directly in your `csproj` files by adding the following and commenting the existing
`<PackageReference ...` to beamable content:

```
<ProjectReference Include="..\..\..\..\..\..\Packages\com.beamable\Common\beamable.common.csproj" />
<ProjectReference Include="..\..\..\..\..\..\Packages\com.beamable.server\Runtime\Common\beamable.server.common.csproj" />
<ProjectReference Include="..\..\..\..\..\..\Packages\com.beamable.server\SharedRuntime\beamable.server.csproj" />
<ProjectReference Include="..\..\..\..\..\..\..\microservice\beamable.tooling.common\beamable.tooling.common.csproj" />
<ProjectReference Include="..\..\..\..\..\..\..\microservice\microservice\microservice.csproj" />
<ProjectReference Include="..\..\..\..\..\..\..\microservice\unityEngineStubs\unityenginestubs.csproj" />
```

These are assuming your microservice lives in the default USAM folder, make changes to these paths according to
the actual location of your microservice.


# Contributing 
This project has the same [contribution policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#Contributing) as the main repository.

# License 
This project has the same [license policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#License) as the main repository.