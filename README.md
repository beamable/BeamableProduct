[Beamable Docs](https://docs.beamable.com/docs/beamable-overview)

# Beamable C# SDK Monorepo
This repository contains the Unity C# SDK, the CLI, project templates, the Microservice framework, and various utilities. This code is available to browse, read, and understand. However, this repository is not _Open Source_, and usage of the code is only permitted through approved Beamable products such as the Unity SDK or the CLI. 

#### [Unity SDK Code](https://github.com/beamable/BeamableProduct/tree/main/client/Packages)
The Unity SDK code is available under the `/client/Packages` folder. In Beamable 2.x, there were two packages, `com.beamable`, and `com.beamable.server`. However, in Beamable 3.0, there will be a single package, `com.beamable`. The `/client` folder is a Unity project that we use for internal testing, but nothing in the `/client/Assets` folder is included in any Unity SDK release. 

#### [Microservice Code](https://github.com/beamable/BeamableProduct/tree/main/microservice)
The Microservice framework code is available under the `/microservice` folder, and we use the `/microservice/microservice.sln` solution when developing the Microservice framework. However, the solution file references the Beamable common project, in the `/client/Packages/com.beamable` folder. 

#### [CLI Code](https://github.com/beamable/BeamableProduct/tree/main/cli)
The CLI code is available under the `/cli` folder, and we use the `/cli/cli.sln` solution when developing the CLI. However, similar to the Microservice solution, the CLI solution references `.csproj` files throughout the code base, including the `/client/Packages/com.beamable` common project.

#### [Beamable.Common Nuget Package](https://github.com/beamable/BeamableProduct/tree/main/client/Packages/com.beamable/Common) 
Inside the `/client/Packages/com.beamable/Common` folder, there is a `.csproj` that declares a Net Standard 2.0 project. This common project contains most of our Beamable base types that are used across Unity, the CLI, and the Microservice.

#### [Unity SDK Installer Code](https://github.com/beamable/BeamableProduct/tree/main/client_installer)
The Unity SDK Installer is available under the `/client_installer` directory. This directory contains Unity project with code for Beamable Installer and code for packaging that installer into `.unitypackage`.

# Getting Started
This repository is for referential use only. If you're looking to get started building with Beamable, then you should head over to our documentation.

To start using the Unity SDK, 
[https://docs.beamable.com/docs/installing-beamable](https://docs.beamable.com/docs/installing-beamable)

To start using the CLI directly,
[https://docs.beamable.com/docs/cli-guide-getting-started](https://docs.beamable.com/docs/cli-guide-getting-started)

# Contributing 
At this time, Beamable is not accepting code contributions from outside the company. However, feedback and discussion is more than welcome, so please consider posting in the [Github Discussions](https://github.com/beamable/BeamableProduct/discussions), or report a [Github Issue](https://github.com/beamable/BeamableProduct/issues/new)

# License 
All source code in this repository is licensed under the [MS-RSL](https://referencesource.microsoft.com/license.html) license, which is also included locally in the repository, in the [LICENSE.txt file](https://github.com/beamable/BeamableProduct/tree/main/LICENSE.txt).

In short, you are free to use this code for _referential use_ only. To use the code for any other purpose, you must use Beamable's official distributions, available through Unity Package Manager, Nuget, or Dockerhub.  