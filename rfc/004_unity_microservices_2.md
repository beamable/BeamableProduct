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


### `.beamable` Folder Integration

When a developer has a configured Standalone Microservice `.csproj` file in their IDE, they should be able to right-click, and run it. However, part of the process of running a local Standalone Microservice is that the `Program.cs` code will use the beam CLI to generate a local `.env` file that the service will inject into its own Environment. The environment data has CID/PID/Auth information to bootstrap the Microservice. 

There will be a `.beamable` folder in the root of the Unity project. Inside the folder, there is a `beamoLocalManifest.json` file that contains details about the availalable Microservices in a project. Also, there is a `config-defaults.json` file that contains the connection strings required to start a Microservice. 

Unity will make sure these files are up to date by scanning assets in the project, and making declarative calls to the beam CLI. 

As part of the Editor's initialization, Unity should set the CID/PID/Auth in the `./beamable` folder by running a `beam login` command passing in all required arguments sourced from the Unity Beamable SDK life cycle. 

Unity should also make sure that the current Unity project is listed as a linked project in the `./beamable` folder. As such, client code will be generated by default when the Microservice is updated and built. 


### IDE Integration 

When a developer wants to run a Microservice directly from the IDE instead of using the Microservice Manager, they will right-click on the project, and click "run". The IDE should launch the Microservice as a native dotnet application on the developer's machine. However, there are 2 major challenges introduced by this assumption...
1. The IDE needs access to a Dotnet installation to run the service, or to compile it correctly and show intellisense. Unfortunately, Dotnet does not provide an IDE agnostic way to configure a Dotnet installation location. Therefor, we must require our customers to install dotnet globally on their machine if they wish to run Microservices directly from the IDE. It is possible that we could automatically install Dotnet on the user's machine in the expected global position, instead of in the project `/Library` folder. 
2. The Microservice must have access to a `beam` CLI instance when it starts. By default, it uses the globally installed `beam` tool. However, we explicitly need the Microservice to use the CLI available in the `/Library/BeamableEditor/BeamCLI` folder, _not_ the globally installed one. 

To address the second condition, we should modify the resolution of the `beam` command in the Microservice's `Prepare` method. The algorithm to find the `./beamable` folder is easy to replicate (search the current directory, and if not found, move to the parent directory, and repeat until the folder is found or the path is exhausted), and we can re-implement it in the `Prepare` method to identify the location of the `./beamable` folder. After the folder is found, if it is a valid Unity project, there may be a `/Library/BeamableEditor/BeamCLI/<version>/beam` file relative to the root `./beamable` folder. If the file is identified, then it should be used instead of the global `beam` command. To identify the version, we should use the nuget version of the Beamable SDK. 


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
    "relativeProjectfile": "A.csproj",
    "standardDockerFileChecksum": "abc",
    "standardProgramFileChecksum": "abc",
    "standardCsProjectChecksum": "abc",
    "beamCsProjReferences": [
        // paths to .beam.csproj files
    ]
}
```

It is critical we use a custom file type, `.beamservice` instead of using a Scriptable Object sub type, because we need to be able to reliably access this file outside of the Unity Asset Database life cycle. We should use a custom Scripted Object Importer to give Unity the ability to understand the `.beamservice` file type, and we can extend a `BeamServiceAsset` type as the main root asset type of the file. This also gives us the ability to extend the inspector for this file type. 

When the BeamEditor initializes, it calls into the Microservice Editor initialization logic. One of the first steps that must take place is the Signpost asset syncronization phase. All `.beamservice` files need to be identified in the project (including all /Package) folders, and sent to the BeamCLI to re-create the local `./beamable/localBeamoManifest.json` file. 

Later, during usage of the Microservice Manager, the contents of `localBeamoManifest.json` are used via CLI commands to populate the UI. The Microservice Manager should never interact with the `.beamservice` files directly, because that introduces a dependency beyond the CLI. 

----

In addition to `.beamservice` files, we should also have `.beamlibrary` files, `.beamstorage`, and later, `.beamfrontend` files. These other files operate similarly to the `.beamservice` file, but represent different types of hidden projects. 

The `.beamlibrary` file represents a common project that targets netstandard 2.0.
```json
{
    "name": "CommonCode",
    "assetRelativePath": "Hidden~/ACommon",
    "relativeProjectfile": "A.csproj",
    "standardCsProjectChecksum": "abc"
}
```

The `.beamstorage` file represents a micro storage project.
```json
{
    "name": "Storage",
    "assetRelativePath": "Hidden~/AStorage",
    "relativeProjectfile": "A.csproj",
    "standardCsProjectChecksum": "abc"
}
```

Neither `.beamlibrary` or `.beamstorage` files represent executable projects.


### .Csproj, Dockerfile, and Program.cs Generation

There are 3 critical files to every Microservice,
1. the `.csproj` file is the dotnet project configuration and controls nuget references, build steps, and project settings
2. the `Dockerfile` is the configuration for how to turn the dotnet project into a standalone docker image that is uploaded to Beamable
3. the `Program.cs` is the boot process of the service, and for local development, is responsible for resolving required connection strings by using the local beam cli. 

All of these files are included in the `Beamable.Templates` package. When a standalone microservice is created, those files are copied from the template project, contextualized with the developer's service name, and placed into the developer's project. 

When a developer in Unity creates a Microservice, we should be using the beam CLI to create the project, which will use the templates by default, and the contents of the files will be correct with respect to the current SDK version. 

However, if the developer accidently modifies one of those files, or attempts to extend in an erronous way, we should offer a way to revert those files. Luckily, they are fairly well generalizable to the common case of Standlaone Microservices. We should offer a feature in the Microservices Manager that resets those files to their factory state. In order to accomplish this, we could create a temp project using the beam CLI, targetted in the `/Temp` folder, and simply copy the relevant 3 files out back into the developer's hidden microservice folder. 

We can also use this methodology to detect if there are changes to those files. The checksum of the file can be kept in the signpost asset, and then the checksum can be compared against the current checksum of the files. This difference should be visible on the signpost asset. 


### Nuget package alignment

The Beamable SDK has a semantic version number baked into the release. Microservices receive their Beamable C# types from a nuget package, `Beamable.MicroserviceRuntime`, and the nuget package has an equally versioned semantic version. There is a 1:1 mapping between SDK version and nuget package version. If the versions become misaligned, because the developer upgrades the nuget package, but forgets to upgrade the SDK, or vice-verca, there is a high probability that _something_ will go wrong due to the version misalignment. As such, Beamable should make sure that the two version numbers stay in sync. 

The SDK version number will be the source of truth. During the initialization of the Beamable SDK, all Microservices should be discovered, and using a new beam CLI command, the nuget version of the Microservice's `MicroserviceRuntime` package is reported back to Unity. If the version is misaligned, then the editor should invoke a new beam CLI command to change the installed nuget version for the project. 

```sh
beam project version ./path-to-project #should return the installed Beamable SDK versions for the runtime, and the common nuget package

beam project version install ./path-to-project --version=1.19.2 #should set the Beamable SDK version
```

If these commands fail for any reason, then we should log an error in the console and request that the user take manual intervention. 


### Sharing Code From Unity to Microservice

Standalone Microservices already have a way to share autogenerated client code, and common code `.dll` files with linked Unity projects. The `./beamable` folder will list the current Unity project as a linked project, so it will receive those updates. 

However, it is common for customers to write code in Unity that they wish to share with the Microservice, not the other way around. To faciliate this, the target `.csproj` file should reference the dependencies the developer wishes to share. For example, if a developer has the following folder structure, and wants the `Service.cs` to reference the `/Common` code...

```
/Assets
    /Common
        MyFile.cs
        MyOtherFile.cs
        MyCommonCode.asmdef
    ServiceSignpost.beamservice
    /Hidden~
        /Service
            Program.cs
            Service.csproj
            Dockerfile
            Service.cs
```

Then the `Service.csproj` file needs to somehow reference the `MyFile.cs` and `MyOtherFile.cs` files. Niavely, we could add a custom `<Compile>` block to the `.csproj` file. However, this does not scale well with Unity's handling to assembly definitions, or shared libraries. Instead, it is preferred to use dotnet's standard way of project referencing, the `<ProjectReference>`. This requires a `.csproj` file to reference, not an `.asmdef`.

Unity does generate a `.csproj` for every `.asmdef`, in the root of the Unity project. However, these projects are only for the benefit of IDEs, and not for the actual compilation of the service. They make many references to unneeded projects, and invalid projects for the Microservice to pull in. As such, we need to create a secondary `.csproj` file that is meant to act as the referencable project. For namesake, this secondary `.csproj` will take on the `.beam.csproj` format. 

The developer must create a `X.beam.csproj` file next to an `.asmdef` file. The rules for `.beam.csproj` files follow `.asmdef` files, in that there can only be one per directory. There is a scripted object importer for `.beam.csproj` extensions that autogenerates a csproj file based on the associated `.csproj` file for the linked `.asmdef` file. Critically, the `.beam.csproj` file content is set by the importer, such that it is a valid `.csproj` file that references all of the files the `.asmdef`'s `.csproj` file does, as well as predefined `.dll` references. The `.beam.csproj` file must also specify its intermediate build output paths to the `/Temp` folder such that `/bin` and `/obj` folders do not get generated in the `/Assets` folder where the `.beam.csproj` resides.

These files should also be generated during the same `AssetPostProcessor` that runs the `.sln` generation, but during the `OnProjectGenerated` callback. In this callback, we can scan for the existence of the `.beam.csproj` file related to the generated `.csproj` file, and perform the file writing. 

When the `.beam.csproj` files are creatd, they represent valid references for Microservices and Common net6 projects to reference. 

---

The `.beamservice` asset contains an array, `beamCsProjReferences`, of paths to `.beam.csproj` files. The associated Microservice `.csproj` file should have a comment flag section, 
```
<-- (begin)Generated By Unity: Do not remove -->
<-- (end)Generated By Unity: Do not remove -->
```

And inside the comments, we should automatically generate an `<ItemGroup>` with project references to the linked `.beam.csproj` files. This should count as part of the unmodified checksum of the project file, such that if a developer factory-resets the file, these references remain. 


### User Interface

The Microservice Manager should no longer use the `MicroserviceDescriptor` type to render Microservice cards. Instead, the Beam editor should use the beam CLI to list out the state of Microservices, using a `beam services ps` and `beam services ps --remote` command. The resulting data should be the source of rendering for the UI. 

All interactions from the UI should use existing CLI commands, or be the cause to create new CLI commands. 

#### Microservice life cycle

The `beam project ps` command can be used to determine if a Microservice is running locally. When the developer toggles the play-button on the Microservice card, we should create a new command, `beam project run` that starts the Microservice as a local dotnet process, or run `beam project stop` that terminates the process. 

We should use ZMQ messaging in the Microservice process itself to listen for stop signals. 

#### Microservice logs

The `beam project logs` command can be used to stream logs from a running Microservice to the UI. 

#### Deployment

The `beam services deploy` command can be used to prepare and execute Microservice deployments from within Unity.

#### Creating a service

The `beam project add` command can be used to create a new Microservice in the Unity project. There is already a variety option for storage objects, and we should add a command to support the creation of a common library. 


### Migration

The shape of Microservices in Beamable SDK 2.0 looks very different from SDK 1.0. We must force a migration from the old format to the new format. Dotnet and CLI installation is already included in the SDK and can be taken as valid assumptions. 

We must be able to detect the presence of a all old Microservice in the project. The following rules were used to determine the existence of Microservices in 1.0,
1. A class that extends `Microservice`, and
2. has a `[Microservice]` attribute, and
3. has an associated `.asmdef` in the same folder.

On every domain reload, we should check for the existence of these files using a Reflection Cache. If any are found, we must show a popup explaining a data migration is going to occur, and that the developer should ensure they have a backup of their project. 

The following steps must be taken for each Microservice
1. create a hidden Microservice folder in the location the `.asmdef` used to be,
2. populate it with the required service files by using the `beam project add` command,
3. copy the entire contents of the folder into the hidden folder's service folder. However, if there are subfolders with their own `.asmdef` files, do not copy those folders, or their decendents. This means we need to write our own recursive file copier.
4. create a sign post asset for the service
5. automatically create `.beam.csproj` files for any referenced `.asmdef` files from the original `.asmdef` file
6. delete the original `.asmdef`,
7. delete all the original source code in the folder, including all sub folders that don't have their own `.asmdef` files.

A similar process must happen for all Microstorage objects.