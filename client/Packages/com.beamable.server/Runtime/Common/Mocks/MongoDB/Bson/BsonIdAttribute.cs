// this file was copied from nuget package Beamable.Server.Common@4.2.0-PREVIEW.RC3
// https://www.nuget.org/packages/Beamable.Server.Common/4.2.0-PREVIEW.RC3

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	[BsonMemberMapAttributeUsage(AllowMultipleMembers = false)]
	public class BsonIdAttribute : Attribute, IBsonMemberMapAttribute { }
}
#endif
