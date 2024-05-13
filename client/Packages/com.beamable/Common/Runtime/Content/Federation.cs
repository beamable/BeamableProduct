// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

﻿#pragma warning disable CS0618

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
	public class OptionalFederation : Optional<Federation> { }
}
