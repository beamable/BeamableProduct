# CLI Unity Integration

## Summary


## Motivation

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


### Integrating SAMs

At a high level, Unity should be able to display Standalone Microservices, and offer the ability to eject existing Unity backed services. However, the editor should not show _both_ Unity backed services and Standalone at the same time. Managing a set of services from Unity, and a different set from elsewhere will create confusion and bugs. Microservice deployment would be especially confusing if there was a partial CLI based manifest, and a partial Unity backed service list. Instead, the Unity editor should allow using standard Unity backed services, or entirely switching to use Standalone Microservices.


The following requirements need to met for a full SAMs integration with Unity.

1. Unity needs to be aware of SAM `.sln`s 
    - SAMs are aware of Unity Projects through the `.linkedProjects.json` file.
    - 
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

### Bundling the CLI with the SDK

### Replacing existing Unity systems with CLI