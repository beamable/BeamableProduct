# Content asset compatibility fixtures

Each version folder contains nine assets saved by that exact Unity Editor:
`2022.3.62f3` and `6000.3.22f1`. Both producers used the full client source at
`5b0ca6185444502a7828ce6fe9a3d86b02410643`, including native Unity Common types,
with the additional producer files in `Producer~`. No Common DLL built against
Unity stubs was used.

- `legacy`: populated old field shapes (`phases`/`teams` lists and string
  `symbol`, `contentId`, `itunes`, and `googleplay` fields).
- `empty`: the same old shapes with empty lists and strings.
- `modern`: current wrapper types, including nested values.

Legacy fixtures use the explicit old-shape producer classes, not a claim that
the entire historical SDK or a customer project was exported. Modern fixtures
use the same `Migration*Asset` classes as the consumer tests.

Only `m_Script`'s GUID is replaced with `__SCRIPT_GUID__`. All other bytes are
native output, including trailing spaces and the `m_EditorClassIdentifier`
written by Unity 6000.3. The event schedule is fixed before saving so it does
not depend on the generation date.

`ContentMigrationAssetTests` imports each version's fixtures, checks the
migrated values, saves the asset, unloads it, and checks it after reimport.
The six misspelled-field controls use the 2022 fixtures. No legacy fields are
injected through reflection and no migration callbacks are called directly.
Temporary assets and the previous serialization mode are restored after each
test. This covers asset import/reload, not a full editor restart or player build.

## Reproduce the producer output

1. Export the pinned client to a separate project for each editor above and
   resolve its packages. Keep the destination project separate.
2. Copy the four `.cs` files from `Producer~` into a temporary folder under
   `Packages/com.beamable/Tests/Editor`. Unity normally ignores `Producer~`.
3. Run the EditMode test `FixtureProducer.SaveFixtures` in that editor. It
   requires a fresh `Assets/FixtureProduction` folder and writes nine `.asset`
   files there, plus `fixture-producer-result.json` with the observed editor
   version and raw asset SHA-256 hashes.
4. Copy those assets into the corresponding fixture version folder as
   `.yaml.txt`, replacing only the GUID in `m_Script` with `__SCRIPT_GUID__`.
   Do not regenerate legacy fixtures from the current wrapper types.
5. Run `Beamable.Editor.Tests.ContentMigrationAssetTests` in Unity 6000.7 to
   verify both producer versions against the destination serializer.
