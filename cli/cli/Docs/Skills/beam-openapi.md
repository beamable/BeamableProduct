---
name: beam-openapi
description: Download and use Beamable OpenAPI specs for platform and microservice APIs
---

# OpenAPI Specs

## Overview

Beamable exposes OpenAPI specifications for both the platform API and every microservice. Use these specs to understand available endpoints, generate clients, or integrate with external tools.

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- Authenticated with `beam login` or saved credentials

## Steps

### 1. Download the platform OpenAPI spec
```
beam_exec("oapi download --output beam-oapi.json -q")
```
This downloads the full Beamable platform API spec as a single JSON file.

#### `beam oapi download` options
| Option | Type | Description |
|---|---|---|
| `--output` | string | When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path |
| `--filter` | string | Filter which open apis to generate. An empty string matches everything |
| `--combine-into-one-document` | flag | Combines all API documents into one. In order to achieve that it will need to rename some of the types because of duplicates, eg. GetManifestResponse |


### 2. Filter to specific API groups
You can filter the spec to only include endpoints matching a keyword:
```
beam_exec("oapi download --filter inventory --output inventory-oapi.json -q")
beam_exec("oapi download --filter content --output content-oapi.json -q")
beam_exec("oapi download --filter stats --output stats-oapi.json -q")
```
This is useful when you only need a subset of the platform API.

### 3. Combine all specs into one document
```
beam_exec("oapi download --combine-into-one-document --output combined-oapi.json -q")
```
This merges all microservice specs and the platform spec into a single OpenAPI document.

### 4. Open Swagger UI for a microservice
```
beam_exec("project open-swagger --service MyService -q")
```
This opens an interactive Swagger UI in the browser for a running microservice. The service must be running locally via `project run` first.

#### `beam project open-swagger` options
| Option | Type | Description |
|---|---|---|
| `--routing-key` | string | The routing key for the service instance we want. If not passed, defaults to the local service |
| `--remote` | flag | When set, enforces the routing key to be the one for the service deployed to the realm. Cannot be specified when --routing-key is also set |
| `--src-tool` | string | A hint to the Portal page which tool is being used |


## Microservice OpenAPI Specs

Each microservice automatically generates its own OpenAPI spec during build. The spec file is located at:
```
services/<ServiceName>/bin/Debug/net8.0/beam_openApi.json
```
This file is regenerated each time the service is built (`beam project build`). It describes all `[ClientCallable]`, `[AdminOnlyCallable]`, `[ServerCallable]`, and `[Callable]` endpoints, including their request/response schemas.

## Using Specs to Understand Endpoints

The downloaded OpenAPI spec contains:
- **Paths**: Every callable endpoint with HTTP method, URL, parameters, and request/response schemas
- **Components/Schemas**: All data transfer objects (DTOs) used by the API
- **Security**: Authentication requirements for each endpoint

### Workflow for exploring the API
1. Download the spec: `beam_exec("oapi download --output beam-oapi.json -q")`
2. Read the JSON file to discover available endpoints
3. Filter by keyword if you only need a specific API area
4. Use the schema definitions to understand request/response formats

## Common Pitfalls
- **Microservice must be built first.** The `beam_openApi.json` file is generated during `project build`. If the file is missing, run `beam_exec("project build --ids <ServiceName> -q")`.
- **Swagger UI requires a running service.** `project open-swagger` opens a browser to the local service — the service must be running via `project run` first.
- **`--filter` is a substring match.** It matches against endpoint paths, so `--filter inventory` returns all paths containing "inventory".
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was downloaded**: Which spec was downloaded — full platform API, filtered subset, or microservice-specific spec. Note the output file path.
2. **Where the files live**:
   - Downloaded spec: The path specified via `--output` (or the default output location).
   - Microservice specs: `services/<ServiceName>/bin/Debug/net8.0/beam_openApi.json` — auto-generated during build.
3. **Why specific choices were made** — explain the reasoning:
   - **Full vs filtered spec**: The full spec contains all platform endpoints. Filtering is useful when you only need a specific API area (e.g., inventory, content, stats) to reduce noise.
   - **Combined document**: Merging all specs into one document is useful for tools that consume a single OpenAPI file, or for getting a complete view of all available endpoints across the platform and microservices.
4. **How to use the spec**: The OpenAPI JSON can be imported into tools like Postman, used to generate typed clients, or read directly to understand endpoint signatures and data models.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
