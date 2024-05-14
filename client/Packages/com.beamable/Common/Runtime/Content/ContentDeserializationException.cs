// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

using System;

namespace Beamable.Common.Content
{
	public class ContentDeserializationException : Exception
	{
		public ContentDeserializationException(string json) : base($"Failed to deserialize content. json='{json}'")
		{

		}
	}
}
