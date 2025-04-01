// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC5
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC5

﻿namespace Beamable.Content
{
	/// <summary>
	/// When content is serialized, any type that inherits from this interface will not have their Unity
	/// serialization callback methods during for the content serialization.
	/// </summary>
	public interface IIgnoreSerializationCallbacks { }
}
