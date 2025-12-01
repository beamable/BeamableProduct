// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

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
