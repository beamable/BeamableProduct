# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Native **React Native** build target (`dist/react-native`), selected automatically by Metro
  via the package `exports` `"react-native"` condition. Ships AsyncStorage-backed token,
  config, and content storage (`ReactNativeTokenStorage`, `ReactNativeConfigStorage`,
  `ReactNativeContentStorage`) as the built-in default — no explicit `tokenStorage` needed on
  React Native. Adds the `@beamable/sdk/react-native/polyfills` side-effect entry (installs the
  URL polyfill Hermes lacks) and optional peer deps `@react-native-async-storage/async-storage`
  and `react-native-url-polyfill`. Replaces the standalone `@beamable/sdk-react-native` adapter.
- `TokenStorage.hydrate()` — a concrete no-op hook (overridden by the React Native storage) the
  SDK awaits at the start of `connect()` so asynchronously-persisted tokens are loaded before
  the synchronous `isExpired` check.
- `BeamConfig.realtime.enabled` — opt out of the realtime websocket at init (defaults to `true`). Lets the SDK be used as a pure API client when there's no player to sustain a realtime session.
- `Beam.connectRealtime()` / `Beam.disconnectRealtime()` — public methods to start/stop the realtime websocket on demand (e.g. after creating a player via `beam.auth.loginAsGuest()`), and to cleanly tear the connection down.
- `BeamBaseConfig.host` — pass an explicit platform host URL to `Beam.init()` / `BeamServer.init()`.
  A built-in URL resolves to `dev`/`stg`/`prod` and any other URL becomes a custom environment,
  taking precedence over `environment`. This makes the CLI's `.beamable/config.beam.json`
  `{ cid, pid, host }` shape usable verbatim.
- `BeamEnvironmentRegistry.fromHost(host)` and `BeamEnvironmentRegistry.findByApiUrl(host)` —
  resolve a full `BeamEnvironmentConfig` from a host URL, trailing-slash-insensitive.
- `resolveBeamConfig()`, exported from the new Node-only `@beamable/sdk/node` entry point — walks
  up from a directory to the nearest `.beamable/config.beam.json` carrying both `cid` and `pid`,
  falling back to the `BEAM_CID` / `BEAM_PID` / `BEAM_HOST` env vars, and returns `{}` rather than
  throwing when nothing is found. Adds the `ResolvedBeamConfig` and `ResolveBeamConfigOptions` types.
- `beam.messageRail` / `beamServer.messageRail(playerId)` — a `MessageRailService` with
  `optIn(federationId, registrationData?)` and `optOut(federationId)` for player opt-in and
  opt-out on a message rail (`push`, `email`, `ingame`, or any deployed `IMessageRailFederation`
  id), plus the `MessageRailFederationId` type. Re-calling `optIn` refreshes a rotated push token.
- Generated API surface for campaigns, data bindings, and the message rail: `CampaignApi`
  (draft, publish, status, funnel, deactivate, archive, reactivate), `DataBindingApi`,
  `MessageRailApi` (`register`, `unregister`, `messages`, `staging`), and segment property
  recompute on `SegmentsApi`.
- `beam.analytics` / `beamServer.analytics(playerId)` — an `AnalyticsService` with `track(event)`,
  `trackBatch(events)` and the never-throwing `trackSafely(event)`, plus the `AnalyticsEvent` type
  and the `FUNNEL_CATEGORY` constant. The SDK could previously only *query* analytics
  (`analyticsPostQuery`) and had no way to emit at all, so a web or React Native game could not
  report campaign funnel stages the way Unity and the native push SDKs do.
- `beam.mail` / `beamServer.mail(playerId)` — a `MailService` with `list(params)`, `get(id)`,
  `markAsRead(id)` and `update(params)`, plus the `MailState` constant object and the
  `MailStateValue`, `MailListParams` and `MailUpdateParams` types.
- Campaign funnel `Opened` is now reported **automatically** when mail sent by a campaign moves
  from `Unread` to `Read` through `MailService`, so a game writes no tracking code of its own.
  Push gets that stage from the handset echoing the notification payload back; in-game mail has no
  handset, so this is the equivalent interception point. Reporting is fire-and-forget and never
  fails the read — including when `AnalyticsService` was not registered, where reading
  `beam.analytics` throws rather than returning undefined. Register `MailService` and
  `AnalyticsService` together, or in-game campaigns silently report zero engagement.
- `metadata` on the generated `Message`, `SendMailRequest` and `SendMailObjectRequest` schemas —
  the opaque per-message map a campaign message rail stamps its `beam_outreach` / `trackId`
  attribution onto.

### Changed

- `Beam.init()` now skips the realtime connection when `realtime.enabled` is `false` instead of always connecting.
- `getUserDeviceAndPlatform()` now detects React Native (`navigator.product === 'ReactNative'`) and
  reports `{ deviceType: 'Mobile', platform: 'React Native' }` instead of falling through to the
  Node/Desktop branch.
- `BeamServer.connect()` now awaits `tokenStorage.hydrate?.()` before reading token data, matching
  `Beam.connect()`.
- The package is no longer `sideEffects: false`; it declares `["**/react-native/polyfills*"]` so
  bundlers keep the polyfill import.

### Fixed

- The realtime websocket no longer fails with close 1006 on a device or emulator when the realm's
  client defaults advertise a loopback socket host. A `localhost` / `127.0.0.1` socket host is now
  retargeted to the configured `apiUrl` host, preserving the socket's own scheme and port.
- `BeamServerWebSocket` no longer rewrites `localhost` to `host.docker.internal` in the browser,
  which made the portal's socket target an unresolvable host and closed the connection with 1006.
  The Docker rewrite still applies to in-container Node microservices.
- Content manifest sync no longer sends the account id in the manifest `uid` query slot. Both the
  checksum and public-JSON manifest calls were one argument short, so `accountId` landed in `uid`
  instead of `gamertag`.

## [1.2.1] - 2026-06-03

### Fixed

- Content manifest fetch now treats a 404 response as an empty manifest instead of throwing, fixing portal extension startup when no content has been published.

## [1.2.0] - 2026-05-27

### Added

- Generated Stripe payment API calls: checkout sessions, webhook setup, and return URLs.

### Changed

- Updated auto-generated APIs and content manifest schemas to latest OpenAPI specs.

### Fixed

- Fixed useless regex escapes in `BeamJsonUtils`.
- Fixed session-start event for Athena pipeline.

## [1.1.1] - 2026-04-16

### Fixed

- `createStandaloneRequester` no longer requires a `pid`. When omitted, the `X-BEAM-SCOPE` header uses only the CID.

## [1.1.0] - 2026-04-16

### Added

- `codegen` script in `package.json` to regenerate TypeScript API types from OpenAPI specs via the CLI.
- New generated APIs: `BillingApi`, `CustomerApi`, `PlayerSessionApi`, `PlayerStatsApi`.
- `BeamWebSocket` now sends a `session-start` frame as the first message after connecting, carrying device and platform info that browsers cannot set via WebSocket upgrade headers.

### Fixed

- Code generator now quotes TypeScript property names that contain invalid identifier characters (e.g., `x5t#S256`).
- Code generator now produces distinct method names for PATCH endpoints instead of colliding with GET.
- `tsdown.config.ts` updated to use `import.meta.url` instead of `__dirname` for ES module compatibility.
- Code generator now correctly emits `w: true` (auth flag) for endpoints that require a bearer token. Previously, after the `ForcePlayerScopedAuth` OpenAPI processor renamed the security scheme from `user` to `auth` and `Reserailize` stripped reference info, `DetermineAuth` failed to detect auth requirements for all `basic` service endpoints — causing the SDK to omit the `Authorization` header on calls like `GET /basic/accounts/me`.
- HTTP response parsing now preserves precision for int64 values (e.g., player IDs, gamer tags). Previously `JSON.parse` rounded large integers before the reviver could convert them to `BigInt`, producing incorrect IDs like `70820408384930820` instead of `70820408384930816`. A new `BeamJsonUtils.parse` pre-quotes integers >10 digits before parsing so they reach the reviver as strings.

### Changed

- Updated auto-generated APIs and schemas to latest OpenAPI specs.
- Web code generator now only emits header parameters that `makeApiRequest` actually supports (currently `X-BEAM-GAMERTAG`); unsupported headers like `X-BEAM-TIMEOUT` are no longer added to generated method signatures.

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
