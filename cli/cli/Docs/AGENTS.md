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

## MCP Integration

This project has a Beamable MCP server. If `.mcp.json` exists, your AI tool can use it directly. Otherwise, run:
```
beam mcp setup
```

### Available MCP tools
- `beam_list_commands(prefix)` — discover CLI commands
- `beam_get_help(command)` — full command documentation
- `beam_exec(command)` — execute a command (always pass `-q`)
- `beam_get_skill(skill)` — step-by-step workflow guides

### Workflow Skills
Load a skill before attempting complex tasks:
- `beam-initialize-project` — set up a new workspace
- `beam-create-microservice` — create services with storage/federation
- `beam-manage-content` — sync, publish, create content types
- `beam-build-and-deploy` — Docker build and cloud deployment
- `beam-create-portal-extension` — portal UI extensions
- `beam-login-auth` — authentication flows

## Project Structure

- `.beamable/` — workspace config (CID, PID, host, auth tokens)
- `.beamable/content/` — content JSON files organized by manifest
- `.config/dotnet-tools.json` — pins the `beam` CLI version

## Important

- Always pass `-q` (quiet mode) when executing beam commands from an AI agent
- Run `beam config` to check current CID, PID, and auth state
- Content publish is irreversible — always check `content status` first
- `beam deploy release` with `--replace` (default) removes services not in the plan
