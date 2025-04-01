// this file was copied from nuget package Beamable.Server.Common@4.2.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Server.Common/4.2.0-PREVIEW.RC4

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	public abstract class BsonSerializationOptionsAttribute : Attribute, IBsonMemberMapAttribute { }
}

#endif
