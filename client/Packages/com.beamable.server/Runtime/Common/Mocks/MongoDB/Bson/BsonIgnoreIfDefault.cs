// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

#if !BEAMABLE_IGNORE_MONGO_MOCKS

using System;

namespace MongoDB.Bson.Serialization.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class BsonIgnoreIfDefaultAttribute : Attribute, IBsonMemberMapAttribute
	{
		private bool _value;

		public BsonIgnoreIfDefaultAttribute() => this._value = true;

		public BsonIgnoreIfDefaultAttribute(bool value) => this._value = value;

		public bool Value => this._value;
	}
}
#endif
