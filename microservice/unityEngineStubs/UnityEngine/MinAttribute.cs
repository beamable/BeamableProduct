using System;

namespace UnityEngine
{
	/// <summary>
	///   <para>Attribute used to make a float or int variable in a script be restricted to a specific minimum value.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class MinAttribute : PropertyAttribute
	{
		/// <summary>
		///   <para>The minimum allowed value.</para>
		/// </summary>
		public readonly float min;

		/// <summary>
		///   <para>Attribute used to make a float or int variable in a script be restricted to a specific minimum value.</para>
		/// </summary>
		/// <param name="min">The minimum allowed value.</param>
		public MinAttribute(float min)
		{
			this.min = min;
		}
	}
}
