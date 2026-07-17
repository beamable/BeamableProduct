[Documentation](https://help.beamable.com/Unity-Latest/unity/getting-started/installing-beamable/)

# Beamable Common
> Note: This folder is a Unity-package copy of the Common project used for Unity package consumption. The canonical source for the Common project is maintained under `cli/beamable.common`.

The Beamable Common project contains shared .NET types used by the Unity SDK, the Beamable CLI, and the Microservice runtime. It targets .NET Standard 2.0 to maintain compatibility with Unity and other runtimes and is published as the NuGet package `Beamable.Common`.

## What lives here
- Dependency injection primitives
- Promise library and async helpers
- Core Beamable types used across runtime and client packages

## Getting started (developer)
This code is not meant to be used directly. Instead, it is part of the Unity SDK, the Microservice framework, and our CLI tooling. However, the common project is published as a standalone package on Nuget, [https://www.nuget.org/packages/Beamable.Common](https://www.nuget.org/packages/Beamable.Common).
- Use the repo `dev.sh` workflow to update the Unity local package to a local NuGet feed used by downstream projects (see root `README.md` Quickstart):
  - `./setup.sh` (run once)
  - `./dev.sh`

If you are a maintainer who needs to change the authoritative Common sources, edit `cli/beamable.common` and publish via the project's tooling; changes here are intended to reflect the published NuGet package for Unity consumption.


# Contributing 
This project has the same [contribution policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#Contributing) as the main repository.

# License 
This project has the same [license policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#License) as the main repository.