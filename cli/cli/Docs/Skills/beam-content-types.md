---
name: beam-content-types
description: Create custom content types for Unity, Unreal, and Microservices
---

# Custom Content Types

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- Authenticated with `beam login` or saved credentials

## Discover existing content types
Before creating custom types, check what already exists. Use the `beam-get-source` skill to read SDK source files for built-in content types. Common built-in examples:
- `ItemContent` (`[ContentType("items")]`) — inventory items with icon and federation support
- `CurrencyContent` (`[ContentType("currency")]`) — currencies with starting amounts
- `AnnouncementContent` (`[ContentType("announcements")]`) — announcements with scheduling

## C# content types (Unity and Microservices)

### Creating a new content type
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

### Extending existing content types
Subclass any built-in content type:
```csharp
[ContentType("magic_items")]
[System.Serializable]
public class MagicItemContent : ItemContent
{
    public int manaCost;
    public string spellEffect;
}
```

### Content type rules
- The `[ContentType("name")]` string becomes the prefix in content IDs (e.g., `weapons.sword_01`)
- Type names **cannot contain dots** (reserved for the `{type}.{name}` ID format)
- Use `[ContentField("json_name")]` to control JSON serialization names
- Content files are stored locally in `.beamable/content/<realm-id>/<manifest-id>/`

### File placement by engine
- **Unity**: Place in `Assets/Beamable/Common/` so types are shared between the Unity project and microservices in `/BeamableServices/`
- **Standalone Microservice**: Place in a shared project that all microservices reference

## Validation attributes
Add validation constraints to content fields:
```csharp
[ContentType("quests")]
[System.Serializable]
public class QuestContent : ContentObject
{
    [CannotBeBlank]
    public string title;

    [MustBeNonNegative]
    public int rewardAmount;

    [MustBeDateString]
    public string expirationDate;
}
```
Available validators:
- `[CannotBeBlank]` — string must not be null or empty
- `[MustBeNonNegative]` — numeric value must be >= 0
- `[MustBeDateString]` — string must be a valid date format

## Optional fields
Use `Optional<T>` wrapper types for fields that may not be set:
```csharp
public OptionalString bonusEffect;     // Optional<string>
public OptionalInt bonusDamage;         // Optional<int>
```
Optional fields are omitted from serialization when not set, keeping content JSON clean.

## Unreal C++ content types
Subclass `UBeamContentObject` or any SDK subtype (`UBeamItemContent`, `UBeamGameTypeContent`, etc.). Each type must define a content type ID via a naming-convention function:
```cpp
UCLASS(BlueprintType)
class BEAMABLECORE_API UWeaponContent : public UBeamContentObject
{
    GENERATED_BODY()
public:
    UFUNCTION()
    void GetContentType_UWeaponContent(FString& Result){ Result = TEXT("weapons"); }

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 damage;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    float attackSpeed;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FString description;
};
```
- The function name **must** follow the pattern `GetContentType_<ClassName>`
- Annotate properties with `EditAnywhere` and `BlueprintReadOnly` (or `BlueprintReadWrite`)
- For built-in types (`UBeamCurrencyContent`, `UBeamItemContent`, etc.), C# equivalents already exist in the Microservice SDK
- For custom types used in microservices, you **must** declare matching C# equivalents

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


## Manifest operations

### Create a new manifest
```
beam_exec("content new-manifest <manifest-id> -q")
```
The default manifest is `global`. Create additional manifests to organize content into groups (e.g., `events`, `seasons`).

#### `beam content new-manifest` options
| Option | Type | Description |
|---|---|---|


### List manifests
```
beam_exec("content list-manifests -q")
```
Shows all available manifests for the current realm.

#### `beam content list-manifests` options
| Option | Type | Description |
|---|---|---|
| `--include-archived` | flag | Include content manifest ids that have been archive |


### Archive a manifest
```
beam_exec("content archive-manifest --manifest-id <id> -q")
```
Archives a manifest that is no longer needed. Archived manifests are not deleted but become inactive.

## Common pitfalls
- **Type names cannot contain dots.** The dot is reserved for the `{type}.{name}` content ID format.
- **Unreal custom types need C# equivalents** if microservices access them. Use the serialization mapping table above.
- **Place shared types correctly.** Unity types go in `Assets/Beamable/Common/`; standalone projects use a shared library.
- **Microservices need explicit imports.** When using `ContentRef<T>`, `ContentObject`, or validation attributes in a microservice, add `using Beamable.Common.Content;` and `using Beamable.Common.Content.Validation;`. These are not auto-imported.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.

## Wrap-up
After creating content types, remind the user to: create content instances in `.beamable/content/<realm-id>/global/`, run `content status` to verify, and `content publish` when ready to go live.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
