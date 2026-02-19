using System;
using Newtonsoft.Json;

namespace UnrealEngine
{
	/// <summary>
	///   <para>A vector in 3-D space composed of components (X, Y, Z) with floating point precision.</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public struct FVector : IEquatable<FVector>
	{
		/// <summary>
		///   <para>Vector's X component.</para>
		/// </summary>
		public double X;

		/// <summary>
		///   <para>Vector's Y component.</para>
		/// </summary>
		public double Y;

		/// <summary>
		///   <para>Vector's Z component.</para>
		/// </summary>
		public double Z;

		public FVector(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public bool Equals(FVector other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
		}

		public override bool Equals(object obj)
		{
			return obj is FVector other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = X.GetHashCode();
				hashCode = (hashCode * 397) ^ Y.GetHashCode();
				hashCode = (hashCode * 397) ^ Z.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(FVector lhs, FVector rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(FVector lhs, FVector rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			return $"X={X} Y={Y} Z={Z}";
		}

		public static FVector Zero => new FVector(0, 0, 0);
		public static FVector One => new FVector(1, 1, 1);
	}
}

