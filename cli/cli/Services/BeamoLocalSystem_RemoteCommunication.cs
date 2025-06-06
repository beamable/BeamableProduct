/**
 * This part of the class defines how we manage Beamo Services and how we deploy them remotely to Beam-O.
 * It handles synchronizing the remote and local manifests, generating a remote manifest from the locally defined data, uploading the docker images to a registry and finally pushing the manifest
 * up to Beam-O.
 */

using Beamable.Serialization.SmallerJSON;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	/// <summary>
	/// Docker manifest data structure, from JSON like:
	///   [{"Config":"...","RepoTags":["..."],"Layers":["...","..."]}]
	/// But the uploader does not need RepoTags so we omit it.
	/// </summary>
	[Serializable]
	public class DockerManifest
	{
		public string config;
		public string[] layers;

		/// <summary>
		/// Given JSON bytes that fit the expected Docker manifest
		/// schema, create a manifest data structure for the first
		/// manifest in the JSON.
		/// </summary>
		/// <param name="bytes">JSON data bytes.</param>
		/// <returns>Manifest data structure.</returns>
		public static DockerManifest FromBytes(byte[] bytes)
		{
			var result = new DockerManifest();
			var manifests = (List<object>)Json.Deserialize(bytes);
			var firstManifest = (IDictionary<string, object>)manifests[0];
			result.config = firstManifest["Config"].ToString();
			var layers = (List<object>)firstManifest["Layers"];
			result.layers = layers?.Select(x => x.ToString()).ToArray();
			return result;
		}
	}

}
