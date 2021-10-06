# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.17.0]
### Added
- Ability to use Promises in ClientCallable methods
- Container health checks are reported in deployment manifests
- Local Mongo Storage Preview

### Fixed
- Microservice clients can now deserialize json lists

## [0.16.0]
### Added
- New Microservices Manager window
- Local microservice health checks are accessible on container port 6565
- Snyk testing for microservices
- Ability to categorize `[ClientCallable]` methods in documentation with `[SwaggerCategory]`

### Changed
- Beamable Platform errors all extend from `RequesterException` in Unity Client and microservice code

### Fixed
- Visual Studio Code debug configuration source maps are now correct
- `AssumeUser()` no longer throws null reference

## [0.15.0]
### Added
- Commerce Service AccelerateListingCooldown support to reduce the cooldown on a listing


## [0.14.3]
- JSON Serialization error in Stats API
- `AssumeUser()` no longer throws null reference


## [0.14.2]
### Fixed
- Namespace declarations starting with Editor, and removed un-used class PopupBtn
 

## [0.14.1]
- no changes


## [0.14.0]
### Added
- Dependency Injection system
- ISerializationCallbackReceiver for request and response objects used in `[ClientCallable]` methods
- Custom routing prefix setting in `MicroserviceConfiguration`

## [0.13.0]
### Added
- Added a difference check at Microservice Deployment time, so that microservices that have not changed are not reuploaded.
- Added a RealmConfig service to available Services. This can be used to read protected realm settings.

## [0.11.0]
### Changed
- Changed response serialization from Microservice. Use UseLegacySerialization to maintain old behaviour.

### Added
- Added a cache for content fetches. 
- Added Doc Url to package.json.
- Added Open API 3.0 auto-generative documentation.
- Added AdminOnlyCallable version of ClientCallable that requires caller to have full scopes.
- Added requiredScopes field to ClientCallable that requires caller to have required scopes.

### Fixed
- Fixes leaderboard and stat fetching cache errors.
- Fixes null reference bug when passing null to [ClientCallable] arrays.