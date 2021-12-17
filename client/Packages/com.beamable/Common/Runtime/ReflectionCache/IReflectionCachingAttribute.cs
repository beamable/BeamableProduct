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
	/// <typeparam name="T">The type of the attribute implementing this interface.</typeparam>
	public interface IReflectionCachingAttribute<T> where T : Attribute, IReflectionCachingAttribute<T>
	{
		/// <summary>
		/// Takes in the <see cref="MemberInfo"/> associated with this attribute and returns a <see cref="AttributeValidationResult{T}"/>.
		/// </summary>
		AttributeValidationResult<T> IsAllowedOnMember(MemberInfo member);
	}

	/// <summary>
	/// Result of a data structure that holds the result of a validation performed by <see cref="IReflectionCachingAttribute{T}"/>. 
	/// </summary>
	/// <typeparam name="T">The type of the attribute implementing the <see cref="IReflectionCachingAttribute{T}"/> interface.</typeparam>
	public readonly struct AttributeValidationResult<T> where T : Attribute, IReflectionCachingAttribute<T>
	{
		public readonly MemberAttributePair Pair;
		public readonly ReflectionCache.ValidationResultType Type;
		public readonly string Message;

		public AttributeValidationResult(T attribute, MemberInfo ownerMember, ReflectionCache.ValidationResultType type, string message)
		{
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
		public static List<AttributeValidationResult<T>> Validate<T>(this IReadOnlyList<MemberAttributePair> cachedMemberAttributePairs)
			where T : Attribute, IReflectionCachingAttribute<T>
		{
			return cachedMemberAttributePairs
			       .Select(pair => ((T)pair.Attribute).IsAllowedOnMember(pair.Info))
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
		/// <typeparam name="T">Any <see cref="Attribute"/> implementing <see cref="IReflectionCachingAttribute{T}"/> so that we can validate its existence and correct usage.</typeparam>
		/// <returns>A list of <see cref="AttributeValidationResult{T}"/> that can be used to display error/warnings or parse valid results.</returns>
		public static List<AttributeValidationResult<T>> GetAndValidateAttributeExistence<T>(this IEnumerable<MemberInfo> membersToCheck,
		                                                                                     AttributeOfInterest attributeOfInterest,
		                                                                                     Func<MemberInfo, AttributeValidationResult<T>> validateOnMissing)
			where T : Attribute, IReflectionCachingAttribute<T>
		{
			System.Diagnostics.Debug.Assert(attributeOfInterest.AttributeType == typeof(T), "This function's generic type must match the attribute of interest you are passing in.");

			var members = membersToCheck;
			var validationResults = new List<AttributeValidationResult<T>>();
			foreach (var checkMember in members)
			{
				var attribute = checkMember.GetCustomAttribute(attributeOfInterest.AttributeType, false);
				if (attribute != null)
				{
					var cast = (T)attribute;
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
		public static List<AttributeValidationResult<TAttribute>> GetOptionalAttributeInMembers<TAttribute>(this IEnumerable<MemberInfo> membersToCheck)
			where TAttribute : Attribute, IReflectionCachingAttribute<TAttribute>
		{
			return membersToCheck.GetAndValidateAttributeExistence(
				new AttributeOfInterest(typeof(TAttribute)),
				info => new AttributeValidationResult<TAttribute>(null, info, ReflectionCache.ValidationResultType.Discarded, ""));
		}

		/// <summary>
		/// Helper that splits a list of <see cref="AttributeValidationResult{T}"/> into it's three lists (for each possible <see cref="ReflectionCache.ValidationResultType"/>).
		/// Caller can use the resulting lists can be used to process valid attributes or display context-sensitive error/warning messages. 
		/// </summary>        
		public static void SplitValidationResults<T>(this IReadOnlyList<AttributeValidationResult<T>> mainList,
		                                             out List<AttributeValidationResult<T>> valid,
		                                             out List<AttributeValidationResult<T>> warning,
		                                             out List<AttributeValidationResult<T>> error)
			where T : Attribute, IReflectionCachingAttribute<T>
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

		/// <summary>
		/// Helper that standardizes how we log <see cref="AttributeValidationResult{T}"/>.
		/// </summary>        
		/// <param name="messageIntro">A context-sensitive intro message to make the string easier to understand for a reader.</param>
		/// <param name="messageOutro">A context-sensitive outro message to make the string easier to understand for a reader.</param>
		/// <returns>A formatted string ready to be displayed containing each validation result's relevant information.</returns>
		public static string GeneratePerAttributeValidationMessage<T>(this IReadOnlyList<AttributeValidationResult<T>> listOfErrors,
		                                                              string messageIntro = "",
		                                                              string messageOutro = "")
			where T : Attribute, IReflectionCachingAttribute<T>
		{
			var validationResultsMessage = new StringBuilder();
			validationResultsMessage.Append(messageIntro);
			validationResultsMessage.AppendLine($"The following [{typeof(T).Name}]s are not valid:");
			foreach (var failedAllowedValidationType in listOfErrors)
			{
				var type = failedAllowedValidationType.Pair.Info;
				var validationResult = failedAllowedValidationType.Type;
				var message = failedAllowedValidationType.Message;

				if (type.MemberType == MemberTypes.TypeInfo || type.MemberType == MemberTypes.NestedType)
					validationResultsMessage.AppendLine($"{type.Name} | {validationResult} => {message}");
				else
					validationResultsMessage.AppendLine($"{type.ReflectedType?.Name}.{type.Name} | {validationResult} => {message}");
			}

			validationResultsMessage.AppendLine(messageOutro);
			return validationResultsMessage.ToString();
		}
	}
}
