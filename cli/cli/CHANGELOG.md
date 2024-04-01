# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.19.17]
### Changed
- `BEAM_DOCKER_URI` environment variable will override docker connection uri

## [1.19.16]
no changes

## [1.19.15]
### Fixed
- Progress bars and logs do not appear side by side.
- Unreal Microservice client generation now correctly generates non-primitives used in C#MS signatures

## [1.19.14]

### Fixed
- Docker path issue when adding storage objects 

## [1.19.13]

no changes
=======
- Fixed issue that caused incorrect code-gen of Unreal wrapper types in SAMS-Client code
- Docker will not connect at common unix home directory if `/var/run/docker.sock` is not available
>>>>>>> b8d17c661 (fixes 3216 - docker uri fallbacks and override (#3221))

## [1.19.12]

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

## [1.19.11]

### Added
- `beam config realm` command suite for working with realm config via the CLI.

### Fixed
- Stack traces from Dependency Injection, `GetService`, show inner stack trace instead of Reflection based stack trace.

## [1.19.10]

### Fixed
- fixed issue an issue that would cause an NRE if an existing service had no federated component when running services deploy command

## [1.19.9]

no changes

## [1.19.8]

### Fixed

- OpenAPI generation fixes.

## [1.19.7]

no changes

## [1.19.6]

no changes

## [1.19.5]

### Added

- Auto generation handles trials and session services.

### Changed

- API code-gen now generates structs with properly initialized fields.

## [1.19.4]

### Fixed
- `--reporter-use-fatal` channel supports JSON strings

## [1.19.3]

no changes

## [1.19.2]

### Fixed
- `beam services deploy` no longer times out.

### Changed
 - Templates update with refactor to improve it receiving updates and fixes in the future.

## [1.19.1]

no changes

## [1.19.0]

### Added

- `content tag`, `content tag add/rm` commands. We are supporting passing content ids as list seperated by commas or as a regex pattern.
- Improved `CliRequester` error logs.

### Fixed

- Fixed if older version of templates are installed, allow `beam project new` continue without installing latest templates
- Detect no services found scenario in `beam services ps`.
- Fixed Powershell users having blue-on-blue text when selecting options.
- When executing a microservice that depends on a storage through the IDE, the storage was not booting up in docker.
- Add more information logs when executing C#MS through the IDE.

## [1.18.0]

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

## [1.17.3]

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

## [1.17.2]

### Added

- Added missing XML documentation to CLI related code, enable `TreatWarningsAsErrors` in CLI project.

### Fixed

- `--log` option correctly changes desired log level.
- `beam project {new/add}` commands work if called from other directory than the one with Beamable config.

## [1.17.1]

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

## [1.17.0]

### Added

- `beam project generate-ignore-file` command to generate an ignore file in config folder for given VCS
- `beam services get-connection-string my-storage-name` retrieves the local connection string
for the specified micro-storage
- `beam services get-connection-string my-storage-name --remote` retrieves the remote connection string
for the specified micro-storage
- Add `--quiet` to ignore confirmation step when retrieving connection string

## [1.16.2]

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

## [1.16.1]

### Added

- `beam services deploy` accepts an optional `--docker-registry-url`

### Fixed

- `CliRequester` incorrectly assuming token needs refreshing instead of failed request
- `beam services deploy` uses docker registry endpoint derived from call to `/basic/beamo/registry`

## [1.16.0]

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

## [1.15.2]

### Added

- Project commands such as `project new`
- Basic app structure
