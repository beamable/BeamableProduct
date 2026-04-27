Initialize a Beamable project workspace with realm context and authentication.

## Key Concepts
- **CID** (Customer ID): Your Beamable organization identifier. Can be a numeric ID or an alias.
- **PID** (Project ID): A specific realm within your organization. Each game can have multiple realms (dev, staging, production).
- **`.beamable` folder**: The workspace root created by `beam init`. Contains config, content, and credentials. Required by most commands.

## Steps

### 1. Initialize the workspace
```
beam_exec("init --save-to-file -q")
```
If you know the CID and PID:
```
beam_exec("init --cid <CID> --pid <PID> --save-to-file -q")
```
- `--save-to-file` persists credentials so subsequent commands don't require re-authentication
- Without `--save-to-file`, credentials are ephemeral and lost after the command

### 2. Verify the configuration
```
beam_exec("config")
```
This shows the current CID, PID, host, and other workspace settings.

### 3. Login (if not done during init)
```
beam_exec("login --username <email> --password <password> --save-to-file -q")
```
Or with a refresh token:
```
beam_exec("login --refresh-token <token> --save-to-file -q")
```

## Switching Realms
To switch to a different realm within the same organization:
```
beam_exec("init --cid <CID> --pid <NewPID> --save-to-file -q")
```

## Common Pitfalls
- **Both `--cid` and `--pid` are required in quiet mode** (`-q`). Without them, the command tries interactive realm selection which will hang from MCP.
- **Without `--save-to-file`, credentials are not persisted.** Every subsequent command would need to re-authenticate.
- **CID can be an alias or numeric ID.** The CLI resolves aliases automatically.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.
- **`dotnet tool restore` runs automatically** during init to set up the local toolchain.
