using System;

namespace UnityEngine
{
	/// <summary>
	///   <para>Use this PropertyAttribute to add some spacing in the Inspector.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class SpaceAttribute : PropertyAttribute
	{
		/// <summary>
		///   <para>The spacing in pixels.</para>
		/// </summary>
		public readonly float height;

		public SpaceAttribute()
		{
			this.height = 8f;
		}

		/// <summary>
		///   <para>Use this DecoratorDrawer to add some spacing in the Inspector.</para>
		/// </summary>
		/// <param name="height">The spacing in pixels.</param>
		public SpaceAttribute(float height)
		{
			this.height = height;
		}
	}
}
