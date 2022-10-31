


# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.18.7]
### Fixed
- dispose of native collections in `IPlatformRequester`


## [0.18.6]
### Fixed
- Realm scoped permissions are now respected


## [0.18.5]
### Fixed
- Deferred retry of failed uploads to the poll coroutine, to eliminate an infinite loop that could crash the app.


## [0.18.2]
### Changed
- Improved baked content performance by keeping data in a single file and limiting number of IO operations.


## [0.18.1]
### Fixed
- The `Editor 'namespace' but is used like a 'type'` error has been fixed
- The "Whats New" banner links to blog post if available
- Reference to Unity 2019.3 specific `HasOpenInstances` function removed in Unity 2018

## [0.18.0]
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

## [0.17.4]
- no changes

## [0.17.3]
### Added
- Added `RemoveDeviceId` method in `AuthService`
- Limit amount of elements displayed by `list_content` command in Admin console, allow to specify start index for `list_content` command

### Fixed
- Removes _Menu Window/Panels/1_ warning after opening schedule type dropdown on Unity 2019 and 2020
- Limit displayed admin console output


## [0.17.2]
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

## [0.17.1]
### Fixed
- calls to Leaderboard scoring API support large numbers
- schedule UI validation for daily and day-of-week schedules

## [0.17.0]
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


## [0.16.1]
### Fixed
- `ExecuteRolling` method of `Promise` now supports a condition on which stop execution
- No longer re-imports config-defaults without cause
- Batch imports Module Configuration files to improve speed
- No longer refreshes asset database on ContentIO FindAll()
- Allow Content Deserializer to consume incorrectly typed fields as empty values


## [0.16.0]
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

## [0.15.0]
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

## [0.14.3]
### Fixed
- Re-namespaced internal UnityEngine.UI.Extensions to Beamable.UnityEngineCopy.UI.Extensions so as to not collide with other versions.

## [0.14.2]
- no changes

## [0.14.1]
### Fixed
- Facebook SDK won't be referenced unless the Facebook setting is checked in the Account Management Configuration

## [0.14.0]
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


## [0.13.3]
### Fixed:
- Matchmaking state transition bug
- Increases heartbeat rate for MatchMaking


## [0.13.2]
### Fixed:
- Fixed isHttpError obsolete errors
- Removed use of Social.localUser.id from GameCenter Authentication

### Added:
- Support for GameCenter Authentication on iOS 13.5+


## [0.13.1]
### Fixed:
- GameCenter SDK errors with non-iOS il2cpp builds


## [0.13.0]
### Fixed:
* Fixed possible null reference exception in MustReferenceContent validation attribute

### Added
* GameCenter sdk Authentication Support
* Adds an optional field, activeListingLimit to Store Content

### Changed
* Switched MatchmakingService API to point to our new backend matchmaking service.


## [0.12.0]
(this release was skipped)


## [0.11.0]
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
