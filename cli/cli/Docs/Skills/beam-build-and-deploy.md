---
name: beam-build-and-deploy
description: Build Docker images and deploy services to a Beamable realm.
---

# Build and Deploy

## Prerequisites
- A `.beamable` workspace with services defined
- Docker installed and running
- Authenticated with `beam login` or saved credentials

## Steps

### 1. Build locally to check for compile errors
```
beam_exec("project build --ids <service1> <service2>")
```
Omit `--ids` to build all services.

#### `beam project build` options
| Option | Type | Description |
|---|---|---|
`--watch` | flag | When true, the command will run forever and watch the state of the program
`--ids` | Set[string] | The list of services to include, defaults to all local services (separated by whitespace). To use NO services, use the --exact-ids flag
`--exact-ids` | flag | By default, a blank --ids option maps to ALL available ids. When the --exact-ids flag is given, a blank --ids option maps to NO ids
`--stop-reason` | string | A message to send to the running service when it is terminated
`--max-parallel-count` | int | Maximum number of parallel services builds


### 2. Plan the deployment (builds Docker images)
```
beam_exec("deploy plan -q")
```
This builds Docker images for all services and generates a deployment plan.

#### `beam deploy plan` options
| Option | Type | Description |
|---|---|---|
`--comment` | string | Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal
`--service-comments` | String[] | Any number of strings in the format BeamoId::Comment
Associates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal
`--from-manifest` | string | A manifest json file to use to create a plan
`--from-manifest-id` | string | A manifest id to download and use to create a plan
`--run-health-checks` | flag | Run health checks on services
`--restart` | flag | Restart existing deployed services
`--build-sequentially` | flag | Build services sequentially instead of all together
`--max-parallel-count` | int | Maximum number of parallel services builds
`--merge` | flag | Create a Release that merges your current local environment to the existing remote services. Existing deployed services will not be removed
`--replace` | flag | Create a Release that completely overrides the existing remote services. Existing deployed services that are not present locally will be removed (default)
`--docker-compose-dir` | string | Specify an output path where a new docker-compose project will be created. The compose file can be used to run services locally. (Note, existing files in this folder will be overwritten)
`--sln` | string | Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used
`--to-file` | string | A file path to save the plan


### 3. Release the deployment
```
beam_exec("deploy release -q")
```
This pushes the most recent plan to the realm. Alternatively, use `--from-latest-plan` to explicitly use the last generated plan:
```
beam_exec("deploy release --from-latest-plan -q")
```

You can also plan and release in one step:
```
beam_exec("deploy release --comment \"Initial deployment\" -q")
```

#### `beam deploy release` options
| Option | Type | Description |
|---|---|---|
`--comment` | string | Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal
`--service-comments` | String[] | Any number of strings in the format BeamoId::Comment
Associates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal
`--from-manifest` | string | A manifest json file to use to create a plan
`--from-manifest-id` | string | A manifest id to download and use to create a plan
`--run-health-checks` | flag | Run health checks on services
`--restart` | flag | Restart existing deployed services
`--build-sequentially` | flag | Build services sequentially instead of all together
`--max-parallel-count` | int | Maximum number of parallel services builds
`--merge` | flag | Create a Release that merges your current local environment to the existing remote services. Existing deployed services will not be removed
`--replace` | flag | Create a Release that completely overrides the existing remote services. Existing deployed services that are not present locally will be removed (default)
`--from-plan` | string | The file path to a pre-generated plan file using the `deploy plan` command
`--sln` | string | Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used
`--from-latest-plan` | flag | Use the most recent plan generated from the plan command


### 4. Verify the deployment
```
beam_exec("deploy list")
```

## Filtering Services

Target specific services by ID or group tags:
```
beam_exec("deploy plan --ids MyService1 MyService2 -q")
beam_exec("deploy plan --with-group backend -q")
beam_exec("deploy plan --without-group experimental -q")
```

## Common Pitfalls
- **`services build`, `services deploy`, `services run`, `services stop` are ALL REMOVED.** They will throw an error. Use `project run`/`project stop`/`project build` for local development and `deploy plan`/`deploy release` for cloud deployment.
- **Docker must be running** before `deploy plan`. The command builds Docker images using `docker buildx`.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.
- **`--replace` is the default mode** for `deploy plan`. This means services NOT included in the plan will be removed from the realm. Use `--merge` to add services without removing existing ones.
- **Build errors appear in stderr.** If `deploy plan` fails, check the error output for .NET compilation errors.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was deployed**: List each service by name and whether it was included in the deployment plan. Note if any services were skipped or filtered out via `--ids`/`--with-group`/`--without-group`.
2. **Where the artifacts live**:
   - Docker images: Built locally and tagged during `deploy plan`. The images are pushed to Beamable's container registry as part of the release.
   - Deployment plan: Stored in `.beamable/temp/` — contains the image references and service configuration that `deploy release` uses.
   - Service manifest: `.beamable/beamoLocalManifest.json` — the local definition of all services, their Docker settings, and dependency graph.
3. **Why specific choices were made** — explain the reasoning:
   - **Replace vs merge mode**: Replace (default) ensures the realm matches exactly what was deployed — services not in the plan are removed. Merge adds or updates services without touching others. Explain which was used and why (e.g., clean deployment vs incremental update).
   - **Service filtering**: If `--ids` or group tags were used, explain why only those services were deployed (e.g., the others were unchanged, or a staged rollout was needed).
   - **Plan then release (two-step)**: The two-step workflow lets the user inspect the plan before committing. A single-step `deploy release` (without a prior plan) builds and releases in one shot — faster but with no review step.
4. **How to verify**: The user can run `deploy list` to see the current deployment state, or check individual service health via the Beamable Portal. If something went wrong, `deploy plan` output contains build logs and image details.
