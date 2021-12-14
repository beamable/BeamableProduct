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
		public const string DEFAULT_REFLECTION_CACHE_SYSTEM_PATH = "Assets/ReflectionCacheSystems";
		public const string BEAMABLE_REFLECTION_CACHE_SYSTEM_PATH = "Packages/com.beamable/Editor/ReflectionCacheSystems";
		public const string BEAMABLE_SERVER_REFLECTION_CACHE_SYSTEM_PATH = "Packages/com.beamable.server/Editor/ReflectionCacheSystems";

		public static CoreConfiguration Instance => Get<CoreConfiguration>();

		public enum EventHandlerConfig { Guarantee, Replace, Add, }

		[Tooltip("By default, Beamable gives you a default uncaught promise exception handler.\n\n" +
		         "You can set your own via PromiseBase.SetPotentialUncaughtErrorHandler.\n\n" +
		         "In Beamable's Initialization, we guarantee, replace or add our default handler based on this configuration.\n\n" +
		         "- Guarantee => Guarantees at least Beamable's default handler will exist if you don't configure one yourself\n" +
		         "- Replace => Replaces any handler configured with Beamable's default handler.\n" +
		         "- Add => Adds Beamable's default handler to the list of handlers, but keep existing handlers configured.\n")]
		public EventHandlerConfig DefaultUncaughtPromiseHandlerConfiguration;


		[Header("Beamable Assistant")]
		[Tooltip("Enable this to receive a warning (toggle-able per Beam Hint Validation) when entering playmode.\n\n" +
		         "This aims to help you enforce project workflows and guarantee people are not wasting time chasing issues that we can identify for you.")]
		public bool EnablePlayModeWarning; 
		
		[Header("Reflection Cache Systems")]
		[Tooltip("Register all paths in which we'll need to look for ReflectionCacheUserSystemObjects.\n" +
		         "We don't look everywhere as that could impact editor experience on larger projects.")]
		public List<string> ReflectionCacheUserSystemPaths = new List<string>();

		[Tooltip("Register any assemblies you wish to ignore from the assembly sweep.")]
		public List<string> AssembliesToSweep = new List<string>();

		private void OnValidate()
		{
			if (!ReflectionCacheUserSystemPaths.Contains(DEFAULT_REFLECTION_CACHE_SYSTEM_PATH))
				ReflectionCacheUserSystemPaths.Add(DEFAULT_REFLECTION_CACHE_SYSTEM_PATH);

			if (!ReflectionCacheUserSystemPaths.Contains(BEAMABLE_REFLECTION_CACHE_SYSTEM_PATH))
				ReflectionCacheUserSystemPaths.Add(BEAMABLE_REFLECTION_CACHE_SYSTEM_PATH);

			if (!ReflectionCacheUserSystemPaths.Contains(BEAMABLE_SERVER_REFLECTION_CACHE_SYSTEM_PATH))
				ReflectionCacheUserSystemPaths.Add(BEAMABLE_SERVER_REFLECTION_CACHE_SYSTEM_PATH);

			ReflectionCacheUserSystemPaths = ReflectionCacheUserSystemPaths.Distinct().ToList();

#if UNITY_EDITOR
			Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies();
			AssembliesToSweep.AddRange(playerAssemblies.Select(asm => asm.name));
			AssembliesToSweep = AssembliesToSweep.Distinct().ToList();
			AssembliesToSweep.Sort();
#endif
		}
	}
}
