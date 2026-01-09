// this file was copied from nuget package Beamable.Server.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.0-PREVIEW.RC2

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	public abstract class BsonSerializationOptionsAttribute : Attribute, IBsonMemberMapAttribute { }
}

#endif
