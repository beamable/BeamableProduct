# Microservice

This is a doetnetcore library and base docker image.
A customer docker image will contain a runnable console program that invokes the types and functionality of this Microservice library.


# dev
You'll need dotnet core and docker installed locally.

Open the solution file, which should include project references to 
1. microservices
2. com.beamable.core
3. com.beamable.server

The com.beamable.core and com.beamable.server projects are physically stored in the Unity project.
They are linked through relative pathways to this solution. 

In a terminal, from the microservices project folder, run
```
./restore.sh
./build.sh
```

The `restore` command will run a dotnet restore on the Unity projects. If your IDE ever complains that system types don't exist, re-run the restore.

The `build` command will build the Unity projects, and place their dll outputs into a `/lib` folder. 
Then it will build a docker image from the microservice source code, and include the `/lib` folder dlls.

Any time you want to make a change to the base image for a microservice, re-run the build command,
and then rebuild your Unity microservice code.

In case you want to debug changes made in this project inside a microservice, you can run (inside the `client/` folder):
```
./set-pacjages.sh
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