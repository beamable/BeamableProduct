// this file was copied from nuget package Beamable.Common@4.3.0
// https://www.nuget.org/packages/Beamable.Common/4.3.0

﻿namespace Beamable.Common.Api
{
	public interface IBeamableFilesystemAccessor
	{
		/// <summary>
		/// Retrieve a file path that can be safely written and read from.
		/// </summary>
		/// <returns>A filepath</returns>
		string GetPersistentDataPathWithoutTrailingSlash();
	}
}
