---
name: beam-create-portal-extension
description: Create a portal extension (full-page or component) mounted on the Beamable Portal.
---

# Create Portal Extension

## Prerequisites
- A `.beamable` workspace must exist (run `beam init` first)
- Node.js and npm must be available on the system

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

### 3. Run the extension locally
```
beam_exec("project run --ids <Name>")
```

### 4. Open in browser to verify
```
beam_exec("portal --extension <Name>")
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
