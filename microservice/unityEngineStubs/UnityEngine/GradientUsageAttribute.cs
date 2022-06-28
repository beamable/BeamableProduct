using System;

namespace UnityEngine
{
	/// <summary>
	///   <para>Attribute used to configure the usage of the GradientField and Gradient Editor for a gradient.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public sealed class GradientUsageAttribute : PropertyAttribute
	{
		/// <summary>
		///   <para>If set to true the Gradient uses HDR colors.</para>
		/// </summary>
		public readonly bool hdr = false;

		/// <summary>
		///   <para>Attribute for Gradient fields. Used for configuring the GUI for the Gradient Editor.</para>
		/// </summary>
		/// <param name="hdr">Set to true if the colors should be treated as HDR colors (default value: false).</param>
		public GradientUsageAttribute(bool hdr)
		{
			this.hdr = hdr;
		}
	}
}
