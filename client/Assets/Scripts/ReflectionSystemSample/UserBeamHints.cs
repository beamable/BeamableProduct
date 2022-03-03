using Beamable.Common.Assistant;
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using Beamable.Editor.Assistant;
using Beamable.Editor.Reflection;
#endif

public class UserBeamHintDomains : BeamHintDomainProvider
{
	[BeamHintDomain] public static readonly string MyUserSystemDomain = BeamHintDomains.GenerateUserDomain("MY_USER_SYSTEM_DOMAIN");
	[BeamHintDomain] public static readonly string MyUserSystemDomain_SubDomain1 = BeamHintDomains.GenerateSubDomain(MyUserSystemDomain, "SUB_DOMAIN");
}

public class UserBeamHintIds : BeamHintIdProvider
{
	[BeamHintId] public static readonly string MyReflectionAttributeInvalidUsage = BeamHintIds.GenerateHintId("MyReflectionAttribute", BeamHintIds.ATTRIBUTE_VALIDATION_ID_PREFIX);
	[BeamHintId] public static readonly string MyReflectionAttributeInterestingCase = BeamHintIds.GenerateHintId("MyReflectionAttributeRelevantInformation", BeamHintIds.ATTRIBUTE_VALIDATION_ID_PREFIX);
	[BeamHintId] public static readonly string MyInterestingBaseTypeMissingAttributesOnMembers = BeamHintIds.GenerateHintId("MyInterestingBaseTypeMissingAttributesOnMembers");
}

#if UNITY_EDITOR
public class UserBeamHintDetailsProvider : BeamHintDetailConverterProvider
{
	/// <summary>
	/// Converter to handle cases where other <see cref="BeamHintDetailConverterAttribute"/> fail their validations. It also handles <see cref="AttributeValidationResults"/>,
	/// but in a way that guarantees that the converter function matches one of the accepted signatures.
	/// </summary>
	[BeamHintDetailConverter(typeof(BeamHintReflectionCache.DefaultConverter),
	                         BeamHintType.Validation, "", "MyInterestingBaseTypeMissingAttributesOnMembers",
	                         "HintDetailsAttributeValidationResultConfig")]
	public static void MyInterestingBaseTypeMissingAttributesOnMembersConverter(in BeamHint hint, in BeamHintTextMap textMap, BeamHintVisualsInjectionBag injectionBag)
	{
		var typesMissing = hint.ContextObject as List<Type>;

		var validationIntro = textMap != null && textMap.TryGetHintIntroText(hint.Header, out var intro) ? intro : hint.Header.Id;

		var validationMsg = new StringBuilder();
		foreach (var type in typesMissing)
		{
			var line = $"{type.FullName}";
			validationMsg.AppendLine(line);
		}

		injectionBag.SetLabel(validationIntro + validationMsg, "hintText");
	}
}
#endif
