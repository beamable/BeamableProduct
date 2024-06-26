// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

using System;
using UnityEngine;

namespace Beamable.Common.Content.Validation
{
	[AttributeUsage(AttributeTargets.Field)]
	public class TimeSpanDisplayAttribute : PropertyAttribute
	{
		public string FieldName { get; }

		public TimeSpanDisplayAttribute(string fieldName)
		{
			FieldName = fieldName;
		}
	}
}
