using System;

namespace UnityEngine
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class HeaderAttribute : PropertyAttribute
	{
		public readonly string header;
		public HeaderAttribute(string header)
		{
			this.header = header;
		}
	}
}
