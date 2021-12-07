using Beamable.Common;
using System;
using System.Reflection;

namespace Common.Runtime.BeamHints
{
	public abstract class BeamHintDomainProvider{}

	[AttributeUsage(AttributeTargets.Field)]
	public class BeamHintDomainAttribute : Attribute, IReflectionCachingAttribute<BeamHintDomainAttribute>
	{
		public AttributeValidationResult<BeamHintDomainAttribute> IsAllowedOnMember(MemberInfo member)
		{
			var field = (FieldInfo)member;
			if (field.IsStatic && field.IsInitOnly)
				return new AttributeValidationResult<BeamHintDomainAttribute>(this, member, ReflectionCache.ValidationResultType.Valid, $"");

			return new AttributeValidationResult<BeamHintDomainAttribute>(this,
			                                                              member,
			                                                              ReflectionCache.ValidationResultType.Error,
			                                                              $"{member.Name} is not \"static readonly string\". It cannot be a BeamHintDomain.");
		}
	}
}
