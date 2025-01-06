#pragma warning disable CS0618

namespace Beamable.Common.Content
{
	[System.Serializable]
	[Agnostic]
	public class Federation
	{
		[ContentField("service")]
		public string Service;
		[ContentField("namespace")]
		public string Namespace;
	}

	[System.Serializable]
	[Agnostic]
	public class Namespace
	{
		[ContentField("name")]
		public string Name;
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalFederation : Optional<Federation> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalNamespace : Optional<Namespace>{}
}
