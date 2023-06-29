# CLI Unity Integration

## Summary

The TLDR is that I think we can move ahead with integrating SAMs into Unity with the CLI without solving the CLI bundling or authentication problems.
1. Auth should come last, and
2. Bundling should come as we need a feature that itself doesn't already enforce the existence of the CLI.

## Motivation

By integrating the CLI into Unity, we will be able to
1. save work by implementing new features in the CLI, and integrating Unity and Unreal with the CLI. Otherwise, we'd need to implement new features once for every engine we want to support.
2. improve the discoverability of SAMs by integrating them directly with Unity

## Implementation


Today, the CLI integrates into Unity by existing on the machine. If `beam` is an accessible command, then the Beamable SDK will attempt to use it for Standalone Microservice request routing (using `beam project ps`). 

There are critical CLI integration points to design and consider.

1. Should the CLI be bundled with the SDK? If so, how?
2. Should the CLI take over the responsibility of authentication with Beamable? 
3. How can the SDK utilize the SDK?

However, these three questions are _separate_. We can solve them at different times. 
We can make considerable progress without doing everything at once.

We should follow this sequencing of events for further integrating the CLI with Unity.

1. Integrate with Standalone Microservices
    - view and manage SAMs
    - eject Unity C#MS as SAMs
2. Implement a strong assumption that the CLI is always available with the SDK, instead of _possibly_ being available via a global dotnet tool.
3. Refactor existing systems to use the CLI for backing implementation instead of custom Unity solutions
    - Content
    - Unity C#MS & Docker
    - Auth

Contrary to recent thinking, the _last_ step of the integration is shifting authentication responsibility to the CLI. Our current goal is to integrate SAMs into the Unity editor. The Beam CLI is required to faciliate SAMs regardless, and it will be available on the machine by default, thus removing the need to think about bundling straight away. The Unity `config-defaults.txt` system is annoying and error prone, but _mostly_ works. We can forward all Unity authentication details to each CLI invocation. Essentially, the CLI will operate without a `config-default.json` file or `user-token.json` file. 

The value of replacing the Unity `config-default.txt` scheme with the CLI is in simplifying the Unity authentication process. It is not a requirement, it is a desire. We can authenticate the CLI without making this refactor. 

Until we reach a bundled CLI, we rely on a globally installed CLI. 
**Version mismatches are possible here!**


### Integrating SAMs

At a high level, Unity should be able to display Standalone Microservices, and offer the ability to eject existing Unity backed services. However, the editor should not show _both_ Unity backed services and Standalone at the same time. Managing a set of services from Unity, and a different set from elsewhere will create confusion and bugs. Microservice deployment would be especially confusing if there was a partial CLI based manifest, and a partial Unity backed service list. Instead, the Unity editor should allow using standard Unity backed services, or entirely switching to use Standalone Microservices.


The following requirements need to met for a full SAMs integration with Unity.

1. Unity needs to be aware of SAM `.sln`s 
    - SAMs are aware of Unity Projects through the `.linkedProjects.json` file.
    - See the section, "Unity SAM Awareness"
2. The Microservice Manager should show a card for each Standalone Microservice and Microstorage. If there are existing Unity C#MS or StorageObjects, a warning should be displayed suggesting an _Ejection_, and no cards should be shown.
    - Microservices and Microstorages should be runnable. Running a Standalone Microservice should use new CLI commands
        - `beam project run <service> # proxies out to dotnet run`  
        - `beam project stop <service> # list processes and kill`
    - Microservices and Microstorages should display logs with log filtering
        - `beam project logs` already exists.
    - Microservices and Microstorages should indicate that they are Standalone. 
    - Microservices and Microstorages should support the open-swagger/open-data button
        - `beam project open-swagger <service>` already exists
        - `beam project open-mongo <storage>` already exists
    - Microservices and Microstorages should support the open code button
3. The Microservice Manager should support the ability to eject _the entire_ set of current Unity C#MS and StorageObjects. This is only available when the CLI is detected.
    - The developer must select a location for the ejection site.
    - `beam project new` will be run at the site,
    - Each Microservice will run as `beam project add`, 
    - Each Microstrage will run as `beam project new-storage`
    - The new `.beamable` project will use `beam project add-unity-project`
4. The developer must be able to remove the configuration and remove the Ejection state from the Microservice Manager.  


### Unity SAM Awareness


### State Sync
Traditionally, in a SAM solution, the developer may use `beam config` to see what CID/PID they are connected to, and then run their SAM relative to that CID/PID combo. If there is a linked Unity project, and that project is setup to to use the same CID/PID combo, the through `beam project ps`, Unity can direct traffic to the local SAM. 

However, if either Unity or the SAM solution change the CID/PID combo, the traffic will not be routable, because they are considered to be different project realms. 

For a tight integration of SAM and Unity, we should ensure that CID/PID changes in Unity proliferate to the linked SAM solution, and vice verca, changing the CID/PID in the SAM solution should proliferate to the linked Unity project. The design goal is that the user's action is always honored as the source of truth. 

Let us consider each site separately.

#### Changing CID/PID from the SAM

The developer can change the CID/PID in a `.beamable` folder either by manually changing the files, or by using a command such as `beam init`. When the Standalone Microservice starts, we already run a `beam generate-env` command from within the C#MS process. At this point, we should identify any linked Unity projects, and adjust the Unity's project CID/PID/Auth combo to point to the same realm. This should happen automatically, but may be disabled if the (new) `--share-auth-with-linked-projects` flag is disabled. We can extract this as a separate command to be used at the developer's discrection,

```shell
beam unity set-config-defaults <path-to-unity> <cid> <pid> <email> <access_token> <refresh_token> [--for-build]
```

The command will need to change the connection settings and auth in given Unity project.

_At this point, I have a few different ideas for implementation, here they are..._

_idea 1_

In the Unity project, the `/Library/BeamableEditor/beamable/editorStorage/singleton_AccountService_global.json` file contains JSON that defines the currently selected cid. It also contains a list of customers with their associated cid values. For each customer, there is a set of metadata defining the selected realm and game. There is also a user object describing the signed in developer, and list of realms that can be configured. The `beam unity set-config-defaults` command needs to ingest this file, and modify it such that the given connection strings are selected in the file. Ideally, the code that manages this file can be moved to the `/Common/Runtime` folder so that the same code is managing the file in Unity and in the CLI.

However, the actual access tokens are not found in the previous `singleton_AccountService_global.json` file. The tokens are present in `PlayerPrefs`, accessible through the Unity `AccessTokenStorage` class with a "editor" prefix. The CLI will need to find a way to write to the `PlayerPrefs`. 

_idea 2: (because that last one was a doozie)_ 

The Unity SDK is already using a standard `beam project ps` command to receive ZMQ updates about local Standalone Microservices. We can create a variant of `beam project ps` called `beam unity listen` that will encompass `beam project ps`, but add new ZMQ message types. The `beam unity set-config-defaults` command send a ZMQ message to a listening Unity project. The message will include the new connection details, and the Unity SDK will use the existing _Switch Realm_ and _Login/Logout_ methods it has. 

However, if Unity is not currently running when the developer runs the `beam unity set-config-defaults` command, then the ZMQ channel is not open, the Unity will miss the message and therefor remain on an old realm. In this scenario, when the Beamable SDK initalizes, it should recongize all linked SAM solutions, recognize a difference in CID/PID combo, and offer a log message explaining the situation. The log message should contain information on steps to sync the states. This scenario is not incredibly harmful, because if the user stops, and re-runs the Standalone Microservice after opening Unity, the `beam unity set-config-defaults` command is likely to execute again, and switch the Unity profile. 

If Unity receives a set-config-defaults message in Playmode, it should log a warning in the console, and ignore the message, because we cannot support switching realms during Playmode. 

#### Changing CID/PID from Unity

The developer can change the Cid/Pid in Unity by logging out and in, or by using the realm switcher. 
<todo>


### Bundling the CLI with the SDK

If the CLI is always available with the SDK, we can begin to replace existing SDK features in favor of the CLI. If the CLI is not available, and we make the assumption it is, then we will be introducing a new source of bug and configuration concern.

The CLI depends on `dotnet`. Currently, developers do not need to install `dotnet` unless they want to _debug_ their Unity backed C#MS. Normally, the CLI can be installed with `dotnet tool install --global Beamable.Tools --version 1.16.1`. However, if `dotnet` is not available, then obviously this approach will fail.

This leads the SDK towards a cascading approach to resolving a CLI. In all cases, the SDK should attempt to find a version of the CLI with the same matching version number. 

This is the series of steps the SDK will use to resolve the CLI location...
1. If the CLI is not already available, continue...
2. Attempt `dotnet tool install Beamable.Tools --tool-path /Temp/Beam/Cli/<version> --version <version>`. If this fails due to no dotnet, continue...
3. Prompt the Developer with a message, "Beamable needs to install Beam CLI <version>. This can happen automatically if `dotnet` is available. However, no `dotnet` framework was detected. Without `dotnet`, Beamable can download a prebuilt binary of the CLI. However, the binary is larger and harder to manage than the variant available with the `dotnet` framework. Would you like to download `dotnet` to continue with the regular CLI installation flow?". 
    - A "Yes" answer will install `dotnet` as quietly as possible. This may not be very quiet. 
    - A "No" answer will start a download from Beamable's CDN, to the user's `/Temp/Beam/Cli/<version>` directory.

In the CDN case, we will need to modify our build & deploy process for the Beamable SDK to automatically build and deploy self contained variants fo the CLI for linux, mac, and windows, all between x86 and ARM based CPU architectures. The deployed CLIs will be uploaded to an AWS S3 Bucket. The S3 bucket should be accessible through a CloudFront CDN layer.

Ultimately, the CLI is available as a lazily resolved asset.

### Replacing existing Unity systems with CLI
<Todo>