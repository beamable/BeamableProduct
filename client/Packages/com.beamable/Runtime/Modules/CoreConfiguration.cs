using Beamable.Api.Commerce;
using Beamable.Common.Content;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

using static Beamable.Common.Constants.MenuItems.Assets;

namespace Beamable
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(
		fileName = "CoreConfiguration",
		menuName = Paths.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
				   "Core Configuration")]
#endif
	public class CoreConfiguration : ModuleConfigurationObject, ICommerceConfig
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

		[Tooltip("By default, Beamable won't let Beamable assemblies be code stripped from your project. When this setting is enabled, " +
				 "anytime the project is built, a link.xml file will be generated in the Assets/Beamable/Resources folder that protects " +
				 "the Beamable assemblies. If you disable this setting, the link file won't be generated. However, any existing link file " +
				 "will not be deleted.")]
		public bool PreventCodeStripping = true;

		[Tooltip("by default, Beamable won't let Unity Addressables code files be stripped form the project on build. " +
				 "When this setting is enabled, anything the project is built, a link.xml file will be generated in the Assets/beamable/Resources/AddressableLinker " +
				 "folder. If you disable this setting, the link file won't be generated. However, any existing link file won't be deleted. ")]
		public bool PreventAddressableCodeStripping = true;

		public static CoreConfiguration Instance => Get<CoreConfiguration>();

		public enum EventHandlerConfig { Guarantee, Replace, Add, }

		[Tooltip("By default, Beamable gives you a default uncaught promise exception handler.\n\n" +
				 "You can set your own via PromiseBase.SetPotentialUncaughtErrorHandler.\n\n" +
				 "In Beamable's Initialization, we guarantee, replace or add our default handler based on this configuration.\n\n" +
				 "- Guarantee => Guarantees at least Beamable's default handler will exist if you don't configure one yourself\n" +
				 "- Replace => Replaces any handler configured with Beamable's default handler.\n" +
				 "- Add => Adds Beamable's default handler to the list of handlers, but keep existing handlers configured.\n")]
		public EventHandlerConfig DefaultUncaughtPromiseHandlerConfiguration;

		[Tooltip("When a Beamable Context tries to initialize, but fails for some reason, the " +
				 "context will automatically attempt to retry the initialization. A retry will be attempted " +
				 "after the number of seconds in the ContextRetryDelays array, for the index of the current " +
				 "retry attempt. If the array is exhausted, and this EnableInfiniteContextRetries field is true, " +
				 "then the last index in the retry timing array will be used on repeat forever, until the context " +
				 "initializes. If you set this to false, then at after all retries have failed, the BeamContext OnReady " +
				 "promise will result in an error.")]
		public bool EnableInfiniteContextRetries = true;

		[Tooltip("When a Beamable Context tries to initialize, but fails for some reason, the " +
				 "context will automatically attempt to retry the initialization. This array controls " +
				 "the number, and timing, of the retries. Each value represents a number of seconds to wait " +
				 "before retrying again. If an retry fails, then the next index in the array will be used " +
				 "for the next retry delay. Once the array has been exhausted, depending on the value of " +
				 "EnableInfiniteContextRetries, the OnReady promise will either throw an error, or the last " +
				 "value in the ContextRetryDelays array will be used forever.")]
		public double[] ContextRetryDelays = new double[] { 2, 2, 4, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10 };

		[Tooltip("It allows to globally enable/disable offline cache.")]
		public bool UseOfflineCache = true;

		[Tooltip("It will enable/disable hearbeat service default behaviour.\n" +
				 "Disabling it allows to reduce amount of calls to Beamable with cost of disabling support for matchmaking services.")]
		public bool SendHeartbeat = true;

		[Tooltip("By default, when your player isn't connected to the internet, Beamable will accrue inventory writes " +
				 "in a buffer and optimistically simulate the effects locally in memory. When your player comes back " +
				 "online, the buffer will be replayed. If this isn't desirable, you should disable the feature.")]
		public OfflineStrategy InventoryOfflineMode = OfflineStrategy.Optimistic;
		[Tooltip(@"The CommerceService will use the PlayerStoreView.nextDeltaSeconds value
to automatically refresh the store content.

However, the value of the nextDeltaSeconds may be too small, and result in overly chatty networking.
To prevent excess networking, the CommerceListingRefreshSecondsMinimum value is used as a
minimum number of seconds to wait before automatically refreshing the store.

When this value is 0, there is effectively no minimum wait period.

The default is 60 seconds.
")]
		public int CommerceListingRefreshSecondsMinimum = 60;
		
		int ICommerceConfig.CommerceListingRefreshSecondsMinimum => CommerceListingRefreshSecondsMinimum;
		
		[Header("Beamable Toolbar")]
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

		[Tooltip("By default, Unity will look for the Beam CLI as a globally installed dotnet tool. If you wish to override this, you can " +
				 "specify the full path here. The path should include the executable file.")]
		public OptionalString BeamCLIPath = new OptionalString();

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

			RebuildAssembliesToSweep();
		}

		/// <summary>
		/// Updates content of <see cref="AssembliesToSweep"/>
		/// which is later passed to <see cref="Beamable.Common.Reflection.ReflectionCache"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RebuildAssembliesToSweep()
		{
#if UNITY_EDITOR
			string[] assemblyNames =  GetAllUserAssemblies();

			for (int i = 0; i < assemblyNames.Length; i++)
			{
				var nameWithoutEx = Path.GetFileNameWithoutExtension(assemblyNames[i]);

				if(AssembliesToSweep.Contains(nameWithoutEx))
					continue;

				var shouldAddAssembly = !assemblyNames[i].Contains("Packages/") && !assemblyNames[i].Contains("Assets/");

				if (shouldAddAssembly)
				{
					AssembliesToSweep.Add(nameWithoutEx);
				}
			}
			var commonDllsDir = new DirectoryInfo(Beamable.Common.Constants.Directories.SAMS_COMMON_DLL_DIR);

			if (commonDllsDir.Exists)
			{
				FileInfo[] files = commonDllsDir.GetFiles();
				for (int i = 0; i < files.Length; i++)
				{
					var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i].Name);
					if (files[i].Extension.Equals(".dll") && !AssembliesToSweep.Contains(fileNameWithoutExtension))
					{
						AssembliesToSweep.Add(fileNameWithoutExtension);
					}
				}
			}
#if BEAMABLE_DEVELOPER
			for (int i = 0; i < AssembliesToSweep.Count; i++)
			{
				if (AssembliesToSweep[i].Contains("UnityEditor.Test") &&
				    !AssembliesToSweep[i].Contains("Beamable.Microservice") &&
				    !AssembliesToSweep[i].Contains("Beamable.Storage"))
				{
					AssembliesToSweep.RemoveAt(i);
					i--;
				}
			}
#endif
			AssembliesToSweep.Sort();
#endif
		}

		/// <summary>
		/// it's reflection-bruteForce but looks like it gives the same result as CompilationPipeline.GetAssemblies()
		/// </summary>
		/// <returns>Array with paths to assemblies</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string[] GetAllUserAssemblies()
		{
#if UNITY_EDITOR
			var coreAssembly = System.Reflection.Assembly.Load("UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
			var getAssembliesMethodInfo = coreAssembly?.GetType("UnityEngine.ScriptingRuntime")?.GetMethod("GetAllUserAssemblies");

			if (getAssembliesMethodInfo != null)
			{
				string[] assemblyNames =  (string[])getAssembliesMethodInfo.Invoke(null, null);
				return assemblyNames;
			}
#endif
			return new string[] { };
		}

	}
}
