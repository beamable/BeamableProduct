// this file was copied from nuget package Beamable.Server.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Server.Common/4.1.5

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class BsonIgnoreAttribute : Attribute
	{
	}
}

#endif
