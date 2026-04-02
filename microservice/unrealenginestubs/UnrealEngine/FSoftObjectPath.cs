using System;
using Newtonsoft.Json;

namespace UnrealEngine
{
	/// <summary>
	///   <para>A struct that contains a string reference to an object, either a top level asset or a subobject.</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	[Serializable]
	public struct FSoftObjectPath : IEquatable<FSoftObjectPath>
	{
		/// <summary>
		///   <para>Asset path, path to a top level object in a package.</para>
		/// </summary>
		public string AssetPathName;

		/// <summary>
		///   <para>Optional FString for subobject within an asset.</para>
		/// </summary>
		public string SubPathString;

		public FSoftObjectPath(string assetPathName, string subPathString = null)
		{
			AssetPathName = assetPathName ?? string.Empty;
			SubPathString = subPathString ?? string.Empty;
		}

		public FSoftObjectPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				AssetPathName = string.Empty;
				SubPathString = string.Empty;
			}
			else
			{
				// Parse path in format: AssetPath:SubPath or just AssetPath
				var parts = path.Split(new[] { ':' }, 2);
				AssetPathName = parts[0];
				SubPathString = parts.Length > 1 ? parts[1] : string.Empty;
			}
		}

		public bool Equals(FSoftObjectPath other)
		{
			return string.Equals(AssetPathName, other.AssetPathName, StringComparison.Ordinal) &&
			       string.Equals(SubPathString, other.SubPathString, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is FSoftObjectPath other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = AssetPathName != null ? AssetPathName.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (SubPathString != null ? SubPathString.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(FSoftObjectPath lhs, FSoftObjectPath rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(FSoftObjectPath lhs, FSoftObjectPath rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(SubPathString))
			{
				return AssetPathName ?? string.Empty;
			}

			return $"{AssetPathName}:{SubPathString}";
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(AssetPathName);
		}

		public bool IsNull()
		{
			return string.IsNullOrEmpty(AssetPathName);
		}

		public static FSoftObjectPath Empty => new FSoftObjectPath(string.Empty);
	}
}
