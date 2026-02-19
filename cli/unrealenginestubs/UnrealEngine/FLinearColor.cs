using System;
using Newtonsoft.Json;

namespace UnrealEngine
{
	/// <summary>
	///   <para>A linear, 32-bit/component floating point RGBA color.</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public struct FLinearColor : IEquatable<FLinearColor>
	{
		/// <summary>
		///   <para>Red component of the color.</para>
		/// </summary>
		public float R;

		/// <summary>
		///   <para>Green component of the color.</para>
		/// </summary>
		public float G;

		/// <summary>
		///   <para>Blue component of the color.</para>
		/// </summary>
		public float B;

		/// <summary>
		///   <para>Alpha component of the color (0 is transparent, 1 is opaque).</para>
		/// </summary>
		public float A;

		public FLinearColor(float r, float g, float b, float a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public FLinearColor(float r, float g, float b)
		{
			R = r;
			G = g;
			B = b;
			A = 1f;
		}

		public bool Equals(FLinearColor other)
		{
			return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
		}

		public override bool Equals(object obj)
		{
			return obj is FLinearColor other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = R.GetHashCode();
				hashCode = (hashCode * 397) ^ G.GetHashCode();
				hashCode = (hashCode * 397) ^ B.GetHashCode();
				hashCode = (hashCode * 397) ^ A.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(FLinearColor lhs, FLinearColor rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(FLinearColor lhs, FLinearColor rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			return $"(R={R:F3},G={G:F3},B={B:F3},A={A:F3})";
		}

		public static FLinearColor White => new FLinearColor(1f, 1f, 1f, 1f);
		public static FLinearColor Black => new FLinearColor(0f, 0f, 0f, 1f);
		public static FLinearColor Red => new FLinearColor(1f, 0f, 0f, 1f);
		public static FLinearColor Green => new FLinearColor(0f, 1f, 0f, 1f);
		public static FLinearColor Blue => new FLinearColor(0f, 0f, 1f, 1f);
		public static FLinearColor Transparent => new FLinearColor(0f, 0f, 0f, 0f);
	}
}
