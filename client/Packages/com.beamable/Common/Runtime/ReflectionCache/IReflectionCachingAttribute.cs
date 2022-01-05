using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Beamable.Common.Reflection
{
	/// <summary>
	/// Declare this interface over attributes to use our validation utilities declared in <see cref="ReflectionCache"/>.
	/// </summary>
	public interface IReflectionCachingAttribute
	{
		/// <summary>
		/// Takes in the <see cref="MemberInfo"/> associated with this attribute and returns a <see cref="AttributeValidationResult{T}"/>.
		/// </summary>
		AttributeValidationResult IsAllowedOnMember(MemberInfo member);
	}

	/// <summary>
	/// Result of a data structure that holds the result of a validation performed by <see cref="IReflectionCachingAttribute{T}"/>. 
	/// </summary>
	/// <typeparam name="T">The type of the attribute implementing the <see cref="IReflectionCachingAttribute{T}"/> interface.</typeparam>
	public readonly struct AttributeValidationResult
	{
		public readonly MemberAttributePair Pair;
		public readonly ReflectionCache.ValidationResultType Type;
		public readonly string Message;

		public AttributeValidationResult(Attribute attribute, MemberInfo ownerMember, ReflectionCache.ValidationResultType type, string message)
		{
			System.Diagnostics.Debug.Assert(attribute is IReflectionCachingAttribute, "Attribute must implement the IReflectionCacheAttribute");
			Pair = new MemberAttributePair(ownerMember, attribute);
			Type = type;
			Message = message;
		}
	}

	public static partial class ReflectionCacheExtensions
	{
		/// <summary>
		/// Helper that invokes the validation defined by <see cref="IReflectionCachingAttribute{T}"/> over a list of <see cref="MemberAttributePair"/>
		/// </summary>
		public static List<AttributeValidationResult> Validate(this IReadOnlyList<MemberAttributePair> cachedMemberAttributePairs)
		{
			return cachedMemberAttributePairs
			       .Select(pair => ((IReflectionCachingAttribute)pair.Attribute).IsAllowedOnMember(pair.Info))
			       .ToList();
		}

		/// <summary>
		/// Helper that can be used to fill <paramref name="foundAttributes"/> with all pairs found by parsing <paramref name="membersToCheck"/> looking for <paramref name="attributeOfInterest"/>.
		/// For all members containing the <paramref name="attributeOfInterest"/>, we validate they are over an allowed member via <see cref="IReflectionCachingAttribute{T}.IsAllowedOnMember"/>.
		/// For all members missing the <paramref name="attributeOfInterest"/>, the <paramref name="validateOnMissing"/> function is called to validate it. This allows the caller to decide
		/// whether or not missing attributes are valid and the error message for it. 
		/// </summary>
		/// <param name="membersToCheck">The list <see cref="MemberInfo"/> to check. Usually built from <see cref="Type.GetMembers()"/>.</param>
		/// <param name="attributeOfInterest">The <see cref="AttributeOfInterest"/> that we'll look for over the given <paramref name="membersToCheck"/>.</param>
		/// <param name="validateOnMissing">Defines how the caller wants <paramref name="membersToCheck"/> to be validated when the attribute isn't found over a member.</param>
		/// <param name="foundAttributes">The list containing all found <see cref="MemberAttributePair"/>. Built this way so caller can reuse same list allocation multiple times if they wish to.</param>
		/// <returns>A list of <see cref="AttributeValidationResult{T}"/> that can be used to display error/warnings or parse valid results.</returns>
		public static List<AttributeValidationResult> GetAndValidateAttributeExistence(this IEnumerable<MemberInfo> membersToCheck,
		                                                                                     AttributeOfInterest attributeOfInterest,
		                                                                                     Func<MemberInfo, AttributeValidationResult> validateOnMissing)
		{
			var members = membersToCheck;
			var validationResults = new List<AttributeValidationResult>();
			foreach (var checkMember in members)
			{
				var attribute = checkMember.GetCustomAttribute(attributeOfInterest.AttributeType, false);
				if (attribute != null)
				{
					var cast = (IReflectionCachingAttribute)attribute;
					var result = cast.IsAllowedOnMember(checkMember);

					validationResults.Add(result);
				}
				else
				{
					var result = validateOnMissing?.Invoke(checkMember);
					if (result.HasValue) validationResults.Add(result.Value);
				}
			}

			return validationResults;
		}

		/// <summary>
		/// Given a list of members, fetches all attributes of <typeparamref name="TAttribute"/> type.
		/// </summary>
		/// <param name="membersToCheck">List of members to check.</param>
		/// <typeparam name="TAttribute">The <see cref="IReflectionCachingAttribute{T}"/> to look for in the given members.</typeparam>
		/// <returns>A list of <see cref="AttributeValidationResult{T}"/> with the resulting validations and members.</returns>
		public static List<AttributeValidationResult> GetOptionalAttributeInMembers<TAttribute>(this IEnumerable<MemberInfo> membersToCheck)
		{
			return membersToCheck.GetAndValidateAttributeExistence(
				new AttributeOfInterest(typeof(TAttribute)),
				info => new AttributeValidationResult(null, info, ReflectionCache.ValidationResultType.Discarded, ""));
		}

		/// <summary>
		/// Helper that splits a list of <see cref="AttributeValidationResult{T}"/> into it's three lists (for each possible <see cref="ReflectionCache.ValidationResultType"/>).
		/// Caller can use the resulting lists can be used to process valid attributes or display context-sensitive error/warning messages. 
		/// </summary>        
		public static void SplitValidationResults(this IReadOnlyList<AttributeValidationResult> mainList,
		                                             out List<AttributeValidationResult> valid,
		                                             out List<AttributeValidationResult> warning,
		                                             out List<AttributeValidationResult> error)
		{
			var splitByType = mainList.GroupBy(res => res.Type).ToList();

			valid = splitByType
			        .Where(group => group.Key == ReflectionCache.ValidationResultType.Valid)
			        .SelectMany(group => group)
			        .ToList();

			warning = splitByType
			          .Where(group => group.Key == ReflectionCache.ValidationResultType.Warning)
			          .SelectMany(group => group)
			          .ToList();

			error = splitByType
			        .Where(group => group.Key == ReflectionCache.ValidationResultType.Error)
			        .SelectMany(group => group)
			        .ToList();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="attributePairs"></param>
		/// <returns></returns>
		public static Dictionary<MemberInfo, IGrouping<MemberInfo, MemberAttributePair>> CreateMemberAttributePairOwnerLookupTable(this IEnumerable<MemberAttributePair> attributePairs)
		{
			return attributePairs.GroupBy(memberAttributePair => (MemberInfo)memberAttributePair.Info.DeclaringType)
			                     .ToDictionary(groups => groups.Key);
		}
	}
}
