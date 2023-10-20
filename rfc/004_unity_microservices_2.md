# Unity Standalone Microservices

## Summary

In Beamable SDK 2.0, Microservices will change to adopt the best practices learned from Standalone Microservices. The UI and workflow of Microservice Manager will remain mostly unchanged, but the file structure and backend implementation of the SDK will change to use Standalone Microservice concepts. Beamable users upgrading from 1.0 will be forced through a migration flow.

## Motivation

Originally, Unity Microservices were built to serve Unity game developers who may not have experience in C#/Dotnet projects outside of Unity. As such, we hid away a lot of advanced features of Dotnet. We also emposed a large design constraint on our customers that their Microservice code had to be _both_ net6 compatible, _and_ Unity compatible. We've found that customers building games quickly run up against roadblocks emposed by these constraints. 

However, Standalone Microservices are built outside of Unity, and leverage the Beam CLI. Standalone Microservices allow developers to use Dotnet natively, and to configure their own Dockerfile. Standalone Microservices fill the niche of advanced users, but are harder to approach to newer developers. 

Additionally, Beamable now maintains two separate Microservice solutions, one in Unity, and the other as Standalone.

If we can change the Unity Microservice implementation to use Standalone Microservices internally, then we can maintain just one solution, and keep Unity's implementation as a thin wrapper on top of Standalone Microservices. Standalone Microservices give Unity users the advanced capabilities currently missing. The existing Microservice Manager will keep it easy for Unity developers to manage their Microservices without needing to master the Beam CLI.


## Implementation

There are many components to implementing Standalone Microservices inside of Unity. 

- `.sln` and `.csproj` generation augumentation
- Signpost asset
- Hidden file structure
- Microservice Manager hook up
- Migration flow
- `.csproj` and `Dockerfile` generation
- SDK version tracking 
- `./beamable` folder integration
- Sharing code from Unity
- Sharing code with Unity (client generation)
- Deployments
- Logging
- Local Instance Management
- Package Capability 


### File Structure

Microservices will be in a hidden folder in Unity. It does not matter which hidden folder, and different Microservices could be in different hidden folders.

The Microservice is not visible from Unity, so a "SignPost" asset will be used to track the Microservice. We will use a JSON schema, "beamservice", for each hidden Microservice. The SignPost will contain configuration data for the Microservice. 

Additionally, the Microservices need access to a `./beamable` folder which contains information required to connect to Beamable.

For example, take 3 servers, `A`, `B`, and `C`.

```
/.beamable
    config-defaults.json
    beamoLocalManifest.json
/Assets
    A_SignPost.beamservice
    /Hidden~
        /A
/Packages
    B_SignPost.beamservice
    C_SignPost.beamservice
    /HiddenElsewhere~
        /B
        /C
```

(more on the A_SignPost.beamservice later)
(Unity treats any folder ending with a (`~`) character as hidden)

Unity will not compile hidden source code, so the contents of the service folder will be a whole Dotnet project, identical to a Standalone Microservice. 

```
/A
  AServer.cs
  Program.cs
  Dockerfile
  A.csproj
```



### Unity `.sln` Generation

Unity automatically generates a `.csproj` per assembly definition, plus a few other standard `.csproj` files for general Runtime scripts and Editor scripts. Unity also generates a `.sln` file that references all generated `.csproj` scripts. When a developer opens the C# Project from Unity, they are opening the generated `.sln` file. 

The `.sln` file will not include the Standalone Microservice `.csproj` files, which means the IDE will not treat the Standalone Microservices as valid projects. In order to achieve IDE support (running, debugging, etc), the `.sln` file needs to reference the `.csproj` file. 

This can be accomplished using a custom `AssetPostprocessor` that implements the `OnGeneratedSlnSolution` method. Here is an example, [https://gist.github.com/cdhanna/7c5b9a161d848bdb92f97bf61c783433](https://gist.github.com/cdhanna/7c5b9a161d848bdb92f97bf61c783433). 

The SDK needs to scan for all Signpost assets, and use those to inform the `AssetPostprocessor` which `.csproj` files to add references for. 


### IDE Integration 

When a developer wants to run a Microservice directly from the IDE instead of using the Microservice Manager, they will right-click on the project, and click "run". The IDE should launch the Microservice as a native dotnet application on the developer's machine. However, there are 2 major challenges introduced by this assumption...
1. The IDE needs to be aware of the custom Dotnet installation in the developer's project's `/Library/BeamableEditor/Dotnet` folder. 
2. The Microservice must have access to a `beam` CLI instance when it starts. By default, it uses the globally installed `beam` tool. However, we explicitly need the Microservice to use the CLI available in the `/Library/BeamableEditor/BeamCLI` folder, _not_ the globally installed one. 


### Signpost Assets

The Microservices in Unity will be hidden, and for Unity-first developers that will be concerning. We will create a "signpost" asset that is a custom Scripted Imported asset that sits somewhere in the Unity project (ideally next to the hidden folder) and describes a single Microservice. 


An example file name is...
`A_Signpost.beamservice`

And the contents look like
```json
{
    "name": "MyCoolService",
    "assetRelativePath": "Hidden~/A",
    "relativeDockerfile": "Dockerfile",
    "relativeProjectfile": "A.csproj"
}
```

It is critical we use a custom file type, `.beamservice` instead of using a Scriptable Object sub type, because we need to be able to reliably access this file outside of the Unity Asset Database life cycle. We should use a custom Scripted Object Importer to give Unity the ability to understand the `.beamservice` file type, and we can extend a `BeamServiceAsset` type as the main root asset type of the file. This also gives us the ability to extend the inspector for this file type. 

When the BeamEditor initializes, it calls into the Microservice Editor initialization logic. One of the first steps that must take place is the Signpost asset syncronization phase. All `.beamservice` files need to be identified in the project (including all /Package) folders, and sent to the BeamCLI to re-create the local `./beamable/localBeamoManifest.json` file. 

Later, during usage of the Microservice Manager, the contents of `localBeamoManifest.json` are used via CLI commands to populate the UI. The Microservice Manager should never interact with the `.beamservice` files directly, because that introduces a dependency beyond the CLI. 


### IDE Integration

### `.beamable` Folder Integration

When a developer has a configured Standalone Microservice `.csproj` file in their IDE, they should be able to right-click, and run it. However, part of the process of running a local Standalone Microservice is that the `Program.cs` code will use the beam CLI to generate a local `.env` file that the service will inject into its own Environment. The environment data has CID/PID/Auth information to bootstrap the Microservice. 

There will be a `.beamable` folder in the root of the Unity project. Inside the folder, there is a `beamoLocalManifest.json` file that contains details about the availalable Microservices in a project. Also, there is a `config-defaults.json` file that contains the connection strings required to start a Microservice. 

Unity will make sure these files are up to date by scanning assets in the project, and making declarative calls to the beam CLI. 

