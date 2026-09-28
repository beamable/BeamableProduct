using System.Collections.Generic;

namespace cli;

/// <summary>
/// In-memory model of the v2 root manifest file <c>.beamable/bundles.manifest.beam.json</c>. It holds the
/// content-addressed bundle references, split by deploy scope, plus the manifest schema version.
/// A bundle reference lives in <see cref="realm"/> or <see cref="zone"/> depending on the bundle's
/// scope, so a realm deploy consumes only realm pins and a zone deploy only zone pins.
/// The inline <c>manifest[]</c> / <c>storageReferences[]</c> / <c>portalExtensionReferences[]</c>
/// arrays are NOT stored here — they are derived from project source at plan time. See
/// <c>DesignDocs/infra/beamo-manifest/beamo-manifest-redesign.md</c>.
/// </summary>
public class ManifestReferences
{
	/// <summary>The manifest schema version. v2 = bundle-references model.</summary>
	public int schemaVersion = ConfigService.MANIFEST_SCHEMA_VERSION;

	/// <summary>Realm-scoped bundle references: bundle name (<c>&lt;namespace&gt;/&lt;bundle-name&gt;</c>) to content checksum (<c>sha256:&lt;checksum&gt;</c>).</summary>
	public Dictionary<string, string> realm = new Dictionary<string, string>();

	/// <summary>Zone-scoped bundle references, same shape as <see cref="realm"/>.</summary>
	public Dictionary<string, string> zone = new Dictionary<string, string>();

	/// <summary>The reference map for a scope — the <see cref="zone"/> section when zone-scoped, else <see cref="realm"/>.</summary>
	public Dictionary<string, string> ForScope(bool isZone) => isZone ? zone : realm;
}
