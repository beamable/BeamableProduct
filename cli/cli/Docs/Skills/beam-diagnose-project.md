---
name: beam-diagnose-project
description: Diagnose common Beamable local development issues — workspace config, auth, dependencies, Docker, and build problems
---

# Diagnose Project

## When to Use This Skill
- A beam command fails with an unclear error
- `project run` or `project build` fails unexpectedly
- The user reports "it worked before" or "it works on another machine"
- Before attempting complex workflows, to verify the environment is healthy

## Diagnostic Checklist

Run these checks IN ORDER. Stop at the first failure and fix it before continuing.

### 1. Workspace exists
```
beam_exec("config -q")
```
**Expected:** Returns CID, PID, host, and CLI version.
**If fails:** No `.beamable` folder. Run `beam init` (see `beam-initialize-project` skill).

### 2. CLI version matches
Check `.config/dotnet-tools.json` for the pinned version, then compare to the running CLI:
```
beam_exec("version -q")
```
**If mismatch:** Run `dotnet tool restore` in the project directory to sync.

### 3. Authentication is valid
```
beam_exec("me -q")
```
**Expected:** Returns the current user's email and account ID.
**If fails:** Token expired or missing. Run `beam login` (see `beam-login-auth` skill).

### 4. Services are registered
```
beam_exec("project ps -q")
```
**Expected:** Lists all services with their status (running/stopped/not-built).
**If empty:** No services created yet. Use `beam-create-microservice` skill.
**If shows services but paths are wrong:** The `.beamable/beamoLocalManifest.json` may be stale. Re-run `project ps` after fixing paths.

### 5. Projects compile
```
beam_exec("project build -q")
```
**Expected:** Clean build with 0 errors. Check stderr for Beamable Analyzer warnings.
**Common failures:**
- Missing `[System.Serializable]` on DTOs — add the attribute
- Missing package references — check `.csproj` has correct `Beamable.Microservice.Runtime` version
- `IncludeRoutes<T>` type mismatch — ensure `T` matches the `Microservice` subclass name in `Program.cs`

### 6. Docker is running (for storage or deployment)
Only needed if the project uses storage or needs cloud deployment:
```
beam_exec("project ps -q")
```
Check if storage services show "Docker not running" status.
**Fix:** Start Docker Desktop or the Docker daemon.

### 7. Network connectivity
```
beam_exec("config -q")
```
Check the `host` value, then verify the API is reachable. If commands hang or timeout, the Beamable API endpoint may be unreachable from this network.

## Common Root Causes

| Symptom | Likely Cause | Fix |
|---|---|---|
| "No .beamable directory found" | Missing workspace | `beam init --cid <CID> --pid <PID> -q` |
| "401 Unauthorized" on any command | Expired token | `beam login -q` |
| Build error: missing package | Wrong SDK version | Check `.csproj` PackageReference versions match `.config/dotnet-tools.json` |
| Service starts but all calls return 404 | `IncludeRoutes<T>` type mismatch | Fix `Program.cs` — `T` must be the Microservice subclass |
| Serialization returns empty objects | Missing `[System.Serializable]` or using properties | Add attribute, use public fields |
| `project run` hangs at 0% | Build failure or locked files | Run `project build` first to see errors; check if another instance is running |
| Storage connection refused | Docker not running or container stopped | Start Docker, then `project run` to restart storage |

## Wrap-Up
After diagnosing, summarize:
1. **What was checked** and what passed/failed
2. **Root cause** of the failure
3. **What was fixed** and how to verify it worked
4. **Prevention** — what the user can do to avoid this in the future

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
