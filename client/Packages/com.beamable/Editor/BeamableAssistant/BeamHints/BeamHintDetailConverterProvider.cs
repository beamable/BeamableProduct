using Beamable.Common;
using Beamable.Editor;
using Beamable.Editor.BeamableAssistant.Components;
using Common.Runtime.BeamHints;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.BeamableAssistant.BeamHints
{
	public static class BeamHintDetailConverterProvider
	{
		public delegate void DefaultConverterSignature(in BeamHint hint, in BeamHintDetailsConfig config, BeamHintVisualsInjectionBag injectionBag);  
		
		
		[BeamHintDetailConverter("Packages/com.beamable/Editor/BeamableAssistant/BeamHints/BeamHintDetailConfigs/BeamHintDetailsConfig2.asset", typeof(DefaultConverterSignature))]
		public static void SingleTextConverter(in BeamHint hint, in BeamHintDetailsConfig config, BeamHintVisualsInjectionBag injectionBag)
		{
			injectionBag.SetLabel(hint.Header.Id, "hintText");
			injectionBag.SetLabelClicked(() => BeamableLogger.Log("THE ASSISTANT IIISSS ALLIVEEEE!!!!!"), "hintText");
		}
		
		[BeamHintDetailConverter("Packages/com.beamable/Editor/BeamableAssistant/BeamHints/BeamHintDetailConfigs/BeamHintDetailsConfig.asset", typeof(DefaultConverterSignature))]
		public static void HueHueHue(in BeamHint hint, in BeamHintDetailsConfig config)
		{
		} 
		 
		
	}
}
