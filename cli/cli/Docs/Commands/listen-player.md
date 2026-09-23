# About

The {{title}} command will monitor events sent to the logged in
player on the CLI. Player events include updates such as content
notifications, inventory updates, mail updates, and more.

**IMPORTANT**: The command only works if the realm is configured to use
Beamable Notifications, which is the default setting for new realms
as of July 2023. However, if the realm is not using Beamable Notifications,
the following error will be displayed.

```
Only realms with beam notifications are supported. This realm currently has pubnub.
Try setting the realm config to beam with this command,
""beam config realm set --key-values 'notification|publisher::beamable'""
```

To get started with the command, make sure you have the [CLI configured](../../../../guides/getting-started/),
and an available player.

1. Run `beam me` to get access to the player's `playerId`.
2. Use the portal, and navigate to the player's inventory page.
3. Run `beam listen player`, and leave the program running. This starts the listening process.
4. On the portal, change the player's currency value. Alternatively, send the player some mail.
5. On the console, notice the event is received.

## Probing the realtime session

`beam listen player --probe` checks that the realm's realtime socket works, then exits. It opens one session the same way the Web SDK does:

1. It authenticates as the logged-in player (or the `--refresh-token` you pass). With `--guest`, or when no one is logged in, it creates a new guest player instead.
2. It exchanges the refresh token at `/api/auth/tokens/refresh-token` for the JWT the socket accepts. The opaque token from `/basic/auth/token` is rejected by the socket with a 401.
3. It reads the socket URI from the realm's client defaults (`websocketConfig.uri`), and fails with the error above if the provider is `pubnub`.
4. It connects to `{uri}/connect?access_token=<jwt>&send-session-start=true`, sends the `session-start` frame, and listens for `--probe-seconds` seconds (default 5).

The probe succeeds (exit code 0) when the socket opens, the `session-start` frame is sent, and the server does not close the socket during the listen window. Otherwise it exits with code 1 and an error saying what to try next. If the socket is still connecting after 15 seconds, the probe reports that WebSockets may be blocked by a proxy or firewall.

The result is written to the `probe` channel, so tools such as the MCP `beam_exec` tool can read it:

```
beam listen player --probe --guest
```

| Field | Meaning |
| --- | --- |
| `success`, `message` | The verdict, and a summary that says what to try next on failure |
| `failedStage` | Empty on success, otherwise `realm-config`, `token`, `connect`, `session-start` or `listen` |
| `identity` | `guest` or `current` |
| `provider`, `socketUri` | The realm's websocket provider and URI (the access token is never included) |
| `handshakeStatusCode` | `101` when the upgrade was accepted, the HTTP status when it was rejected (such as `401`), or `0` when no response was read |
| `opened`, `timeToOpenMs`, `connectTimedOut` | Whether the socket opened, how long it took, and whether it was still connecting after 15 seconds |
| `sessionStartSent`, `sessionStartFrame` | Whether the `session-start` frame was sent, and the frame itself |
| `framesReceived`, `frames` | How many frames arrived during the listen window, and the first few (truncated) |
| `closedByServer`, `closedAfterMs`, `closeStatusCode`, `closeStatusDescription` | Whether and when the server closed the socket, and the close status it sent |
| `error` | The underlying error, if any |
