// this file was copied from nuget package Beamable.Server.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.0-PREVIEW.RC2

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class BsonRepresentationAttribute : BsonSerializationOptionsAttribute
	{
		private BsonType _representation;
		public BsonRepresentationAttribute(BsonType representation) => this._representation = representation;
	}
}
#endif
