using System;
using Newtonsoft.Json;

namespace UnrealEngine
{
	/// <summary>
	///   <para>A single gameplay tag, which represents a hierarchical name of the form x.y that is registered in the GameplayTagsManager.</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public struct FGameplayTag : IEquatable<FGameplayTag>
	{
		/// <summary>
		///   <para>The name of this tag.</para>
		/// </summary>
		public string TagName;

		public FGameplayTag(string tagName)
		{
			TagName = tagName ?? string.Empty;
		}

		public bool Equals(FGameplayTag other)
		{
			return string.Equals(TagName, other.TagName, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is FGameplayTag other && Equals(other);
		}

		public override int GetHashCode()
		{
			return TagName != null ? TagName.GetHashCode() : 0;
		}

		public static bool operator ==(FGameplayTag lhs, FGameplayTag rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(FGameplayTag lhs, FGameplayTag rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			return TagName ?? string.Empty;
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(TagName);
		}

		public static FGameplayTag Empty => new FGameplayTag(string.Empty);
	}
}
