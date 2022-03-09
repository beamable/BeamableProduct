using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_2018
using UnityEngine.Experimental.Input.UI;
#else
using UnityEngine.InputSystem.UI;
#endif
#endif

namespace Beamable.InputManagerIntegration
{
	public static class BeamableInput
	{
		public static bool IsActionTriggered(InputActionArg arg)
		{
			return arg?.IsTriggered() ?? false;
		}

		public static void AddInputSystem()
		{
			
			var eventSystem = new GameObject("EventSystem");
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			eventSystem.AddComponent<InputSystemUIInputModule>();
#else
				eventSystem.AddComponent<StandaloneInputModule>();
#endif
			eventSystem.AddComponent<EventSystem>();
		}
	}
}
