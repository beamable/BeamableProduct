using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Compilation;
#endif

namespace Beamable
{
	[CreateAssetMenu(
		fileName = "CoreConfiguration",
		menuName = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
				   "Core Configuration")]
	public class CoreConfiguration : ModuleConfigurationObject
	{
		public const string PROJECT_EDITOR_REFLECTION_SYSTEM_PATH = "Assets/Beamable/Editor/ReflectionCache/UserSystems";
		public const string BEAMABLE_EDITOR_REFLECTION_SYSTEM_PATH = "Packages/com.beamable/Editor/ReflectionCache/UserSystems";
		public const string BEAMABLE_EDITOR_SERVER_REFLECTION_SYSTEM_PATH = "Packages/com.beamable.server/Editor/ReflectionCache/UserSystems";

		public const string PROJECT_ASSISTANT_MENU_ITEM_PATH = "Assets/Beamable/Editor/Assistant/MenuItems";
		public const string BEAMABLE_ASSISTANT_MENU_ITEM_PATH = "Packages/com.beamable/Editor/BeamableAssistant/MenuItems";
		public const string BEAMABLE_SERVER_ASSISTANT_MENU_ITEM_PATH = "Packages/com.beamable.server/Editor/BeamableAssistant/MenuItems";

		public const string PROJECT_ASSISTANT_TOOLBAR_BUTTON_PATH = "Assets/Beamable/Editor/Assistant/ToolbarButtons";
		public const string BEAMABLE_ASSISTANT_TOOLBAR_BUTTON_PATH = "Packages/com.beamable/Editor/BeamableAssistant/ToolbarButtons";
		public const string BEAMABLE_SERVER_ASSISTANT_TOOLBAR_BUTTON_PATH = "Packages/com.beamable.server/Editor/BeamableAssistant/ToolbarButtons";

		public const string PROJECT_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH = "Assets/Beamable/Editor/Assistant/Hint/HintDetails";
		public const string BEAMABLE_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH = "Packages/com.beamable/Editor/BeamableAssistant/BeamHints/BeamHintDetailConfigs";
		public const string BEAMABLE_SERVER_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH = "Packages/com.beamable.server/Editor/BeamableAssistant/BeamHints/BeamHintDetailConfigs";

		public enum OfflineStrategy { Optimistic, Disable }

		public static CoreConfiguration Instance => Get<CoreConfiguration>();

		public enum EventHandlerConfig { Guarantee, Replace, Add, }

		[Tooltip("By default, Beamable gives you a default uncaught promise exception handler.\n\n" +
				 "You can set your own via PromiseBase.SetPotentialUncaughtErrorHandler.\n\n" +
				 "In Beamable's Initialization, we guarantee, replace or add our default handler based on this configuration.\n\n" +
				 "- Guarantee => Guarantees at least Beamable's default handler will exist if you don't configure one yourself\n" +
				 "- Replace => Replaces any handler configured with Beamable's default handler.\n" +
				 "- Add => Adds Beamable's default handler to the list of handlers, but keep existing handlers configured.\n")]
		public EventHandlerConfig DefaultUncaughtPromiseHandlerConfiguration;


		[Tooltip("By default, when your player isn't connected to the internet, Beamable will accrue inventory writes " +
				 "in a buffer and optimistically simulate the effects locally in memory. When your player comes back " +
				 "online, the buffer will be replayed. If this isn't desirable, you should disable the feature.")]
		public OfflineStrategy InventoryOfflineMode = OfflineStrategy.Optimistic;

		[Header("Beamable Assistant")]
		[Tooltip("Enable this to receive a warning (toggle-able per Beam Hint Validation) when entering playmode.\n\n" +
				 "This aims to help you enforce project workflows and guarantee people are not wasting time chasing issues that we can identify for you.")]
		public bool EnablePlayModeWarning;
		[Tooltip("Register all paths in which we'll need to look for BeamableAssistantMenuItem assets.\n" +
				 "We don't look everywhere as that could impact editor experience on larger projects.")]
		public List<string> BeamableAssistantMenuItemsPath = new List<string>();
		[Tooltip("Register all paths in which we'll need to look for BeamableToolbarButton assets.\n" +
				 "We don't look everywhere as that could impact editor experience on larger projects.")]
		public List<string> BeamableAssistantToolbarButtonsPaths = new List<string>();
		[Tooltip("Register all paths in which we'll need to look for BeamableToolbarButton assets.\n" +
				 "We don't look everywhere as that could impact editor experience on larger projects.")]
		public List<string> BeamableAssistantHintDetailConfigPaths = new List<string>();


		[Header("Reflection Systems")]
		[Tooltip("Register all paths in which we'll need to look for ReflectionSystemObjects.\n" +
				 "We don't look everywhere as that could impact editor experience on larger projects.")]
		public List<string> ReflectionSystemPaths = new List<string>();

		[Tooltip("Register any assemblies you wish to ignore from the assembly sweep.")]
		public List<string> AssembliesToSweep = new List<string>();

		public void OnValidate()
		{
			// Ensure default paths exist for Reflection Cache User System Objects
			if (!ReflectionSystemPaths.Contains(PROJECT_EDITOR_REFLECTION_SYSTEM_PATH))
				ReflectionSystemPaths.Add(PROJECT_EDITOR_REFLECTION_SYSTEM_PATH);

			if (!ReflectionSystemPaths.Contains(BEAMABLE_EDITOR_REFLECTION_SYSTEM_PATH))
				ReflectionSystemPaths.Add(BEAMABLE_EDITOR_REFLECTION_SYSTEM_PATH);

			if (!ReflectionSystemPaths.Contains(BEAMABLE_EDITOR_SERVER_REFLECTION_SYSTEM_PATH))
				ReflectionSystemPaths.Add(BEAMABLE_EDITOR_SERVER_REFLECTION_SYSTEM_PATH);


			// Ensure default paths exist for Beamable Assistant Menu Items
			if (!BeamableAssistantMenuItemsPath.Contains(PROJECT_ASSISTANT_MENU_ITEM_PATH))
				BeamableAssistantMenuItemsPath.Add(PROJECT_ASSISTANT_MENU_ITEM_PATH);

			if (!BeamableAssistantMenuItemsPath.Contains(BEAMABLE_ASSISTANT_MENU_ITEM_PATH))
				BeamableAssistantMenuItemsPath.Add(BEAMABLE_ASSISTANT_MENU_ITEM_PATH);

			if (!BeamableAssistantMenuItemsPath.Contains(BEAMABLE_SERVER_ASSISTANT_MENU_ITEM_PATH))
				BeamableAssistantMenuItemsPath.Add(BEAMABLE_SERVER_ASSISTANT_MENU_ITEM_PATH);

			// Ensure default paths exist for Beamable Assistant Toolbar Buttons
			if (!BeamableAssistantToolbarButtonsPaths.Contains(PROJECT_ASSISTANT_TOOLBAR_BUTTON_PATH))
				BeamableAssistantToolbarButtonsPaths.Add(PROJECT_ASSISTANT_TOOLBAR_BUTTON_PATH);

			if (!BeamableAssistantToolbarButtonsPaths.Contains(BEAMABLE_ASSISTANT_TOOLBAR_BUTTON_PATH))
				BeamableAssistantToolbarButtonsPaths.Add(BEAMABLE_ASSISTANT_TOOLBAR_BUTTON_PATH);

			if (!BeamableAssistantToolbarButtonsPaths.Contains(BEAMABLE_SERVER_ASSISTANT_TOOLBAR_BUTTON_PATH))
				BeamableAssistantToolbarButtonsPaths.Add(BEAMABLE_SERVER_ASSISTANT_TOOLBAR_BUTTON_PATH);

			// Ensure default paths exist for Beamable Assistant HintDetails Configs
			if (!BeamableAssistantHintDetailConfigPaths.Contains(PROJECT_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH))
				BeamableAssistantHintDetailConfigPaths.Add(PROJECT_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH);

			if (!BeamableAssistantHintDetailConfigPaths.Contains(BEAMABLE_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH))
				BeamableAssistantHintDetailConfigPaths.Add(BEAMABLE_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH);

			if (!BeamableAssistantHintDetailConfigPaths.Contains(BEAMABLE_SERVER_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH))
				BeamableAssistantHintDetailConfigPaths.Add(BEAMABLE_SERVER_ASSISTANT_BEAM_HINTS_DETAILS_CONFIG_PATH);

			ReflectionSystemPaths = ReflectionSystemPaths.Distinct().ToList();
			BeamableAssistantMenuItemsPath = BeamableAssistantMenuItemsPath.Distinct().ToList();
			BeamableAssistantToolbarButtonsPaths = BeamableAssistantToolbarButtonsPaths.Distinct().ToList();
			BeamableAssistantHintDetailConfigPaths = BeamableAssistantHintDetailConfigPaths.Distinct().ToList();

#if UNITY_EDITOR
			Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies();
			AssembliesToSweep.AddRange(playerAssemblies.Select(asm => asm.name).Where(n => !string.IsNullOrEmpty(n)));
			AssembliesToSweep = AssembliesToSweep.Distinct().ToList();
			AssembliesToSweep.Sort();
			Debug.Log($"Assemblies to Sweep\n{string.Join(", ", AssembliesToSweep)}");
#endif
		}
	}
}
