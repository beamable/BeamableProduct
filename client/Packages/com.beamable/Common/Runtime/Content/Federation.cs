// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

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
