# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## (Unreleased)
### Added
- Snyk testing for microservices.

### Changed
- Error structure and paradigm common to Microservices and Unity Client

### Fixed
- Visual Studio code debug configuration source maps are now correct

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