using System;

namespace UnityEngine
{
	public struct Color : IEquatable<Color>
	{
		/// <summary>
		///   <para>Red component of the color.</para>
		/// </summary>
		public float r;
		/// <summary>
		///   <para>Green component of the color.</para>
		/// </summary>
		public float g;
		/// <summary>
		///   <para>Blue component of the color.</para>
		/// </summary>
		public float b;
		/// <summary>
		///   <para>Alpha component of the color (0 is transparent, 1 is opaque).</para>
		/// </summary>
		public float a;

		public Color(float r, float g, float b, float a)
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}

		public Color(float r, float g, float b)
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = 1f;
		}

		public bool Equals(Color other)
		{
			return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
		}

		public override bool Equals(object obj)
		{
			return obj is Color other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = r.GetHashCode();
				hashCode = (hashCode * 397) ^ g.GetHashCode();
				hashCode = (hashCode * 397) ^ b.GetHashCode();
				hashCode = (hashCode * 397) ^ a.GetHashCode();
				return hashCode;
			}
		}
	}
}
