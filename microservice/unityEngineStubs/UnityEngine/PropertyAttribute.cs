
using System;

namespace UnityEngine
{
	[AttributeUsage(AttributeTargets.Field)]
	public abstract class PropertyAttribute : Attribute
	{
		public int order { get; set; }
	}
}
