// this file was copied from nuget package Beamable.Server.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.0-PREVIEW.RC2

using System;
#if !BEAMABLE_IGNORE_MONGO_MOCKS

namespace MongoDB.Bson.Serialization
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class BsonMemberMapAttributeUsageAttribute : Attribute
	{
		private bool _allowMultipleMembers;

		public BsonMemberMapAttributeUsageAttribute() => this._allowMultipleMembers = true;

		public bool AllowMultipleMembers
		{
			get => this._allowMultipleMembers;
			set => this._allowMultipleMembers = value;
		}
	}
}
#endif
