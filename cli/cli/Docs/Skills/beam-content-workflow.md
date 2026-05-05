---
name: beam-content-workflow
description: Day-to-day content workflow — status, sync, publish, resolve conflicts, tags
---

# Content Workflow

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- Authenticated with `beam login` or saved credentials

## Workflow: status -> sync -> resolve -> publish

### 1. Check content status
```
beam_exec("content status -q")
```
Shows the state of each content object: Created (local only), Modified, Deleted, Conflicted, or UpToDate. Add `--show-up-to-date` to include unchanged items.

#### `beam content status` options
| Option | Type | Description |
|---|---|---|
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--show-up-to-date` | flag | Show up to date content |
| `--limit` | int | Limit content displayed amount (default: 100) |
| `--skip` | int | Skips content amount |


### 2. Sync local content with remote
```
beam_exec("content sync --manifest-ids global -q")
```
Without flags, sync does nothing. You must specify what to sync.

**WARNING**: Flags like `--sync-modified` and `--sync-conflicts` are DESTRUCTIVE and IRREVERSIBLE — they discard local changes in favor of the remote version. Always run `content status` first.

#### `beam content sync` options
| Option | Type | Description |
|---|---|---|
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--filter-type` | ContentFilterType | Defines the semantics for the `filter` argument. When no filters are given, affects all existing content.
ExactIds => Will only add the given tags to the ','-separated list of filters
Regexes => Will add the given tags to any content whose Id is matched by any of the ','-separated list of filters (C# regex string)
TypeHierarchy => Will add the given tags to any content of the ','-separated list of filters (content type strings with full hierarchy --- StartsWith comparison)
Tags => Will add the given tags to any content that currently has any of the ','-separated list of filters (tags) |
| `--filter` | string | Accepts different strings to filter which content files will be affected. See the `filter-type` option |
| `--sync-created` | flag | Deletes any created content that is not present in the latest manifest. If filters are provided, will only delete the created content that matches the filter |
| `--sync-modified` | flag | This will discard your local changes ONLY on files that are NOT conflicted. If filters are provided, will only do this for content that matches the filter |
| `--sync-conflicts` | flag | This will discard your local changes ONLY on files that ARE conflicted. If filters are provided, will only do this for content that matches the filter |
| `--sync-deleted` | flag | This will revert all your deleted files. If filters are provided, will only do this for content that matches the filter |
| `--target` | string | If you pass in a Manifest's UID, we'll sync with that as the target. If filters are provided, will only do this for content that matches the filter |


### 3. Resolve conflicts
When `content status` shows Conflicted items, resolve them explicitly:
```
beam_exec("content resolve --use local -q")
```
Or accept the realm version:
```
beam_exec("content resolve --use realm -q")
```

#### `beam content resolve` options
| Option | Type | Description |
|---|---|---|
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--filter-type` | ContentFilterType | Defines the semantics for the `filter` argument. When no filters are given, affects all existing content.
ExactIds => Will only add the given tags to the ','-separated list of filters
Regexes => Will add the given tags to any content whose Id is matched by any of the ','-separated list of filters (C# regex string)
TypeHierarchy => Will add the given tags to any content of the ','-separated list of filters (content type strings with full hierarchy --- StartsWith comparison)
Tags => Will add the given tags to any content that currently has any of the ','-separated list of filters (tags) |
| `--filter` | string | Accepts different strings to filter which content files will be affected. See the `filter-type` option |
| `--use` | string | Whether to use the 'local' or 'realm' version of the conflicted content.
This applies to ALL matching elements of the filter that are conflicted.
Value must be "local" or "realm" |


### 4. Publish content to the realm
```
beam_exec("content publish --manifest-ids global -q")
```
This pushes all local content changes to the remote realm. **WARNING**: Publishing is irreversible for other clients — they will immediately receive the new content.

#### `beam content publish` options
| Option | Type | Description |
|---|---|---|
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--auto-snapshot-type` | AutoSnapshotType | Defines if after publish the Content System should take snapshots of the content.
None => Will not save any snapshot after publishing
LocalOnly => Will save the snapshot under `.beamable/temp/content-snapshots/[PID]` folder
SharedOnly => Will save the snapshot under `.beamable/content-snapshots/[PID]` folder
Both => Will save two snapshots, under local and shared folders |
| `--max-local-snapshots` | int | Defines the max stored local snapshots taken by the auto snapshot generation by this command. When the number hits, the older one will be deletd and replaced by the new snapshot |


## Copy content between realms
Use `content replace-local` to download content from a different realm and overwrite local files:
```
beam_exec("content replace-local --source-pid <other-realm-pid> -q")
```
This is useful for promoting content from staging to production.

## Tag operations
Tags let you label content objects for filtering. Use these commands to manage tags:
```
beam_exec("content tag add --tag beta --filter weapons.sword_01 -q")
beam_exec("content tag set --tag release --filter weapons.sword_01 -q")
beam_exec("content tag remove --tag beta --filter weapons.sword_01 -q")
```
- `tag add` — appends a tag without removing existing tags
- `tag set` — replaces all tags on matched content with the specified tag
- `tag remove` — removes a specific tag from matched content

Filter content by tag in other commands:
```
beam_exec("content status --filter-type Tags --filter beta -q")
beam_exec("content publish --filter-type Tags --filter release --manifest-ids global -q")
```

## Filtering content
Filter by manifest, type, IDs, or tags:
```
beam_exec("content status --manifest-ids global,events -q")
beam_exec("content sync --filter-type TypeHierarchy --filter weapons --manifest-ids global -q")
beam_exec("content publish --manifest-ids events -q")
```

## Cross-SDK content access

### Microservices (C#)

**Get a single content object by ID:**
```csharp
[ClientCallable]
public async Task<int> GetWeaponDamage(string contentId)
{
    var weapon = await Services.Content.GetContent<WeaponContent>(
        new ContentRef<WeaponContent>(contentId)
    );
    return weapon.damage;
}
```

**Get ALL content of a specific type** (there is no `GetAll<T>()`):
```csharp
[ClientCallable]
public async Task<List<string>> GetAllWeaponIds()
{
    // Step 1: Get manifest filtered by type prefix
    var manifest = await Services.Content.GetManifest("t:weapons");

    // Step 2: Resolve each entry individually
    var weapons = new List<WeaponContent>();
    foreach (var entry in manifest.entries)
    {
        var weapon = await Services.Content.GetContent<WeaponContent>(
            new ContentRef<WeaponContent>(entry.contentId)
        );
        weapons.Add(weapon);
    }
    return weapons.Select(w => w.Id).ToList();
}
```

**GetManifest filter syntax:**
- `"t:weapons"` — all content of type "weapons"
- `"t:weapons items"` — all weapons OR items
- `"tag:rare"` — all content tagged "rare"
- `""` — entire manifest (all types)

You can also use `EnableEagerContentLoading` on the `[Microservice]` attribute to preload all content at startup.

### Unity
```csharp
var weapon = await beamContext.Content.GetContent<WeaponContent>("weapons.sword_01");
```
- **ContentRef<T>** — lazy nullable reference, resolved on access
- **ContentLink<T>** — strict reference that must resolve on first frame; use for critical startup content

### Unreal
Use the `UBeamContentApi` subsystem to fetch and cache content at runtime.

### WebSDK
Use the `@beamable/sdk` npm package:
```typescript
const content = await sdk.content.get("weapons.sword_01");
```

## Content file format

Content JSON files in `.beamable/content/` must follow this exact structure:
```json
{
  "tags": [],
  "referenceManifestId": "EmptyManifest",
  "properties": {
    "fieldName": { "data": "<json_serialized_value>" }
  }
}
```

Critical serialization rules:
- Every property value is wrapped in `{"data": "..."}` — the `data` field contains the JSON-serialized value as a **string**
- Strings are double-escaped: `"data": "\"Iron Sword\""`
- Numbers are stringified: `"data": "25"` or `"data": "1.5"`
- Booleans: `"data": "true"`
- Lists/objects: `"data": "[{\"contentId\":\"weapons.iron_sword\",\"weight\":30.0}]"`
- `referenceManifestId` must be `"EmptyManifest"`, not an empty string — an empty string causes a NullReferenceException in the SDK

**WARNING**: Do not use `content save --force` — it can produce malformed files with an empty `referenceManifestId`.

## Common pitfalls
- **Sync without flags does nothing.** You must explicitly specify `--sync-created`, `--sync-modified`, etc.
- **`--sync-modified` discards LOCAL changes**, not remote. This is the opposite of what many expect.
- **Always check `content status` before sync or publish** to understand what will change.
- **The default manifest is `global`**. If you have multiple manifests, specify them with `--manifest-ids`.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.

## Wrap-up
After completing the workflow, summarize: what changed, how many items were synced/published, and remind the user to run `content status` to verify.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
