// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

using System;

namespace Beamable.Common.Content
{
	public class ContentNotFoundException : Exception
	{
		public ContentNotFoundException(string contentId = "unknown") : base($"Content reference not found with ID: '{contentId}' ")
		{

		}

		public ContentNotFoundException(Type type) : base($"No content name found for type=[{type.Name}]")
		{

		}
	}
}
