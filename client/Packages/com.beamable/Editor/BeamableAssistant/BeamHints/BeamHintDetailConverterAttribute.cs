using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Beamable.Editor.Assistant
{
	[AttributeUsage(AttributeTargets.Method)]
	public class BeamHintDetailConverterAttribute : Attribute, IReflectionCachingAttribute
	{
		private static readonly List<SignatureOfInterest> AcceptedSignatures = new List<SignatureOfInterest>() {
			new SignatureOfInterest(
				true,
				typeof(void),
				new[] {
					new ParameterOfInterest(typeof(BeamHint).MakeByRefType(), true, false, false),
					new ParameterOfInterest(typeof(BeamHintTextMap).MakeByRefType(), true, false, false),
					new ParameterOfInterest(typeof(BeamHintVisualsInjectionBag), false, false, false)
				})
		};

		public readonly BeamHintType MatchType;
		public readonly string Domain;
		public readonly string IdRegex;
		
		public readonly string HintDetailConfigId;
		public readonly string UserOverrideToHintDetailConfigId;
		public readonly Type DelegateType;

		public BeamHintDetailConverterAttribute(Type delegateType, BeamHintType matchType, string domain, string idRegex, string hintDetailConfigId, string userOverrideToHintDetailConfigId = null)
		{
			HintDetailConfigId = hintDetailConfigId;
			DelegateType = delegateType;
			MatchType = matchType;
			Domain = domain;
			IdRegex = idRegex;
			UserOverrideToHintDetailConfigId = userOverrideToHintDetailConfigId;
		}

		public AttributeValidationResult IsAllowedOnMember(MemberInfo member)
		{
			var methodInfo = (MethodInfo)member;
			var signatureOfInterests = AcceptedSignatures;

			var matchingMethodSignaturesIndices = signatureOfInterests.FindMatchingMethodSignatures(methodInfo);
			var matchedNoSignatures = matchingMethodSignaturesIndices.TrueForAll(i => i == -1);

			if (matchedNoSignatures)
			{
				var message = new StringBuilder();
				message.AppendLine($"Signatures must match one of the following:");
				message.Append(string.Join("\n", signatureOfInterests.Select(acceptedSignature => acceptedSignature.ToHumanReadableSignature())));

				return new AttributeValidationResult(this,
				                                                                       member,
				                                                                       ReflectionCache.ValidationResultType.Error,
				                                                                       message.ToString());
			}

			return new AttributeValidationResult(this, member, ReflectionCache.ValidationResultType.Valid, $"");
		}
	}
}
