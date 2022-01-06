using Beamable.Common;
using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Editor.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beamable.Editor.Assistant
{
	public static class BeamHintDetailConverterProvider
	{
		
		/// <summary>
		/// Converter to handle cases where other <see cref="BeamHintDetailConverterAttribute"/> fail their validations. It also handles <see cref="AttributeValidationResults"/>,
		/// but in a way that guarantees that the converter function matches one of the accepted signatures.
		/// </summary>
		[BeamHintDetailConverter(typeof(BeamHintReflectionCache.DefaultConverter),
		                         BeamHintType.Validation, "", "MisconfiguredHintDetailsProvider",
		                         "HintDetailsAttributeValidationResultConfig")]
		public static void MisconfiguredHintDetailsAttributeConverter(in BeamHint hint, in BeamHintTextMap textMap, BeamHintVisualsInjectionBag injectionBag)
		{
			var hintId = hint.Header.Id;
			var ctx = hint.ContextObject as IEnumerable<AttributeValidationResult>;

			var validationIntro = textMap != null && textMap.TryGetHintIntroText(hint.Header, out var intro) ? intro : hint.Header.Id;

			var validationMsg = new StringBuilder();
			foreach (var attr in ctx)
			{
				var line = $"{attr.Pair.Info.DeclaringType.FullName}.{attr.Pair.Info.Name}";
				validationMsg.AppendLine(line);
			}

			injectionBag.SetLabel(validationIntro + validationMsg, "hintText");
		}
		
		
		/// <summary>
		/// Converter that handles <see cref="AttributeValidationResult"/>s as context object and displays a single Label text message.
		/// </summary>
		[BeamHintDetailConverter(typeof(BeamHintReflectionCache.DefaultConverter),
		                         BeamHintType.Validation | BeamHintType.Hint, "", BeamHintIds.ATTRIBUTE_VALIDATION_ID_PREFIX,
		                         "HintDetailsAttributeValidationResultConfig")]
		public static void AttributeValidationConverter(in BeamHint hint, in BeamHintTextMap textMap, BeamHintVisualsInjectionBag injectionBag)
		{
			var hintId = hint.Header.Id;
			var ctx = hint.ContextObject as IEnumerable<AttributeValidationResult>;

			var validationIntro = textMap != null && textMap.TryGetHintIntroText(hint.Header, out var intro) ? intro : hint.Header.Id;

			var validationMsg = new StringBuilder();
			foreach (var attr in ctx)
			{
				string line;

				if (hintId == BeamHintIds.ID_CLIENT_CALLABLE_UNSUPPORTED_PARAMETERS) { line = $"{attr.Pair.Info.DeclaringType.Name}.{attr.Pair.Info.Name} => {attr.Message}"; }
				else if (hintId == BeamHintIds.ID_CLIENT_CALLABLE_ASYNC_VOID) { line = $"{attr.Pair.Info.DeclaringType.FullName}.{attr.Pair.Info.Name}"; }
				else if (hintId == BeamHintIds.ID_MICROSERVICE_ATTRIBUTE_MISSING) { line = $"{attr.Pair.Info.Name}"; }
				else if (hintId == BeamHintIds.ID_MISCONFIGURED_HINT_DETAILS_PROVIDER) { line = $"{attr.Pair.Info.DeclaringType.FullName}.{attr.Pair.Info.Name}"; }
				else { line = $"{attr.Pair.Info.ReflectedType.FullName}"; }

				validationMsg.AppendLine(line);
			}

			injectionBag.SetLabel(validationIntro + validationMsg, "hintText");
		}
		
		
		/// <summary>
		/// Converter that handles <see cref="UniqueNameCollisionData"/>s as context object and displays a single Label text message.
		/// </summary>
		[BeamHintDetailConverter(typeof(BeamHintReflectionCache.DefaultConverter),
		                         BeamHintType.Validation | BeamHintType.Hint, "", BeamHintIds.ATTRIBUTE_NAME_COLLISION_ID_PREFIX,
		                         "HintDetailsAttributeValidationResultConfig")]
		public static void UniqueNameAttributeValidationConverter(in BeamHint hint, in BeamHintTextMap textMap, BeamHintVisualsInjectionBag injectionBag)
		{
			var hintId = hint.Header.Id;
			var ctx = hint.ContextObject as IEnumerable<UniqueNameCollisionData>;

			var validationIntro = textMap != null && textMap.TryGetHintIntroText(hint.Header, out var intro) ? intro : hint.Header.Id;

			var validationMsg = new StringBuilder();
			foreach (var collisionData in ctx)
			{
				var line = $"{collisionData.Name} => {string.Join(", ", collisionData.CollidedAttributes.Select(pair => pair.Info.Name))}";
				validationMsg.AppendLine(line);
			}

			injectionBag.SetLabel(validationIntro + validationMsg, "hintText");
		}
	}
}
