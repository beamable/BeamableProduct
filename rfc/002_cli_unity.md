# CLI Unity Integration

## Summary

Beamable 2.0 will have a hard dependency on the Beam CLI and Dotnet LTS. The Beamable SDK will automatically install Dotnet and the CLI to the /Library folder of each Unity project.

## Motivation

By integrating the CLI into Unity, we will be able to
1. save work by implementing new features in the CLI, and integrating Unity and Unreal with the CLI. Otherwise, we'd need to implement new features once for every engine we want to support.
2. improve the discoverability of SAMs by integrating them directly with Unity

## Implementation

As part of the `BeamEditorContext`'s initialization, the SDK will perform 2 resolution phases, one for Dotnet, and then a second for the Beam CLI. 

### Resolving Dotnet

The Beam SDK will require Dotnet LTS (likely dotnet8 by the time of launch), and it will attempt to load the Dotnet LTS major version from a set of paths.

```
$BEAMABLE_DOTNET_PATH
/Library/BeamableEditor/Dotnet
/<system_default_install_location>
```

If Dotnet is found at the environment variable, $BEAMABLE_DOTNET_PATH, then the Dotnet resolution phase is complete.

If Dotnet is found in the /Library folder, then the Dotnet resolution phase is complete. However, Beamable will run a background task checking for any patch updates to the LTS version. 

If Dotnet is found in the default install location, then the Dotnet resolution phase is complete.

If Dotnet is not found in either location, then the SDK will show a popup to the developer with the following text,

> Beamable Requires Dotnet 8 

> Dotnet 8 will automatically be installed for this project. By default, Beamable will keep this installation up to date for the latest security patches. The install will be local to this Unity project only. Check the documentation for more information. 

The popup only shows a link to documentation, and an _Okay_ button. Upon clicking the button, the popup transforms into a loading screen while the SDK installs Dotnet in the background using Microsoft's install script. 

When the installation is done, the resolution phase starts from the beginning, and we expect to find the Dotnet installation in the /Library folder. 

### Resolving Beam CLI

The Beam SDK will require the Beam CLI, and it will attempt to load the CLI from the following path,

```
/Library/BeamableEditor/BeamCLI/<sdk-version>/beam
```

If the `beam` command exists in the /Library, then the `beam version` command is run to confirm the built program uses the same version as the currently installed UPM SDK.

If the version is not aligned, or if no `beam` command exists in /Library, then the SDK will automatically install the correct version. This operation should be quick enough that we don't _need_ a loading bar, but we should present one regardless. 

If the `BEAMABLE_DEVELOPER` flag is enabled, then the CLI resolution starts instead by checking if the `CoreConfiguration.UseGlobalBeamCLI` is set to true. The `CoreConfiguration.UseGlobalBeamCLI` field is only present when the developer flag is set. If the field is true, then the CLI resolution will look for the CLI on the global path, instead of the /Library path.

### Usage

When both the Dotnet Resolution and Beam CLI Resolution are completed, the paths for each tool must be stored in the Editor Beam Context as memory values. They should not be saved to disk, because they should be recomputed every domain reload. 

The CLI SDK must accept parameters for Dotnet's path, and the CLI's path. 
The CLI implementation must use the given Dotnet path in place of all global calls to Dotnet. 

There should be a new _Project Settings / Core / Dotnet_ section that shows the current paths for Dotnet and the Beam CLI. 
