using Beamable.Common;
using Beamable.Editor.BeamableAssistant.Components;
using Common.Runtime.BeamHints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Editor.BeamableAssistant.BeamHints
{
	[AttributeUsage(AttributeTargets.Method)]
	public class BeamHintDetailConverterAttribute : Attribute, IReflectionCachingAttribute<BeamHintDetailConverterAttribute>
	{
		private static readonly List<SignatureOfInterest> AcceptedSignatures = new List<SignatureOfInterest>()
		{
			new SignatureOfInterest(
				true,
				typeof(void),
				new[]
				{
					new ParameterOfInterest(typeof(BeamHint).MakeByRefType(), true, false, false), 
					new ParameterOfInterest(typeof(BeamHintDetailsConfig).MakeByRefType(), true, false, false),
					new ParameterOfInterest(typeof(BeamHintVisualsInjectionBag), false, false, false)
				})
		};

		public readonly string PathToBeamHintDetailConfig;
		public readonly string UserOverridePathToHintDetailConfig;
		public readonly Type DelegateType;

		public BeamHintDetailConverterAttribute(string pathToBeamHintDetailConfig, Type delegateType, string userOverridePathToHintDetailConfig = null)
		{
			PathToBeamHintDetailConfig = pathToBeamHintDetailConfig;
			DelegateType = delegateType;
			UserOverridePathToHintDetailConfig = userOverridePathToHintDetailConfig;
		}

		public AttributeValidationResult<BeamHintDetailConverterAttribute> IsAllowedOnMember(MemberInfo member)
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

				return new AttributeValidationResult<BeamHintDetailConverterAttribute>(this,
				                                                                       member,
				                                                                       ReflectionCache.ValidationResultType.Error,
				                                                                       message.ToString());
			}

			return new AttributeValidationResult<BeamHintDetailConverterAttribute>(this, member, ReflectionCache.ValidationResultType.Valid, $"");
		}
	}
}
