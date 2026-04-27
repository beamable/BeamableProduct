---
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

### 3. Publish content to the realm
```
beam_exec("content publish --manifest-ids global -q")
```
This pushes local content to the remote realm. Options:
- `--auto-snapshot-type LocalOnly` — save a local snapshot before publishing
- `--auto-snapshot-type SharedOnly` — save a shared snapshot (committed to version control)
- `--auto-snapshot-type Both` — save both

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
| `uint8`, `int32`, `int64` | `byte`, `int`, `long` | |
| `float`, `double` | `float`, `double` | |
| `bool` | `bool` | |
| `FString`, `FText`, `FName` | `string` | Serialized as JSON strings |
| `FGameplayTag` | `string` | Use `FGameplayTag::RequestGameplayTag` for deserialization |
| `FGameplayTagContainer` | `string` | Use `FGameplayTagContainer::FromExportString` for deserialization |
| `UClass*` | `string` | Converted to `FSoftObjectPath` when serializing |
| `TSoftObjectPtr<>` | `string` | When None serializes as empty string |
| `TArray<T>` | `List<T>` or `T[]` | Works for any supported element type |
| `TMap<FString, V>` | `Dictionary<string, V>` | Only `FString` keys supported |
| `FBeamOptional` (e.g. `FOptionalInt32`) | `Optional____` | Not serialized when `IsSet==false` |
| `FBeamSemanticType` | `string` or C# semantic equivalent | Serialized as JSON blob inside content |
| `FBeamArray`, `FBeamMap` | `ArrayOf`, `MapOf` | For nested `TArray<TArray<>>` / `TMap<,TMap<>>` with Blueprint support |
| `FBeamJsonSerializableUStruct` | C# class mapping your struct | Serialized as JSON object |
| `IBeamJsonSerializableUObject` | C# class mapping your class | Content UObjects should use `DefaultToInstanced, EditInlineNew`; use `TSoftObjectPtr<>` for asset references |

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
