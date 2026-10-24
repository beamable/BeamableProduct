These YAML fixtures were saved by Unity 2022.3.62f3. The legacy types used raw
`phases`/`teams` lists and string `symbol`, `contentId`, `itunes`, and `googleplay`
fields. Modern fixtures use the current wrappers. Empty fixtures use the legacy
shape with empty lists and strings.

Only the script GUID is replaced at import time. The modern event fixture also
uses a fixed schedule timestamp. Keep the old field names in these files:
regenerating them from current types would remove the migration coverage.

`ContentMigrationAssetTests` imports these fixtures through Unity, saves only
the test asset, unloads it, and imports it again. Six misspelled-name controls
check that legacy values are actually read by name. No legacy fields are set
through reflection and callbacks are not invoked directly.

Run the `Beamable.Editor.Tests.ContentMigrationAssetTests` EditMode tests in the
Unity Test Runner. Temporary assets use a unique folder under `Assets` and are
removed during teardown. The previous serialization mode is restored. This
covers asset reload, not a full editor restart.
