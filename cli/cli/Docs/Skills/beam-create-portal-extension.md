---
name: beam-create-portal-extension
description: Create a portal extension (full-page or component) mounted on the Beamable Portal.
---

# Create Portal Extension

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- Node.js and npm must be available on the system, it uses node version 22 or greater.

## Steps

### 1. Discover mount points (REQUIRED)
You MUST call this before creating the extension. It returns the valid `--mount-page` and `--mount-selector` values from the remote portal config.
```
beam_exec("portal extension list-extension-options")
```

The response has two sections:
- **pageExtensions**: Full-page extensions that add a new page to the Portal navigation. Use `routePrefix` + your custom route as `--mount-page`. The `--mount-selector` is auto-assigned from `autoSelector`.
- **componentExtensions**: Component slots that inject into an existing Portal page. Use `path` as `--mount-page` and pick one of the `selectors` as `--mount-selector`.

### 2. Create the extension

For a **page extension** (new navigation page):
```
beam_exec("project new portal-extension <Name> --mount-page \"<routePrefix><your-route>\" --mount-group \"<NavGroup>\" --mount-label \"<NavLabel>\" -q")
```
- `--mount-group` and `--mount-label` are required for sidebar navigation
- `--mount-selector` is auto-assigned; do not provide it

For a **component extension** (slot on an existing page):
```
beam_exec("project new portal-extension <Name> --mount-page \"<path>\" --mount-selector \"<selector>\" -q")
```
- `--mount-selector` is REQUIRED and must match one of the selectors from step 1
- Do NOT provide `--mount-group` or `--mount-label`

#### `beam project new portal-extension` options
| Option | Type | Description |
|---|---|---|
`--sln` | string | Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used
`--service-directory` | string | Relative path to directory where project should be created. Defaults to "SOLUTION_DIR/services"
`--mount-page` | string | The portal page to mount on. For page extensions use routePrefix + your custom route; for component extensions use the page path. Run 'portal extension list-extension-options' to see all valid values
`--mount-selector` | string | The mount slot on the page. Required for component extensions; omit for page extensions (auto-assigned). Run 'portal extension list-extension-options' to see valid selectors per page
`--mount-group` | string | Specify the navigation group of the extension. This is only valid when the extension is a full page
`--mount-label` | string | Specify the navigation label of the extension. This is only valid when the extension is a full page
`--mount-icon` | string | Specify the Material Design Icon (mdi) that will be used for the extension's navigation. This is only valid when the extension is a full page
`--mount-group-order` | int | Specify the order of the mount group
`--mount-label-order` | int | Specify the order of the mount label


### 3. Run the extension locally
```
beam_exec("project run --ids <Name>")
```

### 4. Add microservice dependency (if needed)
If the portal extension needs to call a microservice, add it as a dependency. The microservice must already exist (created via `project new service`).
```
beam_exec("portal extension add-microservice <Name> <MicroserviceName>")
```
This generates typed client code in the `beamable/clients/` directory inside the extension project, giving you type-safe access to the microservice's `[ClientCallable]` endpoints from TypeScript.

#### `beam portal extension add-microservice` options
| Option | Type | Description |
|---|---|---|


### 5. Open in browser to verify
```
beam_exec("portal open-extension <Name>")
```

## Web SDK Documentation
For TypeScript development in the extension, call:
```
beam_list_types("web")
```
This returns the documentation URL pattern for the Beamable Web SDK (`@beamable/sdk`). The SDK is a transitive dependency of `@beamable/portal-toolkit` — the toolkit version is NOT the SDK version. Follow the `versionResolution` steps in the response to find the actual SDK version from `node_modules/@beamable/portal-toolkit/package.json` → `peerDependencies["@beamable/sdk"]`.

## Common Pitfalls
- **Never guess mount values.** Valid pages and selectors come from a remote config that changes between Portal versions. Always call `list-extension-options` first.
- **Always pass `-q` (quiet mode)** when executing from MCP to avoid interactive prompts that will hang.
- **Page extensions need `--mount-group` and `--mount-label`** for the sidebar entry. Component extensions do not use these.
- **Mount page format differs by type**: page extensions use `routePrefix + custom-route`, component extensions use the exact `path` value.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was created**: The portal extension name, and whether it is a page extension (new navigation entry) or a component extension (injected into an existing page).
2. **Where the files live**:
   - Extension project root: `services/<Name>/` — contains the React/TypeScript source.
   - Entry point: `services/<Name>/src/index.tsx` — the main component rendered by the Portal.
   - Package config: `services/<Name>/package.json` — dependencies including `@beamable/portal-toolkit`.
   - If a microservice dependency was added: `services/<Name>/beamable/clients/` — auto-generated typed TypeScript clients for calling microservice endpoints.
   - Service manifest: `.beamable/beamoLocalManifest.json` — tracks the extension alongside other services.
3. **Why specific choices were made** — explain the reasoning:
   - **Mount point chosen**: Why this `--mount-page` and `--mount-selector` — what Portal page it extends and where in the UI it appears. For page extensions, explain what the nav group and label mean for Portal navigation.
   - **Page vs component**: Why this extension type was chosen — page extensions are standalone features with their own route, while component extensions augment existing Portal pages at specific insertion points.
   - **Microservice dependency**: If added, explain that `portal extension add-microservice` generated typed clients so the extension can call `[ClientCallable]` endpoints with full type safety, and that the microservice must be running (locally or deployed) for those calls to work.
4. **How to iterate**: Remind the user they can run `project run --ids <Name>` to start the dev server, open it with `portal open-extension <Name>`, and that changes to the TypeScript source hot-reload in the browser. For the Web SDK API reference, call `beam_list_types("web")` to get the documentation URL.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
