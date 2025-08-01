# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [5.3.0] 
### Added
- `beam project` commands that take `--ids` also take a `--exact-ids` option to pass an explicitly _empty_ list of ids. 

### Changed
- `generate-client-oapi` command can generate a subset of services using the `--ids` flag.

### Fixed
- `beam deploy plan` no longer breaks when `publish.json` file does not exist due to an empty workspace folder.

## [5.2.0] - 2025-07-30
### Added
- Added a new command `project generate web-client `, which generates typescript/javascript web client code for calling c# microservices.
- New Static Analyzer for Generic Types on `Microservice` classes; 

### Fixed
- `beam deploy` commands handle non JSON `docker build` logs, which fixes error where builds couldn't find the docker image id of successfully built services. 
- `beam publish` commands now updates published content reference manifest UID for the published one
- `beam checks scan` MongoDB validator for `MongoDB.Driver 3.3.0` no longer adds incorrect xml to `.csproj` files

### Changed
- `beam content sync` command emits a progress event on initial content downloads

## [5.1.0] - 2025-07-23
### Added
  - `developer-user-manager ps` command that watches your developer users files to check if there's any user created/removed/updated.
  - `developer-user-manager create-user-batch` command that create multiple developer users in a batch, it can received a list of templates to copy from.
  - `developer-user-manager create-user` command that can create one developer user.
  - `developer-user-manager remove-user` command that remove the user from the local files (it will not remove from the portal).
  - `developer-user-manager save-user` command that can save a new developer user in the local files.
  - `developer-user-manager update-info` command to edit the local files informations like alias, description and etc.
- Improved diagnostic information for failures in `beam project generate-client --logs v` and better error messaging  
- `beam org games` command will fetch list of available games
- `beam content` commands for handling multiple content manifest ids

### Changed
- `beam services run` command now forces cpu architecture to be linux amd64 by default, with `-pfcpu` option to make it use the user's machine
- `beam deploy release` requires explicit typing `yes` to deploy, unless the `--quiet | -q` flag is given. [4101](https://github.com/beamable/BeamableProduct/issues/4101)

### Fixed
- Fixed issue in the `beam checks scan -f *` command that would cause the fixed code to break serialization of federations.
- Fixed issue in `beam project generate-client` command (for Unreal's code-generation) that caused internal `AdminRoutes`'s Callables (health-check/docs every microservice has) to be seen by the UE generation.
- `beam project ps` shows remote running storage objects [4146](https://github.com/beamable/BeamableProduct/issues/4146)
- `beam init` no longer tries to use old PID when switching CID [4178](https://github.com/beamable/BeamableProduct/issues/4178)
- Content serializer will not serialize `null` Optional values as default values of optional type.

## [5.0.4] - 2025-07-02
### Fixed
- Added check to verify that users actually have the required permissions for the various `ps` commands to work. At the moment, due to a backend bug, the permissions must be set per-realm as an Admin.
- Beamable content downloads ignore SSL when networking with the known Beamable CDN

## [5.0.3] - 2025-06-24
### Fixed
- Fixed issue with `content publish` when a content has a `link` field (this would cause the publish to fail).

## [5.0.2] - 2025-06-23
### Fixed
 - Fixed issue with the `content replace-local` command that wasn't replacing the manifest id reference after copy the content from a realm to another.

## [5.0.1] - 2025-06-18
### Fixed
- Fixed issue with the `project logs` command that could cause the command to fail to exit cleanly when the service process was killed. 
- Fixed issue a performance issue with the `content ps`, the watcher wasn't recognizing actions for batch execution.
- `Promise.Recover` no longer hangs forever when callback throws an exception
- CLI no longer throws internal argument exceptions on large log messages
- Fix `unreal init`, it wasn't triggering the re-generate uproject when called by the `beam_init_game_maker.sh`.
- `init` and `login` commands won't attempt to retry new passwords with `--quiet` flag
- `login` and `me` commands emit data and error streams

### Added
- Added a new command `content tag set`, which can be used to replace tags in the contents.
- Added `DefaultToInstanced`, `EditInlineNew` tags for Unreal serializable types. That helps to use those types as serializables in the content window.
- `me` command includes realm role permissions


## [5.0.0] - 2025-06-06
### Added
- New Code Analyzer to return compile time error for async void Callable methods.
- New Code Fixer to fix async void Callable methods on IDE.
- New Code Analyzer to validate Federations.
- New Code Fixer to Implement possible fixes for Federations.
- New Code Fixer to Solve Microservice classes missing Attribute or Partial keyword. 
- New Code Analyzer to Check if Microservice Callable Methods return are inside Microservice Scope (Needs to be enabled by adding `<BeamValidateCallableTypesExistInSharedLibraries>true</BeamValidateCallableTypesExistInSharedLibraries>` to MS C# project)
- New Code Analyzer and Fixer for Microservice ID non matches the `<BeamId>` csproj property.
- New Code Analyzer and Fixer for non-readonly static fields on Microservice classes.
- Added support for Int32 and FString on Enum deserialization in Unreal code generation.
- Enums in the Unreal code gen is now EBeam[ENUM_NAME] instead of E[ENUM_NAME]. We decided to update our enums to avoid potential conflicts with external code enums.
- New Microservice Client Code Generator for Unity that used OAPI for the generation.
- `MicroserviceBootstrapper` creates OAPI document after building the Microservice
- Added support for generating `FDateTime` instead of `FString` in Unreal code generation.
- Added `beam config --set [--no-overrides]` command to enable local overrides to config variables like `PID`.
  The intended usage of this command is to allow a user to select their current realm WITHOUT changing the `configuration-defaults.json` file which is committed to version control.
- Added `beam org realms` command that prints out a list of all available realms for the requesting user.
- New `beam content` command pallet for SAMS and Engine-integration usage.
- CLI can emit open telemetry data when `BEAM_TELEMETRY` environment variable is enabled.
 
### Changed
- Logging uses `ZLogger` instead of `Serilog`
- Revise the categorization of all generated Blueprint nodes to enhance discoverability in Unreal Engine.
- `OptionalString` overrides `.ToString()` for easier print debugging.
- `beam me` command now also gives you back your active token information, but no longer gives you the `deviceIds` for a user
- `beam init -q --cid my-game --username my@email.com --password my_password` now honors the quiet flag correctly. It'll auto-select the realm as the oldest development realm.
- `IAccessToken`, the interface representing a Beamable access/refresh token pair, now exposes the `IssuedAt`/`ExpiresIn` data in addition to the `ExpiresAt` date.
- `beam checks scan` includes fixes for CLI 5 upgrade
- `beam org new` no longer creates an organization directly on the CLI. Instead it opens the browser to the Beamable portal registrations page
- `beam project generate-client` is no longer the default post-build action. Use `beam project generate-client-oapi` instead

### Fixed
- Fixed an issue in which running `beam deploy release` when CID was an alias resulted in an error in execution.
- Fixed `useLocal: true` in Scheduler Microservice invocation when the C#MS is remotely deployed.

## [4.3.1] - 2025-06-05

no changes

## [4.3.0] - 2025-05-08
### Added
- `.beamignore` files may be used to ignore services and storages from the `beam deploy` commands.
- Hidden `--ignore-beam-ids` option can be used to ignore beam ids similar in addition to `.beamignore` files. [#4019](https://github.com/beamable/BeamableProduct/issues/4019)

### Fixed
- `beam deploy` commands no longer attempt to build non-existent source when `--merge` flag is used.

## [4.2.0] - 2025-04-04
### Changed
- `beam deploy` commands use solution level building instead of per-project [#3952](https://github.com/beamable/BeamableProduct/issues/3952)
- `beam project open` command can create a `.slnf` file to show a subset of projects based on the Unity project perspective.
- Refactor on `IstatsApi` and `IMicroserviceStatsApi` to now have new methods to handle Stats with better naming and usability. Older methods were flagged as `Obsolete`

### Fixed
- `beam project generate-client` creates clients with correct `ISupportsFederation` style interfaces directly from the CLI by loading available `IFederationId` types [#3958](https://github.com/beamable/BeamableProduct/issues/3958)
- `beam project open` works with Visual Studio
- `beam init` will save extra path files even when reinitializing a `.beamable` folder

### Added
- `beam deploy release` shows CID/PID information as part of release confirmation [#3954](https://github.com/beamable/BeamableProduct/issues/3954)
- `beam checks scan` command recognizes missing `.dockerignore` configuration where "!**/beamApp" is required as final line.
- `beam project generate-client` supports `--output-unity-projects` flag to specify custom Unity project output paths in addition to linked projects

## [4.1.5] - 2025-03-26
### Fixed
- Microservices Docker images can be larger than 2GB. [#3926](https://github.com/beamable/BeamableProduct/issues/3926)
- `beam deploy` commands use `--disable-build-servers` to prevent file locking
- `beam deploy` commands check that `docker` is running before starting
- `beam deploy` commands set `CopyToLinkedProjects` to `false` to avoid duplicate project copies.
- multi-threading issue `beam deploy` commands that led to `"local service does not exist"` error.

### Added
- `beam deploy` commands support new `--build-sequentially` option to run builds in sequence instead of all together

## [4.1.4] - 2025-03-20

### Fixed
- Improved DateTime deserialization.

## [4.1.3] - 2025-03-07

no changes

## [4.1.2] - 2025-03-06

### Changed
- Microservice docker builds use their project folder as a Docker Build Context, instead of the `.beamable` root folder.

### Fixed
- Deleting local storage objects now marks them as "archived" in remote deployment

## [4.1.1] - 2025-02-24

### Changed
- Autogenerated API uses correct `long` type for Mail time and expiration fields

## [4.1.0] - 2025-02-20

### Added
- `beam content pull` now accepts `content-ids "some.content.id,some.other.content.id"` option [#3878](https://github.com/beamable/BeamableProduct/issues/3878)
- `beam project remote-logs` command exists for advanced use cases
- `beam checks scan` command for finding known issues after CLI upgrades

### Fixed
- Unity project not being added to SAMS if it is a child of the SAMS
- `additional-project-paths` and `project-paths-to-ignore` being overwritten when calling `beam init`
- various CLI commands no longer break when the `.beamable` workspace is
  located in a directory with spaces in the path string. [#3866](https://github.com/beamable/BeamableProduct/issues/3866)
- beam fed add - fails to add inventory federation after adding login federation [#3873](https://github.com/beamable/BeamableProduct/issues/3873)

## [4.0.0] - 2025-01-24

### Added
- `project run` command includes `--require-process-id` option that will cause microservices to exit when given process terminates. [#3839](https://github.com/beamable/BeamableProduct/issues/3839)
- `beam token new-guest` internal command supports `-k` and `-v` pairs for initProperties for player init federation

### Fixed
- Fixed issue in `unreal init` command that could cause the Target file to fail being modified.
- `project generate-client` no longer fails on projects referencing `Microsoft.Extensions.Caching.Memory`, [#3844](https://github.com/beamable/BeamableProduct/issues/3844)
- CLI commands use local microservice federation unless the new `--prefer-remote-federation` flag is provided.

### Changed
- nuget package license and icon use baked files instead of links.
- re-running `beam init` in an existing beamable workspace will prompt for new CID and PID information. [#3456](https://github.com/beamable/BeamableProduct/issues/3456)

## [3.0.2] - 2024-12-17

### Fixed
  - Fix .gitignore not being created when passing cid/pid to the Init command

## [3.0.1] - 2024-12-09

### Fixed
- Source Generator analyzer would incorrectly accuse multiple partial implementations of being different classes. It no longer does that.
- Fixed issue in `beam_init_game_maker.sh` (`unreal init` command) when `OnlineSubsystemBeamable` was already installed in the project.
- Fixed issue in the Microservice Client Generation that would incorrectly generate a type that was already inside the `BeamableCore` generated types (from `AutoGenerated` namespace in Microservices).

## [3.0.0] - 2024-12-04

### Added

- `beam project enable` and `beam project disable` commands will set the `<BeamEnabled>` setting.
- `beam services build` uses Docker Buildkit to build Standalone Microservice images
- `oapi download` flag `--combine-into-one-document` for combining OpenAPI documents into one
- `beam deploy` command suite for planning and releasing deployments
- `beam portal` command suite for opening Portal
- `beam player` command suite for inspecting player data
- `beam logout` command removes auth tokens
- `beam fed` command suite for managing service federations
- `beam deploy` command suite for planning and releasing service deployments
- `--docker-cli-path` option overrides docker cli location used for Buildkit
- `--unmask-logs` option will show full tokens in verbose logs
- `--no-log-file` option will prevent verbose logs from being written to temp file
- `--emit-log-streams` option will send all logs as a raw payload with the type, `logs`
- `--no-redirect` option will prevent global tool invocations from redirecting to local tool invocations

### Changed

- `beam project ps --raw` includes an `executionVersion` representing the version of the Beamable SDK being used in the service
- `beam project ps --raw` includes an `processId` and `routingKeys` representing the locally running OS process id, if any, and the list of routing keys currently registered with the Beamable backend for that service.
- `beam project run` args modified: `--watch` is no longer supported due to underlying .NET issues. Added `--detach` to make it so that, after the service starts, we exit the command (the service stays running as a background process; stopped by `beam project stop` command).
- `beam project open-swagger` now takes in `--routing-key` as opposed to `--remote`. Not passing `--routing-key` gives you the same behavior as passing `--remote`.
- `beam temp clear logs` command will clear old log files in the `.beamable/temp/logs` folder.
- `beam version update` updates the local tool installation
- service deployments use buildkit via Docker CLI instead of Docker API.
- log files are kept in the `.beamable/temp/logs` folder and are cleared after each day if the total number of log files exceeds 250
- use local dotnet tool installation by default instead of global installation
- global invocations of `beam` will automatically redirect to local tool installations

### Fixed
- JSON output will correctly render optional types
- DockerHub 4.31 is supported via microservices using `host.docker.internal` instead of `gateway.docker.internal` to communicate to `localhost`

### Removed
- `beam services deploy` has been replaced in favor of `beam deploy`

## [2.0.3] - 2025-03-24

### Fixed
- `beam services deploy` retries local health check for services up to 5 minutes per service.

## [2.0.2] - 2024-09-25

### Added
- `matchType` field of the `Lobby` struct used by `IFederatedGameServer`

## [2.0.1] - 2024-06-17

### Added
- `--no-filter` option to `beam listen server`

### Fixed
- `beam project stop` will stop services running in docker
- `beam service ps`  was not working when calling it because it was trying to get the ImageId of storage objects
- common lib handling uses `.` as a default path instead of the empty string
- `UpdateDockerfile` update to fix common lib handling for docker builds

### Changed

- `beam service ps` now doesn't have the `--remote` flag and always return information updated with both local and remote

## [2.0.0] - 2024-05-24

### Added

- `--raw` option will output commands in machine readable JSON format
- `beam listen player` command monitors notifications sent to the logged in CLI user
- `beam listen server` command monitors server events
- `beam project run` command will run a dotnet project
- `beam project stop` will stop running dotnet projects
- `beam project build` will build a dotnet project
- Unreal Microservice client generation now correctly generates non-primitives used in C#MS signatures, including containers and nested containers.
- Unreal Microservice client generation now happens for all microservices at once and support shared code.
- `BEAM_DOCKER_URI` environment variable will override docker connection uri
- Added Bulk Edit Content command to the CLI (`beam content bulk-edit`)

### Changed

- Updates the Serilog and Spectre dependencies.
- Updated dotnet framework dependencies to maximize and enforce compatibility (minimum dotnet 6)
- Commands will use `--raw` output automatically when piped to another process
- Root commands with no action will automatically print `--help`
- `docs`, `profile`, `generate-interface` and http commands are marked as `[INTERNAL]`
- Commands automatically save the latest log to the temp path.
- Commands will log single outputs as JSON by default.
- Console logging no longer includes log level and timestamp.
- Logs containing GUID based tokens will be masked and only the last 4 characters will be shown.
- Unreal Microservice Client Code generation no longer appends the microservice name to serializable types.
- Microservices install with current CLI version
- Standalone Microservices no longer have a `LoadEnvironmentVariables` method, and connection strings are handled in the existing `Prepare` method.
- `beam project generate-env` command writes a blank `.env` file and returns connection strings over STDOUT instead.
- Generating microservice clients for Unreal now outputs them to a Plugin called `[ProjectName]MicroserviceClients` instead of placing it in some existing module.
- Internal commands are hidden unless `--help-all` is passed.

### Fixed

- `project add` Dockerfile path fixes.
- `project new-storage` path fixes.
- Progress bars and logs do not appear side by side.
- Fixed issue that caused incorrect code-gen of Unreal wrapper types in SAMS-Client code
- Docker will not connect at common unix home directory if `/var/run/docker.sock` is not available

### Removed

- `beam content` no longer directly opens content folder.

## [1.19.23] - 2024-10-23
no changes

## [1.19.22] - 2024-07-19
no changes

## [1.19.21] - 2024-06-18

### Changed
- config files use indented JSON

## [1.19.17] - 2024-04-04
### Changed
- `BEAM_DOCKER_URI` environment variable will override docker connection uri
- Standalone Microservices no longer have a `LoadEnvironmentVariables` method, and connection strings are handled in the existing `Prepare` method.
- `beam project generate-env` command writes a blank `.env` file and returns connection strings over STDOUT instead.
- Docker will not connect at common unix home directory if `/var/run/docker.sock` is not available

## [1.19.16] - 2024-03-2
no changes

## [1.19.15] - 2024-03-07
### Fixed
- Progress bars and logs do not appear side by side.
- Unreal Microservice client generation now correctly generates non-primitives used in C#MS signatures

## [1.19.14] - 2024-02-06

### Fixed
- Docker path issue when adding storage objects

## [1.19.13] - 2024-02-05
no changes

## [1.19.12] - 2024-01-22

### Added
- Better validation and error messages for add-unreal-project command code-path;
- Unreal Microservice client generation now correctly identifies whether or not the OSS UE Plugin is there and, if so, it'll add the microservices code to that module instead.
- Unreal Microservice client generation now checks whether or not the linked project is using the OnlineSubsystemBeamable plugin and, if so, checks if it is configured correctly. This catches the case where people add the OSS after the Microservice was already added to the project modules;
- CLI will now check if its necessary to run Unreal's Generate VS Project Files command after generating client code and, if so, will run and wait for it as part of the generate-client command (it is needed when new client callables are added/removed);

### Fixed
- Fixed issue that caused paths not to be stored relative to the `.beamable` folder correctly
- Fixed issue that caused incorrect `\\` to be used instead of `/`
- Fixed serializer generation to correctly use `TCHAR` as opposed to `wchar_t`
- Fixed `FGuid` serialization to always serialize with digits + hyphen + lower case;

## [1.19.11] - 2024-01-12

### Added
- `beam config realm` command suite for working with realm config via the CLI.

### Fixed
- Stack traces from Dependency Injection, `GetService`, show inner stack trace instead of Reflection based stack trace.

## [1.19.10] - 2024-01-05

### Fixed
- fixed issue an issue that would cause an NRE if an existing service had no federated component when running services deploy command

## [1.19.9] - 2023-12-20

no changes

## [1.19.8] - 2023-12-15

### Fixed

- OpenAPI generation fixes.

## [1.19.7] - 2023-11-29

no changes

## [1.19.6] - 2023-11-22

no changes

## [1.19.5] - 2023-11-15

### Added

- Auto generation handles trials and session services.

### Changed

- API code-gen now generates structs with properly initialized fields.

## [1.19.4] - 2023-11-02

### Fixed
- `--reporter-use-fatal` channel supports JSON strings

## [1.19.3] - 2023-10-26

no changes

## [1.19.2] - 2023-10-11

### Fixed
- `beam services deploy` no longer times out.

### Changed
- Templates update with refactor to improve it receiving updates and fixes in the future.

## [1.19.1] - 2023-09-22

no changes

## [1.19.0] - 2023-09-20

### Added

- `content tag`, `content tag add/rm` commands. We are supporting passing content ids as list seperated by commas or as a regex pattern.
- Improved `CliRequester` error logs.

### Fixed

- Fixed if older version of templates are installed, allow `beam project new` continue without installing latest templates
- Detect no services found scenario in `beam services ps`.
- Fixed Powershell users having blue-on-blue text when selecting options.
- When executing a microservice that depends on a storage through the IDE, the storage was not booting up in docker.
- Add more information logs when executing C#MS through the IDE.

## [1.18.0] - 2023-09-01

### Added

- Support for storing local content from multiple namespaces.
- Filter out storage objects from `beam services enable` selection wizard.
- `beam project new` now have --disable flag to create service as disabled on publish.
- Ability to retry again if alias or username or password is entered incorrectly.
- Commands which require config to work will be cancelled if no config is available.

### Fixed

- `beam services deploy` fetches current realm snapshot before deploy, allowing publication of new services without old services.
- Validate cid and resolve alias to cid on Microservice deploy.
- Standalone microservices now write federated components to the manifest when deployed.
- Creating a new project with NET 6.0 no longer fails to install templates.

### Changed

- `run-nbomber` cli command accepts a json file as body for request instead of an argument.

## [1.17.3] - 2023-08-21

### Added

- Add `Beamable.Common` as dependency to SAMS Common project

### Changed

- `beam project {new/add}` does update SAMS Beamable dependencies during project creation.
- `beam project update-unity-beam-package` command for installing and updating Beam packages in Unity projects.
- Requests commands (`beam {get/put/post/delete/me}`) always output result to console.

### Fixed

- Fix issue with archived realms showing in options when selecting realm
- Rerunning `beam services run` will detect code changes
- New projects created with `beam project new` will include the required `Microsoft.OpenApi.Readers` package
- If an internal `dotnet` command fails, `beam` will now emit the logs of the failed command
- Improved installed templates detection

## [1.17.2] - 2023-08-10

### Added

- Added missing XML documentation to CLI related code, enable `TreatWarningsAsErrors` in CLI project.

### Fixed

- `--log` option correctly changes desired log level.
- `beam project {new/add}` commands work if called from other directory than the one with Beamable config.

## [1.17.1] - 2023-08-10

### Added

- `beam services run` takes a `--force-amd-cpu-arch` flag that will force the built CPU architecture to `linux/amd64`
- Auto install Beamable templates
- Add `beam services stop` to stop locally running container for selected Beamo services
- Add refresh token to open-swagger url

### Fixed

- Fix issue with cli crash when linking unity/unreal project that scans over protected folder
- Fix `beam project open-swagger` and `beam project open-mongo` when service name or storage is not specified
- Fix `beam project open-swagger` and `beam project open-mongo` when multiple service name or storage exist in the same directory, user can now select the service name or storage to use
- `beam services deploy` will force build services to `linux/amd64` CPU architecture for usage on Beamable Cloud
- Fix `beam open-mongo` returning wrong command to execute when storage is not running
- Fix open-swagger case sensitivity problem

### Changed

- newly created service will have `ShouldBeEnabledOnRemote` as true in `BeamoServiceDefinition`
- when creating new storage, service dependencies are all selected by default

## [1.17.0] - 2023-07-27

### Added

- `beam project generate-ignore-file` command to generate an ignore file in config folder for given VCS
- `beam services get-connection-string my-storage-name` retrieves the local connection string
  for the specified micro-storage
- `beam services get-connection-string my-storage-name --remote` retrieves the remote connection string
  for the specified micro-storage
- Add `--quiet` to ignore confirmation step when retrieving connection string

## [1.16.2] - 2023-07-12

### Added

- create default `.gitignore` file during `beam init` if none is present
- `beam version` commands to manage CLI version install
- Add NET 7.0 as second target for CLI

### Fixed

- Fix issues with `ShowLoading` helper function caused by rethrowing
- Pull `mongo-express` in `project open-mongo` command if needed

### Changed

- If there is only one Microservice, `beam project open-swagger` work without passing Microservice ID
- If there is only one Storage, `beam project open-mongo` work without passing Storage ID

## [1.16.1] - 2023-06-28

### Added

- `beam services deploy` accepts an optional `--docker-registry-url`

### Fixed

- `CliRequester` incorrectly assuming token needs refreshing instead of failed request
- `beam services deploy` uses docker registry endpoint derived from call to `/basic/beamo/registry`

## [1.16.0] - 2023-06-27

### Added

- `beam org new` creates a new Beamable organization
- `beam project logs` tails service logs
- `CLIException` now return error codes with valuable semantics (unknown and reportable errors are error code `1`;
  anything above 1 is a usage error that a user should be able fix)
- `beam docs` creates and uploads CLI documentation to Readme.com
- Added `project add` command that allows to create new project and add it to an existing solution

### Changed

- fix path issues in `project new` with different names for solution and project
- Split `services deploy [--remote]` command into `services deploy` (for remote) and `services run` for running services
  in local docker
- Autogenerated SDK for Unity now includes proto-actor API.
- fix issues with failing `project new` command
- `beam project open-mongo` opens the browser to `localhost` instead of `0.0.0.0`

### Fixed

- `beam project generate-env` loads `.dll` files into new context, allowing for multiple versions of similar libraries

## [1.15.2] - 2023-05-18

### Added

- Project commands such as `project new`
- Basic app structure
