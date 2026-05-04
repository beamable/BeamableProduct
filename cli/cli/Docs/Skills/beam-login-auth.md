---
name: beam-login-auth
description: Authenticate with Beamable using credentials or refresh tokens.
---

# Login and Authentication

## Authentication Methods

### Username and password
```
beam_exec("login --email <email> --password <password> -q")
```

### Refresh token (for automation and CI/CD)
```
beam_exec("login --refresh-token <token> -q")
```

## `beam login` options
| Option | Type | Description |
|---|---|---|
`--email` | string | Specify user email address
`--password` | string | User password
`--save-to-environment` | flag | Save login refresh token to environment variable
`--no-token-save` | flag | Prevent auth tokens from being saved to disk. This replaces the legacy --save-to-file option
`--realm-scoped` | flag | Makes the resulting access/refresh token pair be realm scoped instead of the default customer scoped one
`--refresh-token` | string | A Refresh Token to use for the requests. It overwrites the logged in user stored in auth.beam.json for THIS INVOCATION ONLY
`--print-to-console` | flag | Prints out login request response to console


## Checking Current Auth Status
```
beam_exec("config")
```
This shows the current CID, PID, and authentication state.

## Common Pitfalls
- **Tokens save by default.** Use `--no-token-save` if you need ephemeral credentials.
- **Email must contain `@`** — this is the only format validation on the username.
- **Expired refresh tokens fail silently.** If commands start failing after a period, re-authenticate.
- **Always pass `-q`** when executing from MCP to avoid interactive prompts.
- **Login is often implicit.** Commands like `beam init` handle authentication as part of their flow. You only need explicit `beam login` when credentials have expired or were not saved.

## Wrap-Up

After completing the workflow, provide the user with a summary that covers:

1. **What was done**: Whether authentication used email/password or a refresh token, and whether it succeeded.
2. **Where credentials are stored**:
   - Auth token: `.beamable/connection-configuration.json` — the refresh token that authenticates future CLI commands. This file should NOT be committed to version control.
   - If `--save-to-environment` was used: the refresh token is also stored in the `BEAM_REFRESH_TOKEN` environment variable for the current session.
3. **Why specific choices were made** — explain the reasoning:
   - **Email/password vs refresh token**: Email/password is for interactive use and generates a new token pair. Refresh tokens are for automation (CI/CD, scripts) where interactive login is not possible — they are long-lived and can be rotated.
   - **Realm-scoped vs global**: By default, login produces a token valid across all realms in the organization. If `--realm-scoped` was used, the token only works for the current PID — useful for restricting CI/CD credentials to a specific environment.
   - **Token persistence**: Tokens save by default so subsequent commands work without re-authenticating. If `--no-token-save` was used, explain that the credentials are ephemeral and will not persist after the current session.
4. **How to verify**: The user can run `beam config` to confirm the current authentication state, CID, and PID.

## CLI Version Awareness

If the CLI version has changed (check `.config/dotnet-tools.json`), re-run `beam_list_commands()` and `beam_get_help()` to get up-to-date command information. Command options and behavior may have changed between versions.
