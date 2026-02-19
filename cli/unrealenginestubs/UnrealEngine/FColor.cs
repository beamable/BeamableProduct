using System;
using Newtonsoft.Json;

namespace UnrealEngine
{
	/// <summary>
	///   <para>A color in RGBA format (each channel is a byte).</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public struct FColor : IEquatable<FColor>
	{
		/// <summary>
		///   <para>Red component of the color.</para>
		/// </summary>
		public byte R;

		/// <summary>
		///   <para>Green component of the color.</para>
		/// </summary>
		public byte G;

		/// <summary>
		///   <para>Blue component of the color.</para>
		/// </summary>
		public byte B;

		/// <summary>
		///   <para>Alpha component of the color (0 is transparent, 255 is opaque).</para>
		/// </summary>
		public byte A;

		public FColor(byte r, byte g, byte b, byte a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public FColor(byte r, byte g, byte b)
		{
			R = r;
			G = g;
			B = b;
			A = 255;
		}

		public bool Equals(FColor other)
		{
			return R == other.R && G == other.G && B == other.B && A == other.A;
		}

		public override bool Equals(object obj)
		{
			return obj is FColor other && Equals(other);
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

		public static bool operator ==(FColor lhs, FColor rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(FColor lhs, FColor rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			return $"(R={R},G={G},B={B},A={A})";
		}

		public static FColor White => new FColor(255, 255, 255, 255);
		public static FColor Black => new FColor(0, 0, 0, 255);
		public static FColor Red => new FColor(255, 0, 0, 255);
		public static FColor Green => new FColor(0, 255, 0, 255);
		public static FColor Blue => new FColor(0, 0, 255, 255);
	}
}
