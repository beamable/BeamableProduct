// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

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
