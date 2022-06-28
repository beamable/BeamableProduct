using System;

namespace UnityEngine
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class TooltipAttribute : PropertyAttribute
	{
		public readonly string tooltip;

		public TooltipAttribute(string tooltip)
		{
			this.tooltip = tooltip;
		}
	}
}
