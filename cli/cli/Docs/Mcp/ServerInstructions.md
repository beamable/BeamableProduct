Beamable CLI (`beam`) MCP server. Tools run `beam` commands in the current workspace.

Workflow
- For multi-step work (new project, microservice, web game, deploy, content), call `beam_get_skill("")` first to list guides, then load the relevant one.
- If you know a command, call `beam_exec` directly; on invalid arguments it returns the command's help. Use `beam_list_commands` / `beam_get_help` to discover commands you don't know.
- `beam_exec` runs commands non-interactively (`-q` is added for you), so pass every value a command would otherwise prompt for.
- A workspace needs `.beamable/` first: `init --cid <cid>` (see `beam_get_help("init")`).

Choosing a realm without prompts
- `org realms -q` lists realms with `RealmName` and `Pid`; then `config set pid <Pid> -q`.

Web SDK (browser / Node / React Native)
- The npm package is `@beamable/sdk` (not `beamable-sdk`).
- Typed microservice clients come from `project generate web-client --output-dir <dir>`. Build the service first (`project build`): the generator reads `beam_openApi.json` from the build output.
- A realm used by the Web SDK needs:
  - realm config `notification|publisher=beamable`: `config realm set --key-values 'notification|publisher::beamable'`
  - a published `global` content manifest: `content publish` (works with no content). Without it `Beam.init` fails with a 404 on `/basic/content/manifest/public/json?id=global`.
- `Beam.init` waits for the realtime socket to open and send `session-start`. If it hangs or times out, WebSockets are blocked (proxy, firewall, sandbox); `Unsupported websocket provider` means the realm config above is missing.

Microservices (C#)
- `Promise<T>` is awaitable directly; no `ToTask()` needed.
- `[ClientCallable]` request/response DTOs use public fields and `[Serializable]`.
- `Context.UserId` is the calling player's id.

Deploying
- `deploy release` defaults to `--replace`, which removes remote services that are missing locally. Use `--merge` when unsure.
