using System;
using UnityEngine;

namespace Beamable
{

	[AttributeUsage(AttributeTargets.Field)]
	public class HelpBoxDecoratorAttribute : PropertyAttribute
	{
		public string Tooltip { get; }
		public HelpBoxDecoratorAttribute(string tooltip)
		{
			Tooltip = tooltip;
		}
	}
}
