---
description: Authenticate with Beamable using credentials or refresh tokens.
---

# Login and Authentication

## Authentication Methods

### Username and password
```
beam_exec("login --username <email> --password <password> --save-to-file -q")
```

### Refresh token (for automation and CI/CD)
```
beam_exec("login --refresh-token <token> --save-to-file -q")
```

## Token Persistence Options
- `--save-to-file` — Saves credentials to `.beamable/token.json` for subsequent commands
- `--save-to-environment` — Saves as `BEAMABLE_REFRESH_TOKEN` environment variable
- `--print-to-console` — Outputs the token to stdout for capture
- `--realm-scoped` — Limits the token scope to the current realm (advisory, not enforced)

## Checking Current Auth Status
```
beam_exec("config")
```
This shows the current CID, PID, and authentication state.

## Common Pitfalls
- **Without `--save-to-file`, credentials are ephemeral.** They are lost when the command finishes, and every subsequent command must re-authenticate.
- **Email must contain `@`** — this is the only format validation on the username.
- **Expired refresh tokens fail silently.** If commands start failing after a period, re-authenticate.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.
- **Login is often implicit.** Commands like `beam init` handle authentication as part of their flow. You only need explicit `beam login` when credentials have expired or were not saved.
