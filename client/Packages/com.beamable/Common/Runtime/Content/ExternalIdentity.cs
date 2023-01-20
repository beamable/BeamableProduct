#pragma warning disable CS0618

namespace Beamable.Common.Content
{
	[System.Serializable]
	[Agnostic]
	public class ExternalIdentity
	{
		[ContentField("service")]
		public string Service;
		[ContentField("namespace")]
		public string Namespace;
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalExternalIdentity : Optional<ExternalIdentity> { }
}
