---
name: beam-content-history
description: Content version management — snapshots, history, restore from backups
---

# Content History and Snapshots

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- Authenticated with `beam login` or saved credentials

## Snapshots

Snapshots are point-in-time backups of your content. Use them to restore content if a bad publish occurs.

### Create a snapshot
```
beam_exec("content snapshot -q")
```
Saves the current local content state as a named snapshot.

#### `beam content snapshot` options
| Option | Type | Description |
|---|---|---|
| `--manifest-id` | string | Defines the name of the manifest that the snapshot will be created from. The default value is `global` |
| `--name` | string | Defines the name for the snapshot to be created |
| `--snapshot-type` | ContentSnapshotType | Defines where the snapshot will be stored to.
Local => Will save the snapshot under `.beamable/temp/content-snapshots/[PID]` folder
Shared => Will save the snapshot under `.beamable/content-snapshots/[PID]` folder |


### List snapshots
```
beam_exec("content snapshot-list -q")
```
Lists all available snapshots with their timestamps and diff status against the current content.

#### `beam content snapshot-list` options
| Option | Type | Description |
|---|---|---|
| `--manifest-id` | string | Defines the name of the manifest that will be used to compare the changes between the manifest and the snapshot. The default value is `global` |
| `--pid` | string | An optional field to set the PID from where you would like to get the snapshot to list. The default will get for all the realms |


### Restore from a snapshot
```
beam_exec("content restore --snapshot-id <id> -q")
```
Restores content from a previously saved snapshot. This can be additive (merge new items) or a full replacement depending on flags.

#### `beam content restore` options
| Option | Type | Description |
|---|---|---|
| `--manifest-id` | string | Defines the name of the manifest on which the snapshot will be restored. The default value is `global` |
| `--name` | string | Defines the name or path for the snapshot to be restored. If passed a name, it will first get the snapshot from shared folder '.beamable/content-snapshots/[PID]' than from the local only under '.beamable/temp/content-snapshots/[PID]'. If a path is passed, it is going to try get the json file from the path |
| `--pid` | string | An optional field to set the PID from where you would like to get the snapshot to be restored. The default will be the current PID the user are in |
| `--delete-after-restore` | flag | Defines if the snapshot file should be deleted after restoring |
| `--additive-restore` | flag | Defines if the restore will additionally adds the contents without deleting current local contents |


### Auto-snapshot on publish
When publishing, you can automatically create a snapshot for rollback safety:
```
beam_exec("content publish --manifest-ids global --auto-snapshot-type LocalOnly -q")
```
Snapshot types:
- `LocalOnly` — saved under `.beamable/temp/content-snapshots/[PID]`
- `SharedOnly` — saved under `.beamable/content-snapshots/[PID]` (version-controlled)
- `Both` — saves to both local and shared folders
- `None` — no snapshot (default)

Use `--max-local-snapshots` to limit how many local snapshots are kept. Older snapshots are automatically deleted when the limit is hit.

## Publish history

The history commands let you browse and restore from previously published content versions.

### View publish history
```
beam_exec("content history -q")
```
Returns a timeline of all publishes for the realm, including timestamps and who published.

#### `beam content history` options
| Option | Type | Description |
|---|---|---|
| `--watch` | flag | When true, the command will run forever and watch the state of the program |
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--require-process-id` | int | Listens to the given process id. Terminates this long-running command when the it no longer is running |
| `--from-date` | Nullable`1 | Filter entries from this Unix timestamp (milliseconds) |
| `--to-date` | Nullable`1 | Filter entries to this Unix timestamp (milliseconds) |
| `--manifest-uids` | Set[string] | Filter by specific manifest UIDs |


### Get diff for a specific publish
```
beam_exec("content history sync-changelist --version <version-id> -q")
```
Returns what changed in a specific publish — which content items were added, modified, or removed.

#### `beam content history sync-changelist` options
| Option | Type | Description |
|---|---|---|
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--manifest-uid` | string | The manifest UID for the changelist to sync |


### Download content for a specific version
```
beam_exec("content history sync-content --version <version-id> -q")
```
Downloads the full content payload for a historical version to local files.

#### `beam content history sync-content` options
| Option | Type | Description |
|---|---|---|
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--manifest-uid` | string | The manifest UID for the content to sync |
| `--content-ids` | Set[string] | The content IDs to sync. If not provided, syncs all content in the manifest |


### Restore from historical version
```
beam_exec("content history restore-content --version <version-id> -q")
```
Restores content from a historical publish version. After restoring, run `content publish` to make the restored content live.

#### `beam content history restore-content` options
| Option | Type | Description |
|---|---|---|
| `--manifest-ids` | String[] | Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest |
| `--manifest-uid` | string | The manifest UID from history to restore content from |
| `--content-ids` | Set[string] | The content IDs to restore. If not provided, restores all content in the manifest |


## Typical recovery workflow
1. Run `content history` to find the version before the bad publish
2. Run `content history sync-changelist --version <id>` to confirm what changed
3. Run `content history restore-content --version <id>` to restore locally
4. Run `content status` to verify the restored state
5. Run `content publish --manifest-ids global -q` to push the restored content live

## Common pitfalls
- **Restoring only updates local files.** You must `content publish` afterwards to make changes live.
- **Snapshots are separate from publish history.** Snapshots are local backups you create; history is the server-side publish log.
- **Auto-snapshot with `LocalOnly` is not version-controlled.** Use `SharedOnly` or `Both` if the team needs access to snapshots.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.

## Wrap-up
After completing a restore or history operation, summarize what version was restored, how many items changed, and remind the user to run `content publish` if they want changes to go live.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
