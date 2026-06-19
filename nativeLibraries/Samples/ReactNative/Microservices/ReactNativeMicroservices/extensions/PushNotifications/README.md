# PushNotifications (Portal extension)

An admin Portal page for the **PushNotificationService** microservice. It lists every
player who has registered a push device and lets you send a remote push to any one of them.

## What it does

- **Registered players table** — calls `[AdminOnlyCallable] ListRegisteredPlayers` and shows
  each player's ID, device count, platforms (`apns`/`fcm`), and last-updated time. **Select**
  copies a player ID into the send form.
- **Send a notification** — calls `[AdminOnlyCallable] SendPushToPlayer(playerId, title, body, deepLink)`
  and reports how many devices were delivered to.

## How the roster is discovered

Device tokens live in a **private** per-player stat (`apns_devices`), and private stats can't be
enumerated across players. So the service also writes a **public** marker stat,
`push_devices` = device count, on every register/unregister/prune (see `DeviceTokenStore.SaveDevices`).
`ListRegisteredPlayers` finds the roster with `SearchStats("game","public","player", push_devices > 0)`
and then loads each player's private device summary. Tokens are never returned to the client.

## Run it

```bash
npm install
npm run dev          # vite dev server; load the extension in the Portal
# or, once the realm serves the portal-config endpoint:
beam project run --ids PushNotifications
beam portal open-extension PushNotifications
```

The microservice must be running (locally or deployed) for the calls to resolve. You'll need an
**admin** Portal session — both endpoints are `[AdminOnlyCallable]`.

## Note: hand-authored scaffold

This extension was created by hand rather than via `beam project new portal-extension` because that
command (and `portal extension list-extension-options` / `portal extension add-microservice`)
requires the realm's **remote portal-config** endpoint, which returns 404 on the current realm
(`api.beamable.com`, `DE_83112773772143616`). The CLI still **discovers** the extension by
convention (`beam project ps` lists it as a `portalExtension`), so this only affects scaffolding,
not use.

Consequences:
- `beamable/clients/PushNotificationServiceClient.ts` + `beamable/clients/types/` are hand-written
  to mirror the generator's output. If the service's endpoint signatures change, re-run
  `beam portal extension add-microservice PushNotifications PushNotificationService` (once the
  endpoint is reachable) to regenerate them.
- The mount config in `package.json` (`page`, `selector`) mirrors the working `SampleOneHealthCheck`
  example and was not validated against remote mount options.
