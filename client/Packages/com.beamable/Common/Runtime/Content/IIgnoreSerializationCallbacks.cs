// this file was copied from nuget package Beamable.Common@7.2.0
// https://www.nuget.org/packages/Beamable.Common/7.2.0

﻿namespace Beamable.Content
{
	/// <summary>
	/// When content is serialized, any type that inherits from this interface will not have their Unity
	/// serialization callback methods during for the content serialization.
	/// </summary>
	public interface IIgnoreSerializationCallbacks { }
}
