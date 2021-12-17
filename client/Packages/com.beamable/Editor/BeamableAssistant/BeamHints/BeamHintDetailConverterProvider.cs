using Beamable.Common;
using Beamable.Common.Assistant;

namespace Beamable.Editor.Assistant
{
	public static class BeamHintDetailConverterProvider
	{
		public delegate void DefaultConverterSignature(in BeamHint hint, in BeamHintDetailsConfig config, BeamHintVisualsInjectionBag injectionBag);

		[BeamHintDetailConverter("Packages/com.beamable/Editor/BeamableAssistant/BeamHints/BeamHintDetailConfigs/HintDetailsMultiTextConfig.asset", typeof(DefaultConverterSignature))]
		public static void SingleTextConverter(in BeamHint hint, in BeamHintDetailsConfig config, BeamHintVisualsInjectionBag injectionBag)
		{
			injectionBag.SetLabel(hint.Header.Id, "hintText");
			injectionBag.SetLabelClicked(() => BeamableLogger.Log("THE ASSISTANT IIISSS ALLIVEEEE!!!!!"), "hintText");
		}

		[BeamHintDetailConverter("Packages/com.beamable/Editor/BeamableAssistant/BeamHints/BeamHintDetailConfigs/HintDetailsMultiTextConfig.asset", typeof(DefaultConverterSignature))]
		public static void HueHueHue(in BeamHint hint, in BeamHintDetailsConfig config) { }
	}
}
