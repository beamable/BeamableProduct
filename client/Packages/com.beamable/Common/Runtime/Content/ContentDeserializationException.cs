// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

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
