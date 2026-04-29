---
name: beam-manage-content
description: Manage Beamable content — check status, sync, publish, and create custom content types.
---

# Manage Content

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- Authenticated with `beam login` or saved credentials

## Steps

### 1. Check content status
```
beam_exec("content status")
```
Shows the state of each content object: Created (local only), Modified, Deleted, Conflicted, or UpToDate. Add `--show-up-to-date` to see all content including unchanged items.

#### `beam content status` options
| Option | Type | Description |
|---|---|---|
`--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest
`--show-up-to-date` | flag | Show up to date content
`--limit` | int | Limit content displayed amount (default: 100)
`--skip` | int | Skips content amount


### 2. Sync local content with remote (CAUTION)
```
beam_exec("content sync --manifest-ids global")
```
Without flags, sync does nothing. You must specify what to sync:
- `--sync-created` — DELETE locally-created content not in the remote manifest
- `--sync-modified` — DISCARD local changes on modified files
- `--sync-conflicts` — DISCARD local changes on conflicted files
- `--sync-deleted` — RESTORE files that were deleted locally

**WARNING**: `--sync-modified` and `--sync-conflicts` are DESTRUCTIVE and IRREVERSIBLE. They discard your local changes in favor of the remote version. Always check `content status` first.

#### `beam content sync` options
| Option | Type | Description |
|---|---|---|
`--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest
`--filter-type` | ContentFilterType | Defines the semantics for the `filter` argument. When no filters are given, affects all existing content.
ExactIds => Will only add the given tags to the ','-separated list of filters
Regexes => Will add the given tags to any content whose Id is matched by any of the ','-separated list of filters (C# regex string)
TypeHierarchy => Will add the given tags to any content of the ','-separated list of filters (content type strings with full hierarchy --- StartsWith comparison)
Tags => Will add the given tags to any content that currently has any of the ','-separated list of filters (tags)
`--filter` | string | Accepts different strings to filter which content files will be affected. See the `filter-type` option
`--sync-created` | flag | Deletes any created content that is not present in the latest manifest. If filters are provided, will only delete the created content that matches the filter
`--sync-modified` | flag | This will discard your local changes ONLY on files that are NOT conflicted. If filters are provided, will only do this for content that matches the filter
`--sync-conflicts` | flag | This will discard your local changes ONLY on files that ARE conflicted. If filters are provided, will only do this for content that matches the filter
`--sync-deleted` | flag | This will revert all your deleted files. If filters are provided, will only do this for content that matches the filter
`--target` | string | If you pass in a Manifest's UID, we'll sync with that as the target. If filters are provided, will only do this for content that matches the filter


### 3. Publish content to the realm
```
beam_exec("content publish --manifest-ids global -q")
```
This pushes local content to the remote realm.

#### `beam content publish` options
| Option | Type | Description |
|---|---|---|
`--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest
`--auto-snapshot-type` | AutoSnapshotType | Defines if after publish the Content System should take snapshots of the content.
None => Will not save any snapshot after publishing
LocalOnly => Will save the snapshot under `.beamable/temp/content-snapshots/[PID]` folder
SharedOnly => Will save the snapshot under `.beamable/content-snapshots/[PID]` folder
Both => Will save two snapshots, under local and shared folders
`--max-local-snapshots` | int | Defines the max stored local snapshots taken by the auto snapshot generation by this command. When the number hits, the older one will be deletd and replaced by the new snapshot


### 4. Create additional manifests (optional)
```
beam_exec("content new-manifest <manifest-id>")
```
The default manifest is `global`. Create additional manifests to organize content into groups.

## Content File Location by Engine

Content files must live where both the game engine and microservices can reference them.

### Unity
Place content types in `Assets/Beamable/Common/` so they are shared between the Unity project and microservices in `/BeamableServices/`.

**SDK documentation**: `https://help.beamable.com/Unity-<VERSION>` (e.g. `https://help.beamable.com/Unity-2.2`). To find the version, read `Packages/manifest.json` in the Unity project and look for the `com.beamable` or `com.beamable.server` package version.

### Unreal
Content types defined as `UBeamContentObject` subclasses live in your Unreal project modules. Microservices live under the root project in `/Microservices/`. For custom types used in microservices, you must also declare C# equivalents (see the serialization table below).

**SDK documentation**: `https://help.beamable.com/Unreal-<VERSION>` (e.g. `https://help.beamable.com/Unreal-2.2`). The Beamable version is stored in `Plugins/BeamableCore/Content/Info/BeamableInfoData.uasset`, which is a binary file and cannot be read directly. Ask the user for their Beamable SDK version to build the docs URL.

### Standalone Microservice
Place content types in a common shared project that all microservices reference. This avoids duplicating type definitions across services.

## Creating Custom Content Types

Use `beam_list_types("content")` to discover all existing Beamable content types and their fields before creating custom ones.

### Discovering existing content types
```
beam_list_types("content")
```
This returns all `[ContentType]`-annotated types — their names, C# class names, namespaces, and serializable fields. Examples of built-in types:
- `ItemContent` (`[ContentType("items")]`) — inventory items with icon and federation support
- `CurrencyContent` (`[ContentType("currency")]`) — currencies with starting amounts
- `AnnouncementContent` (`[ContentType("announcements")]`) — announcements with scheduling

### Creating a custom content type (C# — Unity and Microservices)
Subclass `ContentObject` and apply the `[ContentType]` attribute:
```csharp
using Beamable.Common.Content;

[ContentType("weapons")]
[System.Serializable]
public class WeaponContent : ContentObject
{
    public int damage;
    public float attackSpeed;
    public string description;
}
```

### Extending existing content types (C#)
You can also subclass existing Beamable content types:
```csharp
[ContentType("magic_items")]
[System.Serializable]
public class MagicItemContent : ItemContent
{
    public int manaCost;
    public string spellEffect;
}
```

### Creating a custom content type (Unreal C++)
In Unreal, subclass `UBeamContentObject` or any SDK subtype (`UBeamItemContent`, `UBeamGameTypeContent`, etc.). Each type must define a unique content type ID via a naming-convention function:
```cpp
UCLASS(BlueprintType)
class BEAMABLECORE_API UBeamCurrencyContent : public UBeamContentObject
{
    GENERATED_BODY()
public:
    UFUNCTION()
    void GetContentType_UBeamCurrencyContent(FString& Result){ Result = TEXT("currency"); }

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FBeamClientPermission clientPermission;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int64 startingAmount;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, DisplayName="Federation")
    FOptionalBeamFederation external;
};
```
- Annotate properties with `EditAnywhere` and `BlueprintReadOnly` (or `BlueprintReadWrite` if you write utilities to create the objects)
- The function name must follow the pattern `GetContentType_<ClassName>`
- For built-in types (`UBeamCurrencyContent`, `UBeamItemContent`, etc.), C# equivalents already exist in the Microservice SDK
- For your own custom types used in microservices, you must declare C# equivalents using the serialization table below

### Unreal to C# serialization mapping
When accessing Unreal-defined content in microservices, map types as follows:

| C++ Type | C# Equivalent | Notes |
|---|---|---|
| `uint8` | `byte` |  |
| `int16` | `short` |  |
| `int32` | `int` |  |
| `int64` | `long` |  |
| `bool` | `bool` |  |
| `float` | `float` |  |
| `double` | `double` |  |
| `FString` | `string` | Serialized as JSON strings |
| `FText` | `string` | Serialized as JSON strings |
| `FName` | `string` | Serialized as JSON strings |
| `FGuid` | `Guid` |  |
| `FDateTime` | `DateTime` |  |
| `FGameplayTag` | `string` | Use FGameplayTag::RequestGameplayTag for deserialization |
| `FGameplayTagContainer` | `string` | Use FGameplayTagContainer::FromExportString for deserialization |
| `TSoftObjectPtr<>` | `string` | Serializes as FSoftObjectPath; empty string when None |
| `TArray<T>` | `List<T> or T[]` | Works for any supported element type |
| `TMap<FString, V>` | `Dictionary<string, V>` | Only FString keys supported |
| `FOptional<T>` | `Optional<T>` | Not serialized when IsSet==false |
| `FBeamArray` | `ArrayOf` | For nested TArray<TArray<>> with Blueprint support |
| `FBeamMap` | `MapOf` | For nested TMap<,TMap<>> with Blueprint support |
| `FBeamJsonSerializableUStruct` | `C# class` | Serialized as JSON object |
| `IBeamJsonSerializableUObject` | `C# class` | Use DefaultToInstanced, EditInlineNew for content UObjects |


### Key content type rules
- The `[ContentType("name")]` attribute defines the type name used in content IDs (e.g., `weapons.sword_01`)
- Type names cannot contain dots (reserved for the ID format `{type}.{name}`)
- Use `[ContentField("json_name")]` to control JSON serialization names
- Use validation attributes: `[CannotBeBlank]`, `[MustBeNonNegative]`, `[MustBeDateString]`
- Use `Optional<T>` wrapper types for optional fields (e.g., `OptionalString`, `OptionalInt`)
- Content files are stored locally in `.beamable/content/<realm-id>/<manifest-id>/`

## Filtering Content

Filter by manifest, type, IDs, or tags:
```
beam_exec("content status --manifest-ids global,events")
beam_exec("content sync --filter-type TypeHierarchy --filter weapons --manifest-ids global")
beam_exec("content publish --manifest-ids events -q")
```

## Common Pitfalls
- **Sync without flags does nothing.** You must explicitly specify `--sync-created`, `--sync-modified`, etc.
- **`--sync-modified` discards LOCAL changes**, not remote. This is the opposite of what many expect.
- **Always check `content status` before sync or publish** to understand what will change.
- **The default manifest is `global`**. If you have multiple manifests, specify them with `--manifest-ids`.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was done**: Which content operation was performed (status check, sync, publish, or content type creation), and what changed — how many items were synced, published, or created.
2. **Where the files live**:
   - Content files: `.beamable/content/<realm-id>/<manifest-id>/` — JSON files, one per content object. Each file contains the serialized content data including its type, ID, and all field values.
   - Custom content types (C#): Wherever the user placed them — typically `Assets/Beamable/Common/` for Unity or a shared project for standalone microservices. These define the schema that content files must conform to.
   - Custom content types (Unreal): In the project's C++ modules as `UBeamContentObject` subclasses. C# equivalents must also exist if the content is accessed from microservices.
   - Manifest config: `.beamable/content/<realm-id>/` — each subdirectory is a manifest (default is `global`).
3. **Why specific choices were made** — explain the reasoning:
   - **Sync flags used**: If sync was performed, explain what each flag did — `--sync-modified` overwrites local changes with remote versions (useful when the remote is the source of truth), `--sync-created` removes local-only content not in the remote (cleans up stale local data). If destructive flags were avoided, explain why.
   - **Manifest choice**: Content is organized into manifests. The `global` manifest is the default and typically holds all game content. Additional manifests (e.g., `events`, `seasons`) are used to partition content for independent publish cycles or team ownership.
   - **Content type design**: If a custom content type was created, explain the `[ContentType("name")]` naming convention (the string becomes the prefix in content IDs like `weapons.sword_01`), why specific fields were chosen, and how `[ContentField]` and validation attributes help maintain data integrity.
   - **Engine-specific placement**: Explain why content types were placed in a particular directory — Unity requires them in a path shared between the editor and microservices, Unreal requires C++ types with matching C# equivalents for cross-language serialization.
4. **How to verify**: Remind the user to run `content status` to confirm the current state, and that `content publish` pushes local content to the realm where game clients and microservices can access it.
