// this file was copied from nuget package Beamable.Server.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.0-PREVIEW.RC2

#if !BEAMABLE_IGNORE_MONGO_MOCKS

using System;

namespace MongoDB.Bson.Serialization.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class BsonIgnoreIfNullAttribute : Attribute, IBsonMemberMapAttribute
	{
		private bool _value;

		public BsonIgnoreIfNullAttribute() => this._value = true;

		public BsonIgnoreIfNullAttribute(bool value) => this._value = value;

		public bool Value => this._value;

	}
}
#endif
