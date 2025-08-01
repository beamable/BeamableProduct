# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0]

### Changed
- Heartbeats are no longer sent when Realm is configured to use the Beamable websocket. 
- Able to use the new Client Code Generator from CLI that uses OpenAPI instead of the old one that uses Reflection
- `Core.Platform.Api` namespace moved into `Beamable.Api` namespace
- `Core.Platform` namespace moved into `Beamable` namespace
- Updated `Schedule Definition` Property Drawers to use `cron` expression values for better usability and flexibility in scheduling.
- Upgrade CLI to 5.3.0
- Beamable button includes SDK version number

### Fixed
- `Beam.SwitchToPid` resets content instance [3547](https://github.com/beamable/BeamableProduct/issues/3547)
- _Beam Services_ "generate client on build" setting stays configured between editor restarts
- Beamable button visual behaviour is consistent between all supported Unity versions

### Removed
- Admin console command `HEARTBEAT` has been removed.
- Beamable no longer attempts to automatically configure _Text Mesh Pro_ and _Addressables_.
- Beamable Environment Switcher is now part of the login flow.

### Added
- New Login window that uses CLI workflows rather than storing editor login information twice. 
- New Content Manager window that uses CLI workflows and receives dynamic updates.
- `BeamEditorContext.Microservices` property allows access to Microservice clients at editor time. [4102](https://github.com/beamable/BeamableProduct/issues/4102)
- New Validation for Cron Schedule Definition


## [2.4.0] - 2025-06-11 

### Fixed
- Fixed issue that CloudSaving could generate corrupted save files if the application was closed during saving process.
- Fixed incorrect `MailMessage` `expires` field parsing.
- Fixed issue with `WebSocketConnection` not sending updates after reconnection.
- Improve IAP error detection.
- Namespace error in `UpdateGPGSRealmConfigHelper`.

### Changed
- Upgrade CLI to 4.3.1
- Bake content will bake content from realm that is provided by `config-defaults` (used in builds) when available. Instead of using local content and requiring it to be the same as remote one, it will download and bake currently deployed manifest and its content.

### Removed
- Mongo third party libraries no longer exist
- SharpCompress third party library no longer exists
- dotnet `System` namespace dlls no longer exist
- Microservice types no longer exist in the Unity SDK

## [2.3.0] - 2025-05-08

### Added
- Helper menu option for updating Realm configuration for GPGS integration.
- Information about already installed versions in information about missing Dotnet.
- `Open portal for user` button in `BeamableBehaviour` inspector.

### Changed
- Upgrade CLI to 4.3.0
- `AccountManagementConfiguration` no longer overrides `BEAMABLE_GPGS` define symbol.
- Improved IAP Purchaser initialization flow.

### Fixed
- CID/PID Mismatch error message was too big for Unity popup. Now it uses a Beamable Custom Editor Window. [3933](https://github.com/beamable/BeamableProduct/issues/3933)
- Improved error detection for attaching identity providers in `PlayerAccounts`.
- Improved content selection in Content window

## [2.2.0] - 2025-04-04
### Added
- New Implementation for CloudSavingAPI using `ICloudSavingService` as Player SDK. Accessible by `BeamContext.CloudSaving`.
- New `BeamUnityFileUtils` static class to Handle File Operations.
- New LightBeam sample for `BeamContext.CloudSaving` basic operations.
- Added validation of PID input when calling `Beam.SwitchToPid`
- Update the Select Realm menu to allow updating config-defaults.

### Changed
- Upgrade CLI to 4.2.0
- StatsService now supports accessing Private Client Stats.
- Now `PlayerStat` class have the `AccessType` and `DomainType` of the Stat
- Refactored methods on `IStatsApi` and `IMicroserviceStatsApi` for a better name and usage by adding Enums instead of string values as parameters. Old ones were marked as Obsolete.

### Fixed
- `CloudSavingService` could not initialize correctly if a save file could not be found in storage. If that happens that file will be ignored and the system will use the Local one, if exists.
- Fixed an issue which attempting using Stats after refreshing `PlayerStats` did not returned updated values.
- `CloudSavingService` could not initialize correctly if a save file could not be found in storage. If that happens that file will be ignored and the system will use the Local one, if exists.

## [2.1.4] - 2025-03-26

### Fixed
- Admin Console - opens portal on user realm instead of the editor one. Useful when realm changed in runtime.

### Changed
- Upgrade CLI to 4.1.5

## [2.1.3] - 2025-03-10

### Fixed
- Fixed game crash on Android and iOS whenever a error occurred on `CloudSavingService`.

### Changed
- Updated logs on `CloudSavingService.HandleRequest` for better understanding of possible errors regarding manifest keys that doesn't match the server.

## [2.1.2] - 2025-03-05
### Added
- Added new method (`ForceUploadSave`) on `CloudSavingService` to force upload local save to cloud.

### Changed
- Updated logs on `CloudSavingService.HandleRequest` for better understanding of possible errors.

## [2.1.1] - 2025-03-04

### Fixed
- Handle case when dotnet is not installed on macOS.
- Renaming Content in Unity 6 works [3916](https://github.com/beamable/BeamableProduct/issues/3916)

## [2.1.0] - 2025-02-24

### Added
- Unity 6 Support
- `Beam.SwitchToPid` method as a replacement for `Beam.ChangePid` with extra functionality- if the passed PID is the same as the current one no action is performed.
- `Copy item ID to clipboard` menu item in Content Window.

### Fixed
- `Party.Invite()` no longer throws a null reference exception. [#3797](https://github.com/beamable/BeamableProduct/issues/3797)
- `UnityBeamablePurchaser` no longer adds null sku product ids into IAP catalog [#3702](https://github.com/beamable/BeamableProduct/issues/3702)
- `MailService.SearchMail(query)` now correctly returns all properties through the mail [#3817](https://github.com/beamable/BeamableProduct/issues/3817)
- Installing CLI uses correct dotnet tool manifest

### Changed
- Upgrade CLI to 4.1.1
- Added custom property drawer to federations in `SimGameType` scriptable objects. [#3628](https://github.com/beamable/BeamableProduct/issues/3628)
- `WebSocketConnection` will ensure proper disconnect/reconnect when switching players
- The methods `isEmailAvailable` and `IsThirdPartyAvailable` from the `AuthService` obsolete and adds the new `GetCredentialStatus` with overloads for both the email and third party.[#3700](https://github.com/beamable/BeamableProduct/issues/3700)
- `Beam.ChangePid` marked as Obsolete
- Dotnet is not installed locally anymore, if the required version is not installed yet, the SDK will prompt the user to install the right version.
- OpenAPI Generated SDK's `Message.expires` field is now an `OptionalLong`
  instead of `OptionalInt`

## [2.0.3] - 2025-02-13
- no changes

## [2.0.2] - 2024-12-17

### Fixed
- Possible `NRE` when opening the Beamable Button while having never logged in

## [2.0.1] - 2024-12-17

### Changed
- Upgrade CLI to 3.0.2

### Fixed
- `WebSocketConnection` no longer throws `WebSocketConnectionException` during normal reconnect flows
- `WebSocketConnection` will use the most recent JWT to attempt reconnection

## [2.0.0] - 2024-12-11

### Added

- Global dependency injection scope
- Leaderboard PSDK
- LightBeam UI Framework and several samples
- `RecoverAccountWithExternalIdentity` method includes a parameter, `attemptToMergeExistingAccount` that will prevent automatic account merging.
- _Beam Library_ window replaces the _Toolbox_ window.

### Changed

- Editor Account info is in the top-right Beamable button
- Editor Realm info is in the top-right Beamable button
- Uncaught promises use dead letter queue
- Cid and Pid are stored in `Beam.GlobalScope`
- Dotnet is automatically installed to the project's `/Library` folder
- Beam CLI is automatically installed to the project's `/Library` folder
- Baked content exists in a separate memory location than the deserialized `content.json` CDN cache

### Fixed

- Autogenerated SDK code url encodes query args and path args.
- Using a Beam CLI built from source in Unity will provide more helpful logs in case of failures
- Renaming content in newer Unity versions works correctly
- `ContentCache` now updates content tags after deserializing content.
- Fixed `EventView` end time calculation.
- 502 and 504 requests containing HTML are received as `RequesterExceptions`.
- Content alphabetizes fields on checksum calculation as well as publication.
- Listing content `activeFrom` field no longer resets every time it is viewed in the Inspector.
- `Json.Serialize` will treat non `IDictionary` values of `IEnumerable` as json arrays.
- Disabling Content inspectors no longer causes compiler errors.
- Corrected zero-fun issue while logged out of _Beam Library_. type `~tuna` into logged out window.
- Leaderboard `rankgt` field is not null when specifying outlier.
- Fix renaming content throwing infinite warnings
- Content cache evicts old content for new content id versions
- `PlatformRequester` will reattempt failed requests caused by SSL connection issues.
- Logged out windows show logged out view

### Removed

- Beamable Unity Style Sheet (BUSS)
- Theme Manager no longer exists
- Toolbox no longer exists
- Beamable prefabs are no longer accessible.
- Beamable Assistant no longer exists
- Autogenerated OpenAPI based SDK for Chatv2 and Matchmaking
- Legacy static based `ServiceManager` class and the `Beamable.Service` namespace.

## [1.19.23] - 2024-10-23

### Fixed
- Content tags change marks corresponding `ScriptableObject` as dirty
- Editor handles opening project when token is expired correctly.

## [1.19.22] - 2024-07-19
### Added
- All standard `ContentRef` subtypes have a constructor that takes a content id string
- `IPlatformRequester` includes `LatestServerTime` utility properties and functions

### Fixed
- `IAnalyticsTracker` unavailable error no longer thrown in editor when token expires

### Changed
- `NoConnectivityException` error messaging has more detail
- Content Manager publish popup no longer has "CHECK" tooltip

## [1.19.21] - 2024-06-18

### Added
- Core Configuration includes a `enableTokenAnalytics` option that will opt into sending an analytic event when a token expires (`access_token_invalid`), when a new token is issued via a refresh token (`got_new_token`), and when the beam context's token changes (`will_change_token`)
- `PromiseExtensions.ExecuteOnRoutines` utility method runs a set of promise generators on a given number of consumer routines

### Changed
- Editor content imports are debounced into groups.
- Editor content validation speed improved by using editor coroutines
- Editor content upload speed improved by using `ExecuteOnRoutines` utility
- Runtime content cache hydrates once, instead of once per content type
- Runtime content cache resolution is faster and allocates less memory by using `JsonUtilty`
- Runtime content download speed improved by using `ExecuteOnRoutines` utility

## [1.19.20] - 2024-05-31

### Fixed
- `BeamablePackages` utility no longer uses `EditorApplication.delay` call.
-  The `Lobby` now can be created passing the field `data` and that can be updated as well

## [1.19.19] - 2024-05-22
no changes

## [1.19.18] - 2024-05-08

### Fixed
- Prevent `WebSocketConnection` burst of exceptions by adding a delayed retry logic


## [1.19.17] - 2024-04-04
### Changed
- Baked content exists in a separate memory location than the deserialized `content.json` CDN cache

### Fixed
- Content cache evicts old content for new content id versions
- `PlatformRequester` will reattempt failed requests caused by SSL connection issues.
- Fix renaming content throwing infinite warnings

## [1.19.16] - 2024-03-2
### Fixed
- Disabling Content inspectors no longer causes compiler errors.
- Leaderboard `rankgt` field is not null when specifying outlier.

## [1.19.15] - 2024-03-07
### Changed
- CLI is only used if the global CLI matches the exact SDK version.
- Autogenerated SDK synced to match latest API (February 22, 2024).

### Fixed
- 502 and 504 requests containing HTML are received as `RequesterExceptions`.
- Content alphabetizes fields on checksum calculation as well as publication.
- Listing content `activeFrom` field no longer resets every time it is viewed in the Inspector.
- `RecoverAccountWithExternalIdentity` method includes a parameter, `attemptToMergeExistingAccount` that will prevent automatic account merging.
- `Json.Serialize` will treat non `IDictionary` values of `IEnumerable` as json arrays.

## [1.19.14] - 2024-02-06

### Fixed
- added previously missing meta file for `BeamAssemblyVersionUtil`.

## [1.19.13] - 2024-02-05

### Changed
- Marked `EventView` `endTime` field as obsolete, suggest using `GetEndTime` method instead.
- `PlayerAccounts`, `PlayerAnnouncements`, `PlayerFiends`, `PlayerParty`, and `PlayerStats` automatically await their own initialization before running methods.

## [1.19.12] - 2024-01-22

no changes

## [1.19.11] - 2024-01-12

### Fixed
- `CommerceService` uses `CoreConfiguration.CommerceListingRefreshSecondsMinimum` field to set a minimum delay between fetching store updates. The delay defaults to once a minute.

## [1.19.10] - 2024-01-05

no changes

## [1.19.9] - 2023-12-20

### Added

- Support for FederatedGameServer microservices

## [1.19.8] - 2023-12-15

### Changed

- `TournamentAPI` service starts using OpenApi bindings for `GetStandings` and `GetGlobalStandings` calls.

### Fixed

- Enums in microservices calls serialize correctly.
- Fix `BeamContext.Default` first call when called with extra define symbol defined: `BEAMABLE_ENABLE_BEAM_CONTEXT_DEFAULT_OVERRIDE`

## [1.19.7] - 2023-11-29

### Fixed

- Classes in microservices calls serialize correctly.

## [1.19.6] - 2023-11-22

### Added

- Classic SDK `TournamentEntry` has new fields: `tier`, `stage`, `nextStageChange`, and `previousStageChange`.

## [1.19.5] - 2023-11-15

### Added

- OpenAPI `TournamentEntry` has new fields: `nextStageChange` and `previousStageChange`.
- Autogenerated Session and Trial API.

## [1.19.4] - 2023-11-02

no changes

## [1.19.3] - 2023-10-26

no changes

## [1.19.2]  - 2023-10-11

### Fixed
- Content cache data is now saved in a namespaced path.
- Inventory is available on WebGL builds, and `IDependencyNameProvider.DependencyProviderName` uses a sanitized `PlayerCode`.

## [1.19.1] - 2023-09-22

### Fixed
- New enum serialization wasn't compatible with older versions of published content, so it was reverted.

## [1.19.0] - 2023-09-20

### Fixed

- `IsThirdPartyAvailable`, `RemoveThirdPartyAssociation`, and `IsExternalIdentityAvailable` methods no longer use invalid query args.
- Websocket connection authentication in WebGL builds.
- Fixed ContentRef property drawer on Unity 2021 LTS.
- `PlayerAccounts.Current` updates correctly after account switching.

### Changed

- Changed the unknown `PaymentService.ProviderId` value from "bogus" to "unknown".
- Payment ProviderId can be changed by injecting a custom `IPaymentServiceOptions` into the Beam Context scope.
- `Promise.Sequence` return `List<T>` in the same order as input `List<Promise<T>>`.
- `PlayerAccounts.Current` is a distinct instance from any element in the `PlayerAccounts` list.
- `PlayerPrefs` are no longer the source of truth for CID/PID. Instead, use the `IRuntimeConfigProvider`.
- `ConfigDatabase` is no longer used to store and load CID/PID.

### Removed
- Broken Console commands, `config set`, `config reset`, and `config useful`.
- `ConfigDatabase` is deprecated.

### Added

- Script symbol, `BEAMABLE_ENABLE_BEAM_CONTEXT_DEFAULT_OVERRIDE` will set `BeamContext.Default`'s PlayerCode to the PlayerCode used to creat the first `BeamContext`, such as through `BeamContext.ForPlayer()`.

## [1.18.0] - 2023-09-01

### Fixed

- Fixed `HttpUtility` throwing compilation error in Unity.
- `Reset` command works for realms configured to use Beamable notification channel.

### Added

- `IBeamableDisposableOrder` interface allows services to dispose in configurable order.

## [1.17.3] - 2023-08-21

### Added

- Add email validation

### Fixed

- Fixed wrong error code when providing wrong password while logging in.
- Fixed error when trying to get enum type from Microservice.

## [1.17.2] - 2023-08-10

### Fixed

- Announcements clientData not being deserialized correctly.

## [1.17.1] - 2023-08-10

### Fixed

- Content types from SAMS are visible in Content Manager in Unity.

## [1.17.0] - 2023-07-27

### Fixed

- `IsExternalIdentityAvailable` no longer returns 'true' if user_id is already in use by another player and contains special characters
- `IsThirdPartyAvailable` no longer returns 'true' if user_id is already in use by another player and contains special characters
- Avoid early initialization of `BeamEditor` when `Resources` are not available in Editor.
- Missing `TextReference` exception in the `LoadingIndicator` when entering and exiting Playmode quickly
- Disposed `CoroutineService` exception in the `BeamMainThreadUtil` when entering and exiting Playmode quickly
- `PlayerAccounts.SwitchAccount` method make sure that data is initialized before switching account

### Changed

- `IsExternalIdentityAvailable` takes an optional string `providerNamespace` instead of an optional string[] for `namespaces` parameter.
- `PlayerAccounts.IsExternalIdentityAvailable` resolves the `providerNamespace` automatically from parametrized type.
- `TextAreaAttribute` added to `EmailContent` body.

## [1.16.2] - 2023-07-12

### Added

- new `RecoverAccountWithRefreshToken` methods in the `PlayerAccounts` class.

### Fixed

- GPGS compilation issue- move non static action invoke out of a static method

## [1.16.1] - 2023-06-28

### Fixed

- Player Inventory PSDK no longer triggers `OnDataChanged` callbacks with incorrect empty list

## [1.16.0] - 2023-06-27

### Added

- Unity 2022 LTS Support
- Core configuration allows `BeamCLIPath` to be configured.
- `PerformLocalLogin` method in `SignInWithGPG` class
- Add gift field in `Announcement` class.

### Fixed

- Response serialization for external login request
- Autogenerated Scheduler API uses correct polymorphic serialization.
- Static method references non-static property in SignInWithGPG
- Beam CLI no longer leaves dangling processes.
- Intermittent `get_version can only be called from the main thread.` error due to uncached `BeamableDispatcher`.

### Changed

- Allow `env-overrides` config to override single values instead of all of them.
- Beamable's build preprocessor no longer runs in batch mode (CI/CD).
- Autogenerated Scheduler API uses correct polymorphic serialization.
- Cron expression preview now omits leading zeroes: for example " 1:23 PM" instead of "01:23 PM"
- Serialization of `DateTime` now supports optional format parameter for Beamable Serialization library.
- Skip Buss and old styling OnValidate while playing

### Removed

- `JsonSerialzable.Serialize` type constraints no longer require `class` or `new()`.

## [1.15.2] - 2023-05-18

### Added

- Autogenerated API layer for Beamable Proto-Actor based backend services.

### Changed

- Beamable network requests use the combined `X-BEAM-SCOPE` header instead of the `X-KS-CLIENTID` and `X-KS-PROJECTID` headers.
- Detect missing proguard rules during build.
- You can now create a SAMS with no Common Library (needed for UE)
- Added INSTANCE_COUNT to CLI BeamoService's Local Microservice Protocol for SAMS (default to 1)
- Added implementation to `beam services deploy --ids` command so that it now enables the given MS Ids when given any
- Added Docker Container Updates to CheckStatus command in CLI

### Fixed

- Using portal to remove the last item in a group while listening to "items" with the Inventory PSDK will cause the SDK to notice the change.
- Do not init Pubnub Services until needed.
- Fix compilation errors on Standalone platform when using `com.unity.mobile.notifications` package.

## [1.15.1] - 2023-05-04

### Fixed

- Detect issues with parsing baked content.
- Exiting playmode early no longer causes a service scope exception.

### Changed

- Beamable Environment window leaves version number and environment label unchanged when using preset buttons.

## [1.15.0] - 2023-04-27

### Added

- Added additional info about active listing limit default value in StoreContent.
- Limited Beam CLI bindings.

### Fixed

- Invalid package invalidations no longer occur for Beamable based assets.
- Content folders can be case insensitive.

### Changed

- Beamable button includes Beamable SDK version.

## [1.14.0] - 2023-03-31

### Fixed

- "Serialization depth limit" warning for `RealmView` on domain reloads.
- Realms are reloaded if absent on domain reload.
- Realm secret is reloaded on login.
- User is able to publish after reverting from Change Environment.

### Added

- `ILoadWithContext` interface will obligate a service registered as a singleton to be instantiated on `BeamContext.InitServices` call.
- `IsExternalIdentityAvailable` method added to AuthAPI and used in PlayerAccounts SDK during attaching external identity.

## [1.13.0] - 2023-03-22

### Added

- `ItemView` has new `contentId` field.
- `DisableBeamableCidPidWarningsOnBuild` option in _Project Settings/Beamable/Editor_ that will disable the CID/PID warning dialog on build.

### Fixed

- Editor no longer throws "Failed to refresh account" messages.
- AdminConsole works on webGL builds.
- CurrencyHud no longer throws exceptions.
- TournamentContent period field no longer loses focus.
- Switching environments will correctly sign out the current editor user.

### Changed

- CID/PID warning at build time is more descriptive.
- Functions names in third party Websocket library so now they should not conflict when user is using other libraries that depends on that library

## [1.12.0] - 2023-03-08

### Added

- Parse optional `proxyId` as `FederatedId` field for items related to `FederatedInventory` feature
- Possibility to disable property order dependence for content checksum.
- `Beam.ChangePid` allows the game to change the assigned pid. The pid will reset when the game restarts.
- `IPlatformRequesterErrorHandler` implementation can be added to `IDependencyProvider` to handle any uncaught Beamable Network errors.
- `PlayerInventory` has explicit methods for getting Items and Currencies with string references.
- `PlayerInventory` has load methods for Items and Currencies that retrieve data and call `Refresh()`.
- `SDKRequesterOptions<T>` has a field called `disableScopeHeaders` that will prevent CID/PID headers from being sent.

### Changed

- Exception on using `BeamContext` outside playMode
- The _config-defaults.txt_ file no longer controls the which CID/PID are used while in Editor. The _config-defaults.txt_ file will still control the CID/PID in a built game.
- Expose Google Play Game Services `ForceRefreshToken` option and set it to `true` by default
- `Beamable.Common` assembly name changed to `Unity.Beamable.Runtime.Common` to align with assembly definition file.

### Removed

- The Toolbox signin flow no longer allows for guest accounts.

### Fixed

- Fixed slow SDK installation process.
- Fixed Error Code for `NoConnectivityException` at `UnityBeamablePurchaser` on StartPurchase.

## [1.11.1] - 2023-02-16

### Changed

- Expose Google Play Game Services `ForceRefreshToken` option and set it to `true` by default

### Fixed

- `PlayerInventory` triggers `OnDataUpdated` events.
- `PlayerInventory` item properties can be `null` without throwing a `NullReferenceException`.

### Issues

- All content will appear as modified. This is because the content checksum algorithm changed to use alphabetical field ordering.

## [1.11.0] - 2023-02-09

### Added

- `PlayerInventory` supports storing player's inventory in offline mode
- `PlayerInventory` supports `UpdateDelayed` method
- `IFederatedLogin<T>` interface type available for Microservices
- "external" identities section has been added to a `User` class
- `BeamContext.Default.Accounts` now has `IsThirdPartyAvailable` and `IsEmailAvailable` methods which check if given credentials are available for usage.
- Added `BeamContext.Presence` layer to handle requesting/changing player's presence status
- `BeamContext.Social` now contains an event for players changing their presence status
- `PlayerAccounts` supports external identity auth.
- `BeamContext.Default.Instance` returns a `Promise<BeamContext>` that returns the default context.

### Changed

- `PlayerInventory` no longer duplicates items if retrieved with multiple `GetItems()` calls.
- Multiple calls to `PlayerInventory.Update()` will operate serially instead of compete for priority.
- Update banner in Toolbox will link to changelog instead of blog post.
- Refactored `BeamContext` initialization logic.

### Fixed

- `IBeamableDisposable.OnDispose()` is only called once per service, instead of once per service usage.
- Local Content Mode won't fail to load content if internet connection is lost mid-game.
- Fixed an issue with logging in and realm switching while being on an archived realm.
- Duplicate content is now displayed immediately in `Content Manager`
- Update banner in Toolbox will update both `com.beamable` and `com.beamable.server` package.
- Fixed issues with wrong content status and checksum on domain reload.
- `FilePathSelectorAttribute` no longer accesses `Application` in constructor.

## [1.10.3] - 2023-01-25

### Changed

- `IDependencyBuilder.Build()` accepts `BuildOptions` to control re-hydration options for dependency scopes

## [1.10.2] - 2023-01-12

### Changed

- non finite numbers such as `NaN` or `Infinity` will throw a `CannotSerializeException` exception if serialized by the `SmallerJson` utility.

### Fixed

- `RecoverFrom404` and `RecoverFromStatus` method respects HTTP status codes
- Skipping content assets check for current directory in case if `currList` is not initialized
- Corrected URL format for staging-portal in environment picker
- Tournament content can be scheduled for any ISO 8601 Period
- Detect using invalid AA assets inside `LoadTexture` helper method for `AssetReferenceSprite`

## [1.10.1] - 2023-01-06

### Fixed

- possible `NullReferenceException` during Content Manager initialization

## [1.10.0] - 2023-01-05

### Added

- Player Account PSDK
- `EditorDownloadBatchSize` setting in Content Configuration controls the batch download size for Content Manager. The default value is 100.
- Added SDK support for a direct Websocket connection to Beamable services toggleable via realm configuration.

### Changed

- Content Manager uses batch operations for better performance.
- Content Manager uses custom `ContentDatabase` instead of `AssetDatabase` to resolve assets.
- Content Ref Property Drawer no longer loads assets.

### Fixed

- Autogenerated SDK serializes sub fields that are typed objects
- `ValidationContext` service not found bug when using Local Content Mode.

## [1.9.1] - 2022-12-23

### Fixed

- Refresh Content Window process is more optimized
- Content with optional fields of misaligned type use default instance for target type.
- Content validation doesn't occur unless `ValidationContext` has been initialized.

## [1.9.0] - 2022-12-21

### Changed

- Add GPGS MonoBehaviour to AccountsFlow

### Fixed

- Domain Reload times are much faster when working with large amounts of content.
- Realm scoped permissions update in Editor view
- Corrupt cached content no longer crashes the game. Instead, the cache is invalidated.

## [1.8.0] - 2022-12-14

### Changed

- Default avatars use new style and theme

## [1.7.0] - 2022-12-09

### Added

- Local content mode
- Added `OnLeft`,`OnPromoted` and `OnKicked` event support in `PartyMember` class.

### Changed

- Portal opens to `https://portal.beamable.com` instead of `https://beta-portal.beamable.com`
- Content Manager and Toolbox have flat UI

### Fixed

- `NotificationService.Unsubscribe<T>` now correctly unsubscribes from events.
- Content classes with properties with backing field properties serialize correctly when upgrading directly from 1.2.10
- Regression in parsing nested `OptionalString` objects for content.

## [1.6.2] - 2022-11-17

### Fixed

- Fixed package updating error related to `Unable to add package`
- Fixed error preventing user from switching between Beamable accounts

## [1.6.1] - 2022-11-03

- no changes

## [1.6.0] - 2022-11-03

### Fixed

- Fixed empty list for OnElementsAdded and OnElementRemoved events in ObservableReadonlyList type
- Party events will not fire more than once anymore
- Fixed error preventing user from switching between Beamable accounts

## [1.5.2] - 2022-10-27

### Fixed

- Party state is nullified after leaving the party (either by `Leave` and `Kick` methods)
- Beamable button shouldn't overlap experimental package option

## [1.5.1] - 2022-10-20

- no changes

## [1.5.0] - 2022-10-14

### Added

- Presence heartbeat may be disabled in _Project Settings/Beamable/Core/Send Heartbeat_.
- Content Manager's download button shows Reset Content option in dropdown.
- `SimNetworkEventStream` has a `ISimFaultHandler` parameter that exposes error handling and callbacks for network outages.
- `PartyMember` and `ReceivedPartyInvites` observable lists in `PlayerParty` sdk.

### Fixed

- Content Manager Filter does not log exception when type query does not match any types.
- `CloudSavingService` no longer uploads manifests with missing objects.
- Fixed Content not getting resolved when `BeamContext.Default` is not used.

### Changed

- The `[Agnostic]` attribute is now obsolete. It is still usable, but assembly definitions should be used instead for code sharing.
- The `Stats` accessor on `IBeamableAPI` is now obsolete. Use `StatsService` instead.

## [1.4.0] - 2022-10-07

### Added

- Editor tooling for `SerializableDictionaryStringToSomething<T>` has context menu.
- Expanded/Collapsed state represented by icons in `Theme Manager`.
- New customer registrations will send an analytic event to Beamable.
- Autogenerated Beamable SDK available through `IServiceProvider` under the `Beamable.Api.Autogenerated` namespace.
- `SearchStats` method can now accept criteria values for `int`, `long`, `double`, `bool`, `string`, and their associated `List<T>` types.

### Fixed

- The default uncaught Promise handler no longer throws `IndexOutOfBounds` errors in high failure cases.
- Unity Asset Store releases will show correct version number in Unity Package Manager.

## [1.3.6] - 2022-09-22

### Fixed

- Fixed sending custom analytic events using `AnalyticsTracker.TrackEvent`

## [1.3.5] - 2022-09-15

### Added

- Utility apis for setting expiration on Mail Update and Mail Send requests

### Fixed

- VSP Package Attribution now uses requester.Cid rather than requester.AccessToken.Cid (which can be null at editor time)
- Game Type content has validation for minimum team sizes.
- Adding items to Inventory after changing authorized user works.

## [1.3.4] - 2022-09-08

### Added

- Support for nullable types in Content serialization.

### Fixed

- Fixed issue with windows not refreshing after login to Beamable.

### Changed

- Beamable requests have a 15 second application level timeout.

## [1.3.3] - 2022-09-01

### Added

- `ClearCaches` function on `StatsApi` to force invalidate stats cache.
- Beamable version number is displayed in Login Window's footer.
- Added `GetCloudDataContent` method in `ICloudDataApi` to simplify getting cloud data.

### Changed

- `UnityWebRequest` respect a 10 second timeout before reporting `NoConnectivityException`

### Fixed

- fixed issue with ChangeAuthorizedUser that cause the BeamContext to enter a bad state with respect to its MonoBehaviour systems

## [1.3.2] - 2022-08-25

### Added

- `PaymentsService` now supports receipt verification through the `VerifyReceipt` method.

## [1.3.1] - 2022-08-18

### Added

- A leaderboard can now be frozen using `LeaderboardService.FreezeLeaderboard` method to prevent additional scores to be submitted.

### Changed

- Editor tooling for `SerializableDictionaryStringToSomething<T>` now supports all subtypes

### Fixed

- iOS builds will no longer overwrite the Beamable user language preference.
- An expired token will no longer cause an unintended realm changes in rare cases.
- Logging into the editor will automatically put you in the realm (PID) defined in your `config-defaults.txt` file instead of incorrectly resetting you to your default realm.
- Correctly exposed `GetCurrentProject` method in `IAuthApi` to retrieve CID, PID and the project name. This functionality was already exposed in the `AuthService` class; we just moved it to the interface level to make it easier to access via `BeamContext.Api`.
- Microservices Manager window will now render correctly after importing the microservices module.

## [1.3.0] - 2022-08-10

### Added

- `StopListeningForUpdates` and `ResumeListeningForUpdates` methods in `ContentService` to manual control content refresh on ClientManifest deployment.
- `BeamableDispatcher` for editor scenarios to manage registering callbacks on the Unity Editor thread without needing an editor render frame.
- `Latest update` field for content item in Content Manager.
- Content items sort option by `Recently updated` in Content Manager.
- `Window/Beamable/Utilities/Change Environment` path to change the Beamable host parameters
- Added "experimental" package status support to the `PackageVersion` utility
- `Cid` and `Pid` field to `IBeamableRequester` interface
- `Social` list accessible through the `BeamContext`
- User's realm permission overrides apply in editor
- Added posibility of disable content serialization exceptions during content download to allow manual repair for corrupted files.
- Added to `ISocialApi` methods to make/accept/decline friend requests via `gamertag`s.
- Added Party SDK as a `PlayerParty` object inside `BeamContext` to manage parties
- Adds application version headers to all requests sent to Beamable

### Changed

- Fields of auto-properties with attribute SerializeField are now serialized for content classes under the name of the property.
- List of available to create `ContentTypes` in `Content Manager` contextual menu is now ordered alphabetically
- The Beamable host URL is no longer sourced from `config-defaults.txt`. Instead, it comes from the `BeamableEnvironment` class.
- Changed `PackageVersion` to accept "preview" prefix strings instead of requiring a direct match of the string "preview"
- Moved `JsonSerializable` to Beamable.Common assembly
- Moved some parts of the `ChatService` to Beamable.Common assembly
- Changed namespace of `Beamable.Pooling.ClassPool` to `Beamable.Common.Pooling.ClassPool`
- Account Management Flow will merge gamertags when existing login credential is detected, instead of always creating a new gamertag. This allows you to keep your gamertag on the realm.
- Improved performance of `PlayerInventory` sdk

### Fixed

- Beamable button in Unity toolbar should be in correct position for production packages
- Content validation callbacks now support invoking private methods in base classes
- BeamConsole accepts events after RESET command
- Too many `EventSystem` components on startup
- Fixed Beamable login error for archived & not existing realms
- In Events and Listings schedule windows calendar buttons with days earlier than today can't be clicked
- The `RankEntry` for current player is now mapped correctly in `LeaderBoardView`

## [1.2.10] - 2022-07-28

no changes

## [1.2.9] - 2022-07-27

no changes

## [1.2.8] - 2022-07-14

### Added

- `SetLanguage` function for `IAuthApi`

### Changed

- The `Language` field on the `IPlatformRequester` is no obsolete
- Beamable no longer sends "Accept-Language" headers

## [1.2.7] - 2022-07-14

### Changed

- New players will now get a locale and a location stat based on the Unity `Application.language` field.

## [1.2.6] - 2022-07-12

no changes

## [1.2.5] - 2022-07-07

### Fixed

- CurrencyHUD no longer throws null reference error when associated currency content has no addressable icon.

## [1.2.4] - 2022-06-24

no changes

## [1.2.3] - 2022-06-16

### Changed

- Content query strings are no longer case sensitive

## [1.2.2] - 2022-06-09

### Changed

- Content creation menu list is now sorted

### Fixed

- Editor will re-attempt failed requests before auto logging out
- DISABLE_BEAMABLE_TOOLBAR_EXTENDER directive now covers all scenarios
- Trying to rename a deleted content object will no longer log an exception

## [1.2.0] - 2022-05-25

### Added

- Unity 2021 LTS support.
- `PreventAddressableCodeStripping` Core Configuration setting that automatically generates a link.xml file that will preserve addressable types.
- `TryClaim` method in `EventService` to attempt a claim, even if one is not invalid.
- `GetDeviceId` method in `AuthService` to retrieve the current device id.
- `deviceIds` field on `User` object that provides all associated device ids.
- Content sorting option in `Content Manager`.
- Documentation to `IBeamableAPI` and all related accessors.
- `Subscribe<T>` method to `INotificationService` to avoid awkward serialization handling.
- Implicit conversion operators from `Optional<T>` objects wrapping a value type to matching `Nullable<T>` types.
- Inline style editor in BUSS theme manager.
- Added `LobbyService` and `PlayerLobby` to support new Lobby functionality.

### Changed

- `ManifestSubscription` subscription no longer accepts the scope field
- AccountHud logs a warning when pressed if there isn't an AccountManagementFlow in the scene.
- Increased the AdminFlow scroll speed
- InventoryFlow can now be configured at the GameObject level.
- Edit mode for Buss Style Card has been removed in favor of context menus for selector label, variables and properties.
- Claiming an event that a player never submitted a score for will report an accurate error message.
- Added tooltips to Microservice Manager elements which didn't have them.
- Microservice Manager buttons now highlight on hover.
- Beamable third party context systems register with a default order of -1000.
- Global style sheet is turned now into a list of global style sheets.
- Content tags are split on `','` characters in addition to `' '`s.
- A `IBeamableDisposable`'s `OnDispose` method can now resolve services from the `IDependencyProvider` that is being disposed.
- `HeartBeat` will now send heartbeat requests faster for our newer live backend services such as Lobbies
- Content validation for ID fields will now accept IDs without the prefix
- It is now possible to set background sprite as a main texture in SDF Image.
- It is now possible to choose 9-slice source and Pixels Per Unit multiplier in SDF image.

### Fixed

- StoreView prefab now works in landscape mode.
- Playmode ContentObject refresh with disabled domain reload on Unity 2019 and 2020.
- Reading content in offline mode will no longer throw an exception if there is offline cache available
- Android sign-in will always allow user to select an account.
- Editor time Content downloads ignore content where no C# class exists.
- Account management will no longer log an error after pressing change password button more than once.
- Content Manager no longer logs inaccurate warning after renaming content.
- Notification handling for multiple `BeamContext` instances.
- Listing 'sku' price type was incorrect. Fixed to 'skus'.
- The player's location is detected automatically
- Matchmaking no longer breaks after user switch from Account Management flow.

### Removed

- Unity 2018 LTS support.

## [1.1.4] - 2022-05-12

### Fixed

- Documentation links no longer direct to missing web pages.

## [1.1.3] - 2022-05-12

no changes

## [1.1.2] - 2022-04-21

### Fixed

- `AccessTokenStorage` no longer throws `ArgumentOutOfRangeException` when starting in offline mode

## [1.1.1] - 2022-04-15

### Fixed

- The namespace for `PropertySourceTracker` no longer invalidates the usage of `UnityEditor.Editor` as a type reference

## [1.1.0] - 2022-04-14

### Added

- Added `RecoverWith` extension method overloads to `Promise<T>` that allow for configuring  a promise to recover from failure over multiple attempts.
- Selected Buss Element section in Buss Theme Manager
- Added `AddAsChild(VisualElement, string, params string[])` to `BeamHintVisualsInjectionBag` to allow `BeamHintDetailConverter` functions to build and inject dynamically created `VisualElements` into Hint Details.

### Changed

- Behaviour of Add Style button in Buss Theme Manager
- Add Style button moved above Buss Style Cards section in Buss Theme Manager
- Buss Element selection improvement in Buss Theme Manager
- Application will check if there are redundant files in content disk cache on each start. All files but the one needed will be deleted to free disk space.
- All implementations of `[BeamContextSystem]` or `[RegisterBeamableDependencies]` will be preserved durring Unity code stripping
- Updated C#MS Publish window UI/UX
- Properties in Buss Style Card sorted alphabetically by default

### Fixed

- Constant "Invalid token, trying again" errors in the Editor after 10 days.
- Compilation error when using new `com.unity.inputsystem`
- Deferred retry of failed uploads to the poll coroutine, to eliminate an infinite loop that could crash the app.
- Content string fields can contain escaped characters, and won't be double escaped after download
- Fixed issue with `ReflectionCache` that happened on certain platforms when `IEnumerable` returning functions had to be parsed for `AttributesOfInterest`.
- Possible `DivideByZero` exxception that was thrown durring Toolbox usage

## [1.0.8] - 2022-03-25

### Added

- `CoreConfiguration.EnableInfiniteContextRetries` and `CoreConfiguration.ContextRetryDelays` options to allow developers to override what happens when a BeamContext cannot initialize

## [1.0.7] - 2022-03-24

This is a broken package. It includes changes from the 1.1.0 release. Please do not use this version.

## [1.0.6] - 2022-03-23

### Added

- Optional parameter `mergeGamerTagToAccount` to `IAuthService.LoginDeviceId` to support recovering an old account

## [1.0.5] - 2022-03-16

### Fixed

- Unity IAP failure to initialize on device won't hang `BeamContext.Default.OnReady`

## [1.0.4] - 2022-03-08

### Fixed

- `IBeamableRequester` implementations no longer dispose `UnityWebRequest` too soon

## [1.0.3] - 2022-03-03

### Fixed

- All `IBeamableRequester` implementations dispose `UnityWebRequest` after usage
- Beamable.Platform assembly definition references Facebook.Unity dll if it exists

## [1.0.2] - 2022-03-01

### Fixed

- Windows Microservices first time build issue regarding empty build directories

## [1.0.1] - 2022-02-24

### Added

- `IDeviceIdResolver` is now a dependency of the `AuthService`, and can be overriden to produce different device ids other than `SystemInfo.deviceUniqueIdentifier`
- Content Baking feature now also bakes content manifest which is used when there is no Internet connection

### Changed

- The `Promise.ExecuteRolling` function has been deprecated in favor of `Promise.ExecuteInBatchSequence`
- The startup sequence runs startup requests at the same time for speed improvements
- All Beamable Assembly Definitions use the `OverrideReferences` flag so they don't automatically reference project DLLs

### Fixed

- The `ResolveAll` content function no longer exceeds stack frame size limits
- Beamable assets are loaded with their full name so asset types won't collide
- Null references associated with Realm dropdown in Editor

## [1.0.0] - 2022-02-11

### Added

- `BeamContext` classes and new player centric SDK types like `PlayerInventory`
- Beamable Assistant window
- BUSS Theme Manager window

### Changed

- All Beamable Portal interactions use the new Beta Portal
- Consolidated internal assembly type scanning into `ReflectionCache` system. This improves editor time performance by an order of magnitude.
- `ServiceManager` no longer provides Beamable types. Use `BeamContext` instead.
- `Beamable.API.Instance` now returns `BeamContext.Default.Api` after waiting for the context initialization

### Fixed

- Deleting all items from an inventory subscription notifies client
- Immediately failed promises throw uncaught errors on access
- Disabling multiple content namespaces setting will disable both Publish button dropdown and content namespace dropdown
- Content baking will process correct number of objects regardless of local changes
- Baked content meta file warning should not appear anymore
- Immutable prefabs are no longer dirtied by the legacy skinning system
- The Reset command works on unsaved scenes
- `EventContent.StartDate` is kept in sync with schedule definition

## [0.18.2] - 2022-01-13

### Changed

- Improved baked content performance by keeping data in a single file and limiting number of IO operations.

## [0.18.1] - 2022-01-06

### Fixed

- The `Editor 'namespace' but is used like a 'type'` error has been fixed
- The "Whats New" banner links to blog post if available
- Reference to Unity 2019.3 specific `HasOpenInstances` function removed in Unity 2018

## [0.18.0] - 2021-12-16

### Added

- Content can be prebaked with game-builds to speed up content initialization
- `ScheduleDefinition` now supports CRON expression
- Minute support for scheduled listings
- Announcement content includes gifts in addition to attachments. Gifts support webhook calls.
- `scheduleInstancePurchaseLimit` field to the `ListingContent` to enable setting a purchase limit scoped to the schedule instance
- `SearchStats()` admin method is usable from client and microservice code.
- Device ID Deletion APIs (bulk and selective)

### Changed

- `BeamableEnvironment` has moved to the Runtime to enable sdk version checking at runtime
- `list_content` Admin Command displays limited results. You can specify start index for `list_content` command

### Fixed

- Renamed Beamable's iOS plugin for Google Sign-In from `GoogleSignIn` to `BeamableGoogleSignIn` to prevent name collisions with public plugins.
- `InventoryService.GetCurrent` is no longer limited by URI length
- only use `InitializeOnEnterPlayMode` in Unity 2019.3 or higher
- Removed unnecessary Unity asset reimport for identical content data.

## [0.17.4] - 2021-11-19

- no changes

## [0.17.3] - 2021-11-10

### Added

- Added `RemoveDeviceId` method in `AuthService`
- Limit amount of elements displayed by `list_content` command in Admin console, allow to specify start index for `list_content` command

### Fixed

- Removes _Menu Window/Panels/1_ warning after opening schedule type dropdown on Unity 2019 and 2020
- Limit displayed admin console output

## [0.17.2] - 2021-10-28

### Added

- `CoreConfiguration` to project settings to tweak how our Promise library handles uncaught promises by default
- `matchingIntervalSecs` for `SimGameType` allows game makers to specify the rate by which matchmaking occurs

### Changed

- `PromiseBase.SetPotentialUncaughtErrorHandler(handler, replace)` -- replaces by default, but supports adding handlers by passing `false` to second parameter
- New design of Microservices Publish Window with support for Storage Objects

### Fixed

- `CloudSavingService` serialization error caused by Invariant Culture
- Content Manager Publish window loading bar width
- Current Hud no longer emits null reference error if no image is assigned

## [0.17.1] - 2021-10-19

### Fixed

- calls to Leaderboard scoring API support large numbers
- schedule UI validation for daily and day-of-week schedules

## [0.17.0] - 2021-10-19

### Added

- Device id authentication support
- Steam third party authentication support
- Auto-complete text feature for `AdminFlow` prefab
- New default `currency.coins` currency that demonstrates client writable currency.
- Ability to remove a third party authorization with `RemoveThirdPartyAssociation` method in AuthService
- Cohort Settings for EventContent that support partitioning by player stats
- Event schedules for repeating events
- Listing schedules for repeating listings
- Support for archiving manifest namespaces.
- A `Fetch()` method to all subscribable SDKs that requests and returns the raw subscription data

### Changed

- An optional `forceRefresh` parameter to all subscribable SDK's `GetCurrent()` method that forces a network request
- `API.Instance.Requester` is now an `IBeamableRequester`
- The `Promise` class is no longer static, and extends from `Promise<Unit>`
- The realm dropdown now has a loading spinner on realm switches
- Content Inspector datepicker with no user given value no longer constantly updates
- Content deletion popup opens as separate window
- Microservice separator (draggable) moved directly under log group

### Fixed

- If no internet connection exists on startup, `API.Instance()` will retry every 2 seconds until a connection is established
- Able to build games to device

## [0.16.1] - 2021-09-30

### Fixed

- `ExecuteRolling` method of `Promise` now supports a condition on which stop execution
- No longer re-imports config-defaults without cause
- Batch imports Module Configuration files to improve speed
- No longer refreshes asset database on ContentIO FindAll()
- Allow Content Deserializer to consume incorrectly typed fields as empty values

## [0.16.0] - 2021-09-21

### Added

- Support for disabling Unity Domain Reload
- Content console commands (GET_CONTENT, LIST_CONTENT, CONTENT_NAMESPACE, SET_CONTENT_NAMESPACE)
- Easy custom content class creation in `Create/Beamable/Content Types`
- Resetting content to the server state under `Window/Beamable/Utilities/Reset Content`
- `MustBeSlugString` content validation with configurable option to allow underscores
- `OptionalBoolean` type for content
- Leaderboard Apis that support fetching partition/cohort cached assignment transparently
- Ability to disable VIP currency awards on Mail Rewards
- PlayerSettings scripting define symbols are saved in Diagnostics file
- Beamable package version Toolbox announcement

### Changed

- Request and Responses to and from Beamable are GZipped if larger than 1K
- Leaderboard Content supports partitioning, max size, and cohorting
- Leaderboard Update api will transparently fetch cached assignment
- `PlayerStatRequirements` now support providing the domain and access of stats
- `MustBeOneOf` content validation attribute now supports Optional types
- Beamable Platform errors all extend from `RequesterException` in Unity Client and microservice code
- Redesigned internal Toolbox announcements
- Content Manager publish flow shows Realm and Namespace for confirmation
- `ExecuteRolling` method of `Promise` now supports a condition on which stop execution

### Fixed

- Added missing attributes for content classes
- SocialService `SocialList` serialization
- Account Management Flow third party login buttons use correct third parties
- Content Manager Window item selection is cleared after changing the Namespace
- Adjusted confirm window look while trying to delete any content
- Content Manager Popups refresh after Unity domain reload

## [0.15.0] - 2021-08-24

### Added

- Verbose logging capability available in Project Settings
- Exposed cohort requirements for listing content
- Admin commands: ADD_CURRENCY, SET_CURRENCY, GET_CURRENCY
- Tournaments now supports the ability to specify group/guild-based rewards
- Tournaments now support player score rewards in addition to the existing rank rewards
- Tournaments allow developers to specify how many stages a player regresses if they do not participate in a cycle
- Inventory now supports setting properties on currencies at runtime

### Fixed

- Logging back into the Toolbox will remember your Realm selection per game
- Positioning of Validate, Publish and Download windows available from Content Manager

## [0.14.3] - 2021-08-20

### Fixed

- Re-namespaced internal UnityEngine.UI.Extensions to Beamable.UnityEngineCopy.UI.Extensions so as to not collide with other versions.

## [0.14.2] - 2021-08-19

- no changes

## [0.14.1] - 2021-08-18

### Fixed

- Facebook SDK won't be referenced unless the Facebook setting is checked in the Account Management Configuration

## [0.14.0] - 2021-08-18

### Fixed

- Integration for Unity In-App-Purchasing 3.x.x packages
- Content references will update after a manifest subscription update

### Changed

- Rearranged the Portal button and Account button in Toolbox
- Rearranged the Content Count label in Content Manager
- Password reset codes can use PINs instead of UUIDs

### Added

- WebGL build support
- Multiple content namespaces, both in Editor and Runtime. Must enable in Project Settings
- Facebook Limited Login (iOS) Authentication
- `"portal"` console command, which opens portal to the current player's admin page
- Multi-object editing support for Content Reference selector
- New editor tooling for ISO date strings in Content Objects
- Realm Picker in the top right of Content Manager
- Last publish date in bottom-right of Content Manager
- ISerializationCallbackReceiver support for Content Object serialization
- Async support for `Promise<T>` types
- Added Donate api call method to GroupApi

## [0.13.3] - 2021-08-13

### Fixed

- Matchmaking state transition bug
- Increases heartbeat rate for MatchMaking

## [0.13.2] - 2021-08-12

### Fixed

- Fixed isHttpError obsolete errors
- Removed use of Social.localUser.id from GameCenter Authentication

### Added

- Support for GameCenter Authentication on iOS 13.5+

## [0.13.1] - 2021-08-05

### Fixed

- GameCenter SDK errors with non-iOS il2cpp builds

## [0.13.0] - 2021-08-02

### Fixed

- Fixed possible null reference exception in MustReferenceContent validation attribute

### Added

- GameCenter sdk Authentication Support
- Adds an optional field, activeListingLimit to Store Content

### Changed

- Switched MatchmakingService API to point to our new backend matchmaking service.

## [0.12.0]

(this release was skipped)

## [0.11.0] - 2021-06-29

### Changed

- Console Configurations ToggleKey will be reset to BackQuote.

### Fixed

- Fixed Promise multithread safety.
- Fixed ContentRef property drawer showing invalid references when deleted.
- Fixed MustReferenceContent validation  for lists of Content References.

### Added

- Support for the new Unity Input Manager System.
- Added OnUserLoggingOut event available from API. The event fires before a user switches account.
- Doc Url to package.json.
- Event phase validation. Events can no longer have zero phases. This may lead to disappearing Event Phases if your Beamable version is mismatched.
- Switched MatchmakingService API to point to our new backend matchmaking service.
