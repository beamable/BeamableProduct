// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

﻿namespace Beamable.Content
{
	/// <summary>
	/// When content is serialized, any type that inherits from this interface will not have their Unity
	/// serialization callback methods during for the content serialization.
	/// </summary>
	public interface IIgnoreSerializationCallbacks { }
}
