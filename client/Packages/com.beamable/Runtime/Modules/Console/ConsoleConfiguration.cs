using Beamable.InputManagerIntegration;
using UnityEngine;

namespace Beamable.Console
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu]
#endif
	public class ConsoleConfiguration : ModuleConfigurationObject
	{
		public static ConsoleConfiguration Instance => Get<ConsoleConfiguration>();
		
		public bool EnableAdminConsole = true;
		public bool ForceEnabled = true;

#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
		public InputActionArg ToggleAction = new InputActionArg
		{
			KeyCode = KeyCode.BackQuote
		};
#elif ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
      public InputActionArg ToggleAction;
#endif
		
	}
}
