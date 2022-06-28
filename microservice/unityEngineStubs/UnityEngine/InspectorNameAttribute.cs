using System;

namespace UnityEngine
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	internal class InspectorNameAttribute : PropertyAttribute
	{
		public readonly string displayName;

		public InspectorNameAttribute(string displayName)
		{
			this.displayName = displayName;
		}
	}
}
