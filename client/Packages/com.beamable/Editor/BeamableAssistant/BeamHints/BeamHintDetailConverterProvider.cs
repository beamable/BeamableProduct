using Beamable.Common;
using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beamable.Editor.Assistant
{
	public static class BeamHintDetailConverterProvider
	{
		public delegate void DefaultConverterSignature(in BeamHint hint, in BeamHintDetailsConfig config, BeamHintVisualsInjectionBag injectionBag);

		// TODO: Replace with localization library so we can edit this without the recompile and support multiple languages.
		private static Dictionary<string, string> IdToHintDetailIntro =  new Dictionary<string, string> {
			{
				BeamHintIds.ID_CLIENT_CALLABLE_UNSUPPORTED_PARAMETERS, 
				"The ClientCallables below have unsupported parameters. We don't support client callables with Delegate, Task or Promise parameters." +
				"\n\nWe don't support it since it's impossible to serialize these types over to the microservice as they are not really data types (function-pointer-like types)." +
				"\n\nThe following ClientCallables have unsupported parameters:\n\n"
			},
			{
				BeamHintIds.ID_CLIENT_CALLABLE_ASYNC_VOID,
				"ClientCallables with \"async void\" signatures are not Awaitable." +
				"\n\nThis means the Microservice will treat the call as a fire-and-forget and will return a 200 response to the client calling the method provided it doesn't throw any exception during the first part of its execution." +
				"\n\nThe following ClientCallables have \"async void\" signatures:\n\n"
			},
			
			{
				BeamHintIds.ID_MICROSERVICE_ATTRIBUTE_MISSING,
				"When creating a Microservices, you must both subclass the Microservice class as well as add a MicroserviceAttribute with a unique name to it." +
				"\n\nThis allows our systems to correctly identify it and parse its internal attributes in a more performant way." +
				"\nThe following subclassed types have attributes missing:\n\n"
			},
			
			{
				BeamHintIds.ID_MICROSERVICE_NAME_COLLISION,
				"When using Microservices, please make sure that they are all uniquely named across all assemblies in your project."
			},
		};
		
		private static Dictionary<string, Func<AttributeValidationResult, string>> IdToAttributeValidationParsing =  new Dictionary<string, Func<AttributeValidationResult, string>> {
			{
				BeamHintIds.ID_CLIENT_CALLABLE_UNSUPPORTED_PARAMETERS,
				attr => $"{attr.Pair.Info.DeclaringType.Name}.{attr.Pair.Info.Name} => {attr.Message}"
			},
			{
				BeamHintIds.ID_CLIENT_CALLABLE_ASYNC_VOID,
				attr => $"{attr.Pair.Info.DeclaringType.FullName}.{attr.Pair.Info.Name}"
			},
			
			{
				BeamHintIds.ID_MICROSERVICE_ATTRIBUTE_MISSING,
				attr => $"{attr.Pair.Info.ReflectedType.FullName}"
			},
			
			{
				BeamHintIds.ID_MICROSERVICE_NAME_COLLISION,
				attr => $"{attr.Pair.Info.ReflectedType.FullName}"
			},
		};

		[BeamHintDetailConverter("Packages/com.beamable/Editor/BeamableAssistant/BeamHints/BeamHintDetailConfigs/HintDetailsAttributeValidationResultConfig.asset", typeof(DefaultConverterSignature))]
		public static void AttributeValidationConverter(in BeamHint hint, in BeamHintDetailsConfig config, BeamHintVisualsInjectionBag injectionBag)
		{
			var hintId = hint.Header.Id;
			var ctx = hint.ContextObject as IEnumerable<AttributeValidationResult>;

			var validationIntro = IdToHintDetailIntro[hintId];
			
			var validationMsg = new StringBuilder();
			foreach (var attributeValidationResult in ctx)
			{
				validationMsg.AppendLine(IdToAttributeValidationParsing[hintId].Invoke(attributeValidationResult));
			}

			injectionBag.SetLabel(validationIntro + validationMsg, "hintText");
			//injectionBag.SetLabel(validationMsg.ToString(), "hintText");
			injectionBag.SetLabelClicked(() => BeamableLogger.Log("THE ASSISTANT IIISSS ALLIVEEEE!!!!!"), "hintText");
		}
	} 
}
