
using System;

namespace UnityEngine
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class RangeAttribute : Attribute
	{
		public readonly float min;
		public readonly float max;

		public RangeAttribute(float min, float max)
		{
			this.min = min;
			this.max = max;
		}
	}
}
