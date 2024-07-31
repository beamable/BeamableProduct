// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407221259
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407221259

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	public abstract class BsonSerializationOptionsAttribute : Attribute, IBsonMemberMapAttribute { }
}

#endif
