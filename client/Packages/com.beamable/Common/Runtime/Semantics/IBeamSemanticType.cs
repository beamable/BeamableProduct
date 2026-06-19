// this file was copied from nuget package Beamable.Common@7.2.0
// https://www.nuget.org/packages/Beamable.Common/7.2.0

﻿using Beamable.Serialization.SmallerJSON;

namespace Beamable.Common.Semantics
{
	public interface IBeamSemanticType : IRawJsonProvider
	{
		string SemanticName { get; }
	}
	
    public interface IBeamSemanticType<T> : IBeamSemanticType
    {
    }
}
