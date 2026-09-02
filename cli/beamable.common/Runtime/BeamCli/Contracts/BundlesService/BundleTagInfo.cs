using System;

namespace Beamable.Common.BeamCli.Contracts
{
	/// <summary>
	/// Contract projection of a bundle's tags. The API returns them as a tag-to-checksum map
	/// whose types only exist in the CLI assembly, so command outputs expose this type instead,
	/// allowing it to be copied into other SDKs.
	/// </summary>
	[CliContractType, Serializable]
	public class BundleTagInfo
	{
		/// <summary>
		/// The content checksum (sha256:...) the tag currently points at.
		/// </summary>
		public string checksum;

		/// <summary>
		/// The tag name (e.g. "latest").
		/// </summary>
		public string tag;
	}
}
