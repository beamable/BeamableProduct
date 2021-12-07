using Beamable.Common;
using System;
using System.Reflection;

namespace Common.Runtime.BeamHints
{
	public abstract class BeamHintIdProvider{}

	public class BeamHintIdAttribute : Attribute, IReflectionCachingAttribute<BeamHintIdAttribute>
	{
		public AttributeValidationResult<BeamHintIdAttribute> IsAllowedOnMember(MemberInfo member)
		{
			var field = (FieldInfo)member;
			if (field.FieldType == typeof(string) && field.IsStatic && field.IsInitOnly)
				return new AttributeValidationResult<BeamHintIdAttribute>(this, member, ReflectionCache.ValidationResultType.Valid, $"");

			return new AttributeValidationResult<BeamHintIdAttribute>(this,
			                                                          member,
			                                                          ReflectionCache.ValidationResultType.Error,
			                                                          $"{member.Name} is not \"static readonly string\". It cannot be a BeamHintId.");
		}
	}
}
