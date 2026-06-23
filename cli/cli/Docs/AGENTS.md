# Beamable

This project uses the [Beamable](https://beamable.com) backend platform. The `beam` CLI manages realms, content, microservices, and deployments.

## Quick Reference

- **Check config**: `beam config`
- **Login**: `beam login --email <email> --password <password> -q`
- **Switch realm**: `beam init --cid <CID> --pid <PID> -q`
- **Content status**: `beam content status`
- **Publish content**: `beam content publish --manifest-ids global -q`
- **Build services**: `beam project build`
- **Deploy**: `beam deploy release -q`

## Installing the Beamable MCP Extension

There are two ways to connect an AI agent to Beamable. Choose based on your setup:

### Option A: Claude Desktop Extensions (recommended for Claude Desktop / Cowork)

Install the Beamable extension from the **Claude Desktop Extensions directory** — search for "Beamable" and install it directly. This is the simplest path for Claude Desktop and Cowork users.

Alternatively, download the `.mcpb` bundle for your platform from [GitHub Releases](https://github.com/beamable/BeamableProduct/releases):
- `beamable-win-x64.mcpb` — Windows (x64)
- `beamable-osx-arm64.mcpb` — macOS (Apple Silicon)
- `beamable-osx-x64.mcpb` — macOS (Intel)
- `beamable-linux-x64.mcpb` — Linux (x64)

Open the `.mcpb` file in Claude Desktop to install it as an extension. The extension bundles a self-contained `beam` CLI binary — no .NET SDK or global install required.

**After installing:** The extension needs a project directory to work with. Open or create a project in Cowork, then use `beam init` to create the `.beamable` workspace (see `beam-initialize-project` skill).

### Option B: Local MCP setup (recommended for Cursor, Windsurf, VS Code, and other MCP clients)

If you have the `beam` CLI installed (via `dotnet tool install`), run:
```
beam mcp setup
```
This enables the Beamable MCP server by writing a `.mcp.json` file, and offers to also generate this `AGENTS.md` guide in your project (or pass `--agents-file` to generate it non-interactively). MCP-compatible editors (Cursor, Windsurf, VS Code with MCP extension) auto-discover the `.mcp.json` file and connect to the Beamable MCP server.

**Prerequisites:** .NET 8+ SDK and `beam` CLI installed via `.config/dotnet-tools.json` (created by `beam init`).

### If tools are not connecting

If the AI agent cannot reach Beamable tools:
1. **Check if `.mcp.json` exists** in the project root — if not, run `beam mcp setup`
2. **Check if the mcpb extension is installed** — in Claude Desktop, go to Settings → Extensions
3. **Restart the AI client** after installing or updating the MCP configuration
4. **Verify the CLI works** — run `beam version` in a terminal to confirm the CLI is accessible

### Available MCP tools
- `beam_list_commands(prefix)` — discover CLI commands
- `beam_get_help(command)` — full command documentation
- `beam_exec(command)` — execute a command (always pass `-q`)
- `beam_get_skill(skill)` — step-by-step workflow guides
- `beam_get_source(platform, version, filePath, offset, limit)` — get SDK source paths and read file content directly

### Workflow Skills
Load a skill before attempting complex tasks:
- `beam-initialize-project` — set up a new workspace
- `beam-manage-service` — create, configure, and remove microservices with storage/federation
- `beam-diagnose-project` — troubleshoot common local dev issues
- `beam-build-and-deploy` — Docker build and cloud deployment
- `beam-create-portal-extension` — portal UI extensions
- `beam-login-auth` — authentication flows
- `beam-content-types` — create and manage content types
- `beam-content-workflow` — content sync and publish workflows
- `beam-content-history` — content change history
- `beam-openapi` — OpenAPI spec generation
- `beam-review` — code review checklist for Beamable projects
- `beam-get-source` — read Beamable SDK source code
- `beam-unity-examples` — Unity sample projects and getting started
- `beam-unreal-guide` — Unreal development guide: custom content, microservices, Blueprints, semantic types
- `beam-microservice-examples` — Progressive code examples for C# microservices (Stats, Storage, Inventory, Federation)
- `beam-web-guide` — Web SDK guide: installation, authentication, stats, leaderboards, content, announcements, accounts, and microservice clients

## Project Structure

- `.beamable/` — workspace config (CID, PID, host, auth tokens)
- `.beamable/content/` — content JSON files organized by manifest
- `.config/dotnet-tools.json` — pins the `beam` CLI version

## Important

- Always pass `-q` (quiet mode) when executing beam commands from an AI agent
- Run `beam config` to check current CID, PID, and auth state
- Content publish is irreversible — always check `content status` first
- `beam deploy release` with `--replace` (default) removes services not in the plan
