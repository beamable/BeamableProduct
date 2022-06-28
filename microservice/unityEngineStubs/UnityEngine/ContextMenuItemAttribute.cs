using System;

namespace UnityEngine
{
	/// <summary>
	///   <para>Use this attribute to add a context menu to a field that calls a  named method.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class ContextMenuItemAttribute : PropertyAttribute
	{
		/// <summary>
		///   <para>The name of the context menu item.</para>
		/// </summary>
		public readonly string name;
		/// <summary>
		///   <para>The name of the function that should be called.</para>
		/// </summary>
		public readonly string function;

		/// <summary>
		///   <para>Use this attribute to add a context menu to a field that calls a  named method.</para>
		/// </summary>
		/// <param name="name">The name of the context menu item.</param>
		/// <param name="function">The name of the function that should be called.</param>
		public ContextMenuItemAttribute(string name, string function)
		{
			this.name = name;
			this.function = function;
		}
	}
}
