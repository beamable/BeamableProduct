---
name: beam-remove-service
description: Remove a Beamable microservice or storage from the local project and clean up references.
---

# Remove Service

## When to Use This Skill
- The user wants to delete a microservice or storage from their project
- A service was created by mistake or is no longer needed
- Cleanup after experimentation or a failed `project new service` attempt

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- The service must be registered in the local manifest (`project ps -q` shows it)

## Steps

### 1. List existing services
```
beam_exec("project ps -q")
```
Confirm the exact service name with the user before removing.

### 2. Remove the service
```
beam_exec("project remove --ids <ServiceName> -q")
```
This command:
- Deletes the service's project directory recursively (the entire `services/<ServiceName>/` folder)
- Removes the project reference from the `.sln` file

To remove multiple services at once:
```
beam_exec("project remove --ids ServiceA ServiceB -q")
```

### 3. Verify removal
```
beam_exec("project ps -q")
```
Confirm the service no longer appears in the list.

## `beam project remove` options
| Option | Type | Description |
|---|---|---|
`--ids` | Set[string] | The list of services to remove (separated by whitespace)
`--with-group` | Set[string] | Remove services matching these group tags
`--without-group` | Set[string] | Exclude services matching these group tags from removal
`--sln` | string | Path to the .sln file

## Common Pitfalls
- **Removal is permanent and immediate.** The command deletes the service directory recursively with no confirmation prompt. There is no undo — make sure the user has committed or backed up their code first.
- **Remote deployment is not affected.** `project remove` only removes local source code. If the service was previously deployed, it will still be running in the cloud. Use `deploy plan` and `deploy release` after removal to update the remote state.
- **Storage removal does not remove linked references.** If you remove a storage that a service depends on, the service will have a broken project reference. Remove the dependency first with `project deps remove <Service> <Storage>`, or remove both the service and storage together.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was removed**: The service name and its former project directory.
2. **What remains**: Any linked storages or dependent services that still reference the removed project.
3. **Remote state**: Whether the service was previously deployed and needs a `deploy release` to clean up the remote environment.
4. **Version control**: Remind the user to commit the deletion so teammates pick up the change.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
