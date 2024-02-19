[Documentation](https://docs.beamable.com/docs/installing-beamable)

# Beamable Unity SDK
The Unity SDK is a Unity-Package-Manager (UPM) package that delivers a full Beamable workflow inside of Unity 2020+. 

## Getting Started

Use our package installer, 
[https://packages.beamable.com/com.beamable/Beamable_SDK_Installer.unitypackage](https://packages.beamable.com/com.beamable/Beamable_SDK_Installer.unitypackage).

The package installer will modify your `manifest.json` file and add UPM references. To do this manually, you must add a scoped registry to your Unity project's `manifest.json` file, like below.

```json
"scopedRegistries": [
    {
        "name": "Beamable",
        "url": "https://nexus.beamable.com/nexus/content/repositories/unity",
        "scopes": [
            "com.beamable"
        ]
    }
]
```

Then, if you are using Beamable 1.x, you must add references to `com.beamable`, _and_ `com.beamable.server`. If you are using Beamable 2.x, you only need `com.beamable` (as the other package was factored into the first).

Next, you need to open the Beamable Toolbox, and log in to Beamable. You need to create or use a Beamable account. See our [documentation](https://docs.beamable.com/docs/installing-beamable). 


# Contributing 
This project has the same [contribution policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#Contributing) as the main repository.

# License 
This project has the same [license policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#License) as the main repository.