// this file was copied from nuget package Beamable.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Common/4.1.5

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
