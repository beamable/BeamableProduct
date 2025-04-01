// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC5
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC5

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
