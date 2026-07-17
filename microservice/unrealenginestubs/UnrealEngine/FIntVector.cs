using System;
using Newtonsoft.Json;

namespace UnrealEngine
{
	/// <summary>
	///   <para>A vector in 3-D space composed of components (X, Y, Z) with integer precision.</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	[Serializable]
	public struct FIntVector : IEquatable<FIntVector>
	{
		/// <summary>
		///   <para>Vector's X component.</para>
		/// </summary>
		public int X;

		/// <summary>
		///   <para>Vector's Y component.</para>
		/// </summary>
		public int Y;

		/// <summary>
		///   <para>Vector's Z component.</para>
		/// </summary>
		public int Z;

		public FIntVector(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public bool Equals(FIntVector other)
		{
			return X == other.X && Y == other.Y && Z == other.Z;
		}

		public override bool Equals(object obj)
		{
			return obj is FIntVector other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = X;
				hashCode = (hashCode * 397) ^ Y;
				hashCode = (hashCode * 397) ^ Z;
				return hashCode;
			}
		}

		public static bool operator ==(FIntVector lhs, FIntVector rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(FIntVector lhs, FIntVector rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			return $"X={X} Y={Y} Z={Z}";
		}

		public static FIntVector Zero => new FIntVector(0, 0, 0);
		public static FIntVector One => new FIntVector(1, 1, 1);
	}
}
