# Web microservice clients: bind to the service's scope, not the extension's

Prerequisite for `microservice-secrets-api-plan.md` step 8 (the `secrets-test` zone extension calls a
realm service through `assumeRealm`).

## Problem

A generated TypeScript microservice client is typed on one `@beamable/sdk` host type, the
`augmentType`. It controls:
- the client constructor's `beam` parameter;
- the `declare module '@beamable/sdk' { interface X { <name>Client } }` augmentation.

The two possible types are:
- `BeamBase` is the base of `Beam` and `BeamServer`. Its microservice scope is the realm `pid`.
- `BeamZoneSdk` is the zone SDK. Its microservice scope is the zone `zid`.

Today the CLI picks the type from the **extension's** `serviceScope`, so every client in a zone
extension gets `BeamZoneSdk`:

- `GeneratePortalExtensionClientsCommand.cs:79-88` (the post-build `generate pe-client` path)
- `PortalExtensionAddDependencyCommand.GenerateDependenciesClients` (`:94-97`, also run by `project run`)

That is wrong when a zone extension depends on a **realm** service. The only correct way to reach a
realm service from a zone is `zoneBeam.assumeRealm(pid)`, which returns a `BeamServer`, i.e. a
`BeamBase`. A `BeamZoneSdk`-typed client:

- **rejects the right host at compile time.** In agentic-portal,
  `tsc --noEmit` on `push-message-rail` fails with
  `TS2345: Argument of type 'BeamBase' is not assignable to parameter of type 'BeamZoneSdk'`
  (`notificationConfigClient.ts:115,133`). It only ships because `beam-build` is `vite build`, which
  doesn't type-check.
- **accepts the wrong host.** `new XClient(context.beam)` type-checks, but at runtime it routes to
  `/basic/{cid}.{zid}.micro_X`, and that realm service isn't there.
- **pushes people into workarounds.** Extensions cast (`email-message-rail`:
  `beam as ClientBeam`) or hand-write a client over `BeamBase` (`message-rail-debug`:
  `lib/rosterClient.ts`).

Affected today: 8 zone extensions in agentic-portal depend on realm services (the three message
rails, `PlayerEngagementCampaignOfferCatalogService`, `PlayerStatsService`). The reverse mistake
also shows up there, which suggests stale generation:
`campaigns/beamable/clients/CampaignServiceZoneClient.ts` is typed `BeamBase`.

The generic `GenerateWebClientCommand` (`:58`) always uses the default `BeamBase`, so a zone
service's client comes out as `BeamBase` there too.

## Fix

The host type is a property of the **service**: a client needs a host whose `microServiceScope` is
the service's scope. So choose the type from the microservice definition:

- `BeamoServiceDefinition.IsZoneScoped == true` → `BeamZoneSdk`
- otherwise → `BeamBase`

Changes:

1. **`WebClientCodeGenerator`:** add `public static string AugmentTypeFor(BeamoServiceDefinition service)`
   next to the constructor, and update the `_augmentType` doc comment to say "the scope of the
   service the client calls". This lives in the existing class, so no new helper type.
2. **`GeneratePortalExtensionClientsCommand`:** replace the extension-scope `isZone` block with
   `WebClientCodeGenerator.AugmentTypeFor(ms)`. `ms` is already the service definition here. This
   also lets the type be computed once per service, outside the per-extension loop.
3. **`PortalExtensionAddDependencyCommand.GenerateDependenciesClients`:**
   - look up the definition with `manifest.ServiceDefinitions.FirstOrDefault(d => d.BeamoId == beamId)`
     and pass it to `AugmentTypeFor`;
   - remove `IsZoneExtension`, which has no other callers;
   - fix the comment.
4. **`GenerateWebClientCommand`:** apply the same lookup, so zone services get `BeamZoneSdk`.
5. **Unsupported combination: a realm extension depending on a zone service.** A realm extension
   has no `BeamZoneSdk`, so nothing it holds can call that client. Generate it anyway (the type is
   still correct), and log a warning naming the extension and the service. Don't fail, because
   `project run` regenerates on every start.

## Tests (`cli/tests`)

Add a test fixture for `WebClientCodeGenerator`. It uses a minimal `OpenApiDocument` with one
callable, writes to the temp directory, and asserts on the generated file:

- A realm service produces `type BeamBase` in the import, `beam: BeamBase` in the constructor, and
  `interface BeamBase {` in the augmentation.
- A zone service produces `BeamZoneSdk` in the same three places.
- A command-level case, if the existing `CLITest` manifest setup makes it cheap: a zone extension
  depending on a realm service produces a `BeamBase` client. This is the case from agentic-portal.

## Rollout / agentic-portal follow-up (not part of this change)

- Regenerating (`project run` or `generate pe-client`) rewrites the 8 affected clients to
  `BeamBase`. Call sites already pass `realmBeam` / `BeamBase`, so they become type-correct and
  `push-message-rail`'s `tsc` errors go away.
- Any zone extension that passes `context.beam` (the zone SDK) to a realm client starts failing
  `tsc`. That's intended, because those calls were routed to the zid. I didn't find any, but a
  `tsc --noEmit` sweep after regeneration will confirm.
- The `email-message-rail` cast and `message-rail-debug`'s `RailRosterClient` can then be removed.
  That cleanup belongs in the agentic-portal repo; it's listed here so we don't forget it.

## Out of scope

Making `beam-build` type-check (`tsc && vite build`) would have caught this, but it's an
agentic-portal template change.
