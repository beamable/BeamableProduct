
using System;
namespace UnityEngine.Serialization
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
	public class FormerlySerializedAsAttribute : Attribute
	{
		private string m_oldName;

		public FormerlySerializedAsAttribute(string oldName)
		{
			this.m_oldName = oldName;
		}

		public string oldName => this.m_oldName;
	}
}
