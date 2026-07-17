using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace UnrealEngine
{
	/// <summary>
	///   <para>A container for gameplay tags. Optimized for fast queries.</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	[Serializable]
	public struct FGameplayTagContainer : IEquatable<FGameplayTagContainer>
	{
		/// <summary>
		///   <para>Array of gameplay tags.</para>
		/// </summary>
		public List<FGameplayTag> GameplayTags;

		public FGameplayTagContainer(List<FGameplayTag> tags)
		{
			GameplayTags = tags ?? new List<FGameplayTag>();
		}

		public FGameplayTagContainer(params FGameplayTag[] tags)
		{
			GameplayTags = new List<FGameplayTag>(tags);
		}

		public void AddTag(FGameplayTag tag)
		{
			if (GameplayTags == null)
			{
				GameplayTags = new List<FGameplayTag>();
			}

			if (!GameplayTags.Contains(tag))
			{
				GameplayTags.Add(tag);
			}
		}

		public void RemoveTag(FGameplayTag tag)
		{
			if (GameplayTags != null)
			{
				GameplayTags.Remove(tag);
			}
		}

		public bool HasTag(FGameplayTag tag)
		{
			return GameplayTags != null && GameplayTags.Contains(tag);
		}

		public bool HasAny(FGameplayTagContainer other)
		{
			if (GameplayTags == null || other.GameplayTags == null)
			{
				return false;
			}

			return GameplayTags.Any(tag => other.GameplayTags.Contains(tag));
		}

		public bool HasAll(FGameplayTagContainer other)
		{
			if (other.GameplayTags == null || other.GameplayTags.Count == 0)
			{
				return true;
			}

			if (GameplayTags == null)
			{
				return false;
			}

			List<FGameplayTag> tags = GameplayTags;
			return other.GameplayTags.All(tag => tags.Contains(tag));
		}

		public int Count => GameplayTags?.Count ?? 0;

		public bool IsEmpty => GameplayTags == null || GameplayTags.Count == 0;

		public bool Equals(FGameplayTagContainer other)
		{
			if (GameplayTags == null && other.GameplayTags == null)
			{
				return true;
			}

			if (GameplayTags == null || other.GameplayTags == null)
			{
				return false;
			}

			if (GameplayTags.Count != other.GameplayTags.Count)
			{
				return false;
			}

			return GameplayTags.All(tag => other.GameplayTags.Contains(tag));
		}

		public override bool Equals(object obj)
		{
			return obj is FGameplayTagContainer other && Equals(other);
		}

		public override int GetHashCode()
		{
			if (GameplayTags == null)
			{
				return 0;
			}

			unchecked
			{
				int hash = 17;
				foreach (var tag in GameplayTags.OrderBy(t => t.TagName))
				{
					hash = hash * 31 + tag.GetHashCode();
				}
				return hash;
			}
		}

		public static bool operator ==(FGameplayTagContainer lhs, FGameplayTagContainer rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(FGameplayTagContainer lhs, FGameplayTagContainer rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override string ToString()
		{
			if (GameplayTags == null || GameplayTags.Count == 0)
			{
				return "()";
			}

			var tags = new List<string>();
			foreach (var tag in GameplayTags)
			{
				tags.Add(tag.ToString());
			}
			return $"({string.Join(", ", tags)})";
		}

		public static FGameplayTagContainer Empty => new FGameplayTagContainer(new List<FGameplayTag>());
	}
}
