---
name: beam-review
description: Review checklist for Beamable code correctness before completing a task
---

# Beamable Code Review Checklist

## Overview

Before finalizing any Beamable-related code change, run through this checklist to catch common issues. Each section includes what to check, why it matters, and how to verify.

## Prerequisites
- A `.beamable` workspace must exist
- The project must build successfully

## Review Steps

### 1. Build the project and check for warnings
```
beam_exec("project build -q")
```
Fix any Beamable Analyzer warnings. These catch common mistakes like incorrect attribute usage, missing serialization attributes, and invalid content type names.

### 2. Check content status
```
beam_exec("content status -q")
```
Review the output for unexpected changes before publishing. Look for:
- **Modified** items you did not intend to change
- **Deleted** items that should still exist
- **Conflicted** items that need manual resolution

### 3. Verify content types and references
Read the content type source files to confirm types are correctly registered. Use the `beam-get-source` skill to locate SDK source code if needed.

## Checklist

### Content References
- [ ] `ContentRef<T>` is used for references to content objects, not raw string IDs
- [ ] `ContentLink<T>` is used when the content should be resolved eagerly (embedded in parent)
- [ ] `IContentApi` is injected properly in microservices via `Services.Content`
- [ ] Content is resolved with `await Services.Content.GetContent<T>(contentRef)` — not constructed manually

### Ref Types
- [ ] `ContentRef<T>` for typed references to specific content types
- [ ] `AbsRef` for untyped content references (rare — prefer typed refs)
- [ ] `BaseRef` is only used as a base class, never instantiated directly

### Callable Attributes
Choose the right attribute for each microservice endpoint:

| Attribute | Who can call | When to use |
|---|---|---|
| `[Callable]` | Anyone (no auth) | Webhooks, health checks, public endpoints |
| `[ClientCallable]` | Authenticated players | Player-facing game features (most common) |
| `[AdminOnlyCallable]` | Admin tokens only | Back-office tools, moderation, migrations |
| `[ServerCallable]` | Other microservices only | Internal service-to-service communication |

- [ ] Correct attribute is used for the endpoint's intended audience
- [ ] `[ClientCallable]` endpoints do NOT blindly trust client input — microservices are authoritative
- [ ] `[ClientCallable]` endpoints validate and sanitize all parameters from the client
- [ ] `[ServerCallable]` is used for internal methods that should not be exposed to game clients

### Optional Fields
- [ ] `Optional<T>` wrapper types are used for nullable content fields (e.g., `OptionalString`, `OptionalInt`, `OptionalFloat`)
- [ ] Bare nullable types (`string?`, `int?`) are NOT used in content classes — they do not serialize correctly
- [ ] Optional fields are checked with `.HasValue` and accessed with `.Value` before use

### Federation Interfaces
- [ ] Federation interfaces use the correct generic type parameter: `IFederatedLogin<TIdentity>` where `TIdentity : IFederationId, new()`
- [ ] `[FederationId("name")]` attribute has a unique string identifier per external system
- [ ] Federation method return types match the interface contract exactly
- [ ] Each microservice implements at most one federation interface per identity type

### Content Type Naming
- [ ] `[ContentType("name")]` uses a short, lowercase name without dots (dots are reserved for ID format: `type.name`)
- [ ] Type name is unique across the project — no two content classes share the same `[ContentType]` value
- [ ] Type names use underscores for multi-word names (e.g., `magic_items`), not camelCase or PascalCase

### Serialization
- [ ] All fields in content classes and callable method parameters are serializable
- [ ] Callable methods without `CallableFlags.SkipGenerateClientFiles` have serializable arguments and return types
- [ ] Complex types used in callables are plain C# classes with public fields or properties — no interfaces or abstract types as parameters
- [ ] `[System.Serializable]` attribute is present on content classes (required for Unity serialization)

### Content Files
- [ ] All content files have `"referenceManifestId": "EmptyManifest"` (not an empty string — empty string causes NullReferenceException)
- [ ] All property values use `{"data": "..."}` wrapper format
- [ ] String values are properly double-escaped inside `data` (e.g., `"data": "\"Iron Sword\""`)
- [ ] List/object values are JSON-stringified inside `data` (e.g., `"data": "[{\"id\":\"x\"}]"`)

### Deploy Safety
- [ ] `deploy plan` uses `--merge` when adding services without removing existing ones
- [ ] `deploy plan` uses `--replace` (default) only when intentionally replacing the full service set
- [ ] `content publish` targets the correct manifest with `--manifest-ids`
- [ ] Destructive operations (`content sync --sync-modified`, `deploy plan --replace`) are intentional

### Unreal-Specific
- [ ] C++ content types (`UBeamContentObject` subclasses) have matching C# equivalents if accessed from microservices
- [ ] `GetContentType_<ClassName>` function name matches the class name exactly
- [ ] Properties use `EditAnywhere` and `BlueprintReadWrite` (or `BlueprintReadOnly`)

## Common Pitfalls
- **`[ClientCallable]` does not mean "safe".** Any authenticated player can call these endpoints. Never trust input — validate everything server-side.
- **`Optional<T>` is required, not optional.** Using bare nullable types in content classes will cause silent serialization failures.
- **Content type dots break IDs.** The format is `type.name`, so a `[ContentType("my.type")]` would create ambiguous IDs like `my.type.sword` — is the type `my` or `my.type`?
- **`--replace` removes services.** The default deploy mode removes any service NOT in the plan. Use `--merge` to avoid accidentally deleting running services.
- **Always pass `-q`** when executing beam commands from MCP to avoid interactive prompts.

## Wrap-Up

After completing the review, provide the user with a summary that covers:

1. **What was reviewed**: List each category checked and whether issues were found.
2. **Issues found**: For each issue, explain what was wrong, why it matters, and the specific fix applied.
3. **Build and content status**: Report the result of `project build` (any warnings or errors) and `content status` (any unexpected changes).
4. **Confidence level**: State whether the code is ready to deploy or if further changes are needed. If there are remaining concerns, list them explicitly.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
