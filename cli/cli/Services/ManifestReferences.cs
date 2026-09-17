using System.Collections.Generic;

namespace cli;

/// <summary>
/// In-memory model of the v2 root manifest file <c>.beamable/manifest.beam.json</c>. It holds the
/// content-addressed bundle references for the current realm plus the manifest schema version.
/// The inline <c>manifest[]</c> / <c>storageReferences[]</c> / <c>portalExtensionReferences[]</c>
/// arrays are NOT stored here — they are derived from project source at plan time. See
/// <c>DesignDocs/infra/beamo-manifest/beamo-manifest-redesign.md</c>.
/// </summary>
public class ManifestReferences
{
	/// <summary>The manifest schema version. v2 = bundle-references model.</summary>
	public int schemaVersion = ConfigService.MANIFEST_SCHEMA_VERSION;

	/// <summary>Map of bundle name (e.g. <c>&lt;namespace&gt;/&lt;bundle-name&gt;</c>) to content checksum (<c>sha256:&lt;checksum&gt;</c>).</summary>
	public Dictionary<string, string> references = new Dictionary<string, string>();
}
