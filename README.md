[Beamable Docs](https://help.beamable.com/Home/)

# Beamable C# SDK Monorepo
This repository contains the Beamable Unity SDK, the Beamable CLI, microservice runtime code, templates, and developer tooling. The repo is intended for reference and internal development workflows. However, this is not an open-source distribution. Use of this code in products is only permitted through approved Beamable distributions such as the Unity SDK, the CLI NuGet tool, the Web SDK NuGet package, or the Unreal SDK.

## What lives here

### [Unity SDK Code](https://github.com/beamable/BeamableProduct/tree/main/client/Packages)
The Unity SDK code is available under the `/client/Packages` folder. The SDK is distributed as a single UPM package, `com.beamable`. The `/client` folder is a Unity project used for internal testing, but nothing in `/client/Assets` is included in any Unity SDK release. 

#### How to test changes in this project
To test any changes to the Unity SDK, you can open the `/client` Unity project in Unity 2021 or greater. 

### [Dotnet Code (CLI, Microservices, Common)](https://github.com/beamable/BeamableProduct/tree/main/cli)
The CLI, Microservice runtime, and shared common types all live under the `/cli` folder and are developed using the `/cli/cli.sln` solution. The solution references `.csproj` files throughout the codebase. The CLI-specific commands and tooling live under `/cli/cli`, while the microservice runtime and related projects live under `/microservice`. Both reference the shared common project.

#### How to test changes in this project
To test changed made to this project you can run the `dev.sh` script and use it in your local environment by setting the `.config/dotnet-tools.json` to use the version generated from the script, usually starts with `0.0.123.` 

### [Beamable.Common NuGet Package](https://github.com/beamable/BeamableProduct/tree/main/cli/beamable.common)
The canonical source for the shared common project lives in `/cli/beamable.common`. This .NET Standard 2.0 project contains most of the Beamable base types (dependency injection, promise library, core components) used across the CLI, Microservice runtime, and Unity SDK. It is published as [`Beamable.Common`](https://www.nuget.org/packages/Beamable.Common) on NuGet. The folder [`client/Packages/com.beamable/Common`](https://github.com/beamable/BeamableProduct/tree/main/client/Packages/com.beamable/Common) is a copy of this project used for Unity package consumption.

### [Unity SDK Installer Code](https://github.com/beamable/BeamableProduct/tree/main/client_installer)
The Unity SDK Installer is available under the `/client_installer` directory. This directory contains a Unity project with code for the Beamable Installer and code for packaging that installer into a `.unitypackage`.

### [Web SDK](https://github.com/beamable/BeamableProduct/tree/main/web)
The Web SDK is a TypeScript library under the `/web` folder, built for both Node.js and browser environments. It is distributed as `beamable-sdk` on npm and includes samples (e.g., the WordWiz Telegram Mini App demo). See the [Web SDK README](https://github.com/beamable/BeamableProduct/tree/main/web/README.md) for installation and usage.

### [Terraform](https://github.com/beamable/BeamableProduct/tree/main/terraform)
The `/terraform` folder contains Terraform manifests for infrastructure managed by CI workflows. It includes reusable modules (e.g., S3) and environment configurations. The CI workflow at `.github/workflows/runTerraform.yml` runs `terraform init/plan/apply` against the selected environment. See the [Terraform README](https://github.com/beamable/BeamableProduct/tree/main/terraform/README.md) for local usage instructions and prerequisites.


Notes:
- The CLI-specific commands and tooling live under `cli/cli` and reference the shared Common project.
- When developing locally you can either consume the published NuGet package or reference the local project directly (see `cli/beamable.common/DOTNET-CODE-README.md` and `cli/DOTNET-CODE-README.md` for details).

## Quickstart — developer flow
Prereqs: `dotnet` (8+), and a POSIX shell for the provided scripts (or use WSL on Windows). `docker` is only required when you plan to deploy, run microservice integration/unit tests, or run containerized flows for microservice development.

- Repo-level dev scripts
- `./setup.sh` (run once) — prepares the local dev environment and builds helper tooling such as the OTEL collector used by microservices during development.
- `./dev.sh` — builds and publishes local packages into a local NuGet feed consumed by downstream projects (used for fast iteration across CLI, microservices, and Unity SDK).
- Run the repo-level scripts from the repository root. See `cli/` README for how to run CLI-specific projects after running the scripts.

### Web local dev (Portal Toolkit & Web SDK)
Prereqs: Node.js 22+, `pnpm`, and Docker.

- `./setup-web.sh` (run once) — starts a local Verdaccio npm registry and local-unpkg CDN via Docker Compose, resets the build number, and configures npm to resolve `@beamable/*` packages from the local registry.
- `./dev-web.sh` — builds and publishes `@beamable/sdk` and `@beamable/portal-toolkit` to the local Verdaccio registry, then restarts local-unpkg to clear its cache.
- `./teardown-web.sh` — removes the `@beamable/*` registry override from npm config and stops the local Docker stack.

## Documentation and help
- Unity SDK docs: https://help.beamable.com/Unity-Latest/
- CLI docs: https://help.beamable.com/CLI-Latest/
- Web SDK docs: https://help.beamable.com/WebSDK-Latest/

## Contributing
This repository is not open for external code contributions. We welcome feedback via GitHub Discussions or Issues:
- Discussions: https://github.com/beamable/BeamableProduct/discussions
- Issues: https://github.com/beamable/BeamableProduct/issues/new

## License
All source in this repository is licensed under the MS-RSL license: https://referencesource.microsoft.com/license.html

You may use the code for reference only; to ship a product use Beamable's official distributions (UPM, NuGet, Dockerhub).
