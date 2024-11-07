// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

using System;

namespace Beamable.Common.Content
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class ContentFieldAttribute : Attribute
	{
		public string SerializedName { get; set; }
		public string[] FormerlySerializedAs { get; set; }

		public ContentFieldAttribute()
		{

		}

		public ContentFieldAttribute(string name)
		{
			SerializedName = name;
		}

		public ContentFieldAttribute(string name = null, params string[] formerlySerializedAs)
		{
			SerializedName = name;
			FormerlySerializedAs = formerlySerializedAs;
		}

	}
}
