// this file was copied from nuget package Beamable.Server.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Server.Common/3.0.0-PREVIEW.RC4

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	[BsonMemberMapAttributeUsage(AllowMultipleMembers = false)]
	public class BsonIdAttribute : Attribute, IBsonMemberMapAttribute { }
}
#endif
