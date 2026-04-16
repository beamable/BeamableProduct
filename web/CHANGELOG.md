# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-04-16

### Added

- Web SDK: `codegen` script in `package.json` to regenerate TypeScript API types from OpenAPI specs via the CLI.
- Web SDK: New generated APIs: `BillingApi`, `CustomerApi`, `PlayerSessionApi`, `PlayerStatsApi`.
- Web SDK: `BeamWebSocket` now sends a `session-start` frame as the first message after connecting, carrying device and platform info that browsers cannot set via WebSocket upgrade headers.

### Fixed

- Web SDK: Code generator now quotes TypeScript property names that contain invalid identifier characters (e.g., `x5t#S256`).
- Web SDK: Code generator now produces distinct method names for PATCH endpoints instead of colliding with GET.
- `tsdown.config.ts` updated to use `import.meta.url` instead of `__dirname` for ES module compatibility.
- Web SDK: Code generator now correctly emits `w: true` (auth flag) for endpoints that require a bearer token. Previously, after the `ForcePlayerScopedAuth` OpenAPI processor renamed the security scheme from `user` to `auth` and `Reserailize` stripped reference info, `DetermineAuth` failed to detect auth requirements for all `basic` service endpoints — causing the SDK to omit the `Authorization` header on calls like `GET /basic/accounts/me`.
- Web SDK: HTTP response parsing now preserves precision for int64 values (e.g., player IDs, gamer tags). Previously `JSON.parse` rounded large integers before the reviver could convert them to `BigInt`, producing incorrect IDs like `70820408384930820` instead of `70820408384930816`. A new `BeamJsonUtils.parse` pre-quotes integers >10 digits before parsing so they reach the reviver as strings.

### Changed

- Web SDK: Updated auto-generated APIs and schemas to latest OpenAPI specs.
- Web SDK: Web code generator now only emits header parameters that `makeApiRequest` actually supports (currently `X-BEAM-GAMERTAG`); unsupported headers like `X-BEAM-TIMEOUT` are no longer added to generated method signatures.

## [1.0.0] - 2025-11-19

### Added

- Enable Beam SDK configs to register all client/server services during initialization.
- `Beam.use` and `BeamServer.use` now accept arrays of services or microservice clients for batch registration.

### Fixed

- Skip refresh token attempts when endpoints return known `BeamRequester` errors (`InvalidCredentialsError`, `InvalidRefreshTokenError`, `TokenValidationError`).

## [0.6.0] - 2025-09-15

### Added

- Thorium socket support.
- `BeamServerWebSocket` implementation for connecting to thorium sockets.
- Beamable server event types, with support for custom server event types.

### Changed

- `getExternalIdentityStatus` and `removeExternalIdentity` include a required `externalUserId` field.
- Improved code structure in `Beam`, `BeamServer`, `BeamRequester`, and `BeamUtils`.
- Updated `TokenStorage` implementation.

### Fixed

- New tokens were not being added to the Authorization header after a 401 refresh.

## [0.5.1] - 2025-09-02

### Fixed

- Removed unexpected dependencies.

## [0.5.0] - 2025-09-01

### Added

- `Content` service.
- `Content` types.
- `ContentStorage` for persisting manifests and content to IndexedDB or the file system.
- `content.refresh` added to list of refreshable events.
- In-memory cache for `Content` to optimize retrieval.

### Changed

- Updated auto-generated APIs and schemas.

## [0.4.1] - 2025-08-12

### Fixed

- Invalid credential when `loginWithEmail` triggered a refresh token and retried.

## [0.4.0] - 2025-08-06

### Added

- Initialization via `Beam.init()` and `BeamServer.init()`.
- Environment-variable support via `BeamBase.env`, `Beam.env`, and `BeamServer.env`.
- `use()` service locator and SDK mixin to register client and server services.
- Authentication via email/password, third-party providers, and external identity.
- `federationIds` in generated Microservice web client for federated authentication.

### Changed

- Normalized built-in Beamable environment names (`dev`, `stg`, `prod`) to lowercase.

## [0.3.2] - 2025-07-24

### Added

- Web client generation for Beamable C# Microservices.

## [0.3.1] - 2025-07-18

### Changed

- Switched from API classes to functions.
- Access SDK APIs via `beamable-sdk/api`.

### Removed

- `BeamApi` class.

## [0.3.0] - 2025-07-14

### Added

- Signed Requests implementation.
- `Leaderboard` service.
- `BeamServer` class for server-side integration with the Beam SDK.

## [0.2.0] - 2025-06-30

### Added

- WebSocket implementation.
- `Announcements` service.
- `Stats` service.
- Access to SDK schema types via `beamable-sdk/schema`.

## [0.1.7] - 2025-06-19

### Changed

- Separate build configs for various bundle formats.

## [0.1.6] - 2025-06-19

### Changed

- Package renamed.

## [0.1.5] - Unpublished

### Changed

- Split bundle based on platform.

## [0.1.4] - Unpublished

### Added

- File storage for token persistence in Node environments.

### Changed

- TokenStorage implementations upgraded.
- Reduced bundle size of generated API classes.

### Fixed

- `TokenStorage.isExpired` always returning true.
- `BeamRequester` token refresh functionality.

## [0.1.3] - Unpublished

### Changed

- Minor updates to API classes.

## [0.1.2] - Unpublished

### Changed

- `Beam.ready()` is idempotent; repeated calls have no additional effect after initialization.

## [0.1.1] - Unpublished

### Added

- `ready` function to initialize the SDK.
- Initial `Auth`, `Account`, and `Player` services.

## [0.1.0] - Unpublished

### Added

- Autogenerated schemas and APIs for the Beam web SDK.
- `BeamRequester` class for serializing and deserializing API requests and responses.
- `TokenStorage` interface and implementations for browser and Node environments.
- Automatic token refresh and Beam API request retry.

## [0.0.2] - Unpublished

### Added

- Initial core implementation with default requester using the Fetch API.

## [0.0.1] - Unpublished

### Added

- Initial project setup for the Web SDK.
