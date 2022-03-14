using Beamable.AccountManagement;
using Beamable.Avatars;
using Beamable.Common.Assistant;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Config;
using Beamable.Console;
using Beamable.Content;
using Beamable.Editor;
using Beamable.Editor.Assistant;
using Beamable.Editor.Modules.EditorConfig;
using Beamable.Editor.Reflection;
using Beamable.Editor.ToolbarExtender;
using Beamable.Inventory.Scripts;
using Beamable.Reflection;
using Beamable.Sessions;
using Beamable.Shop;
using Beamable.Sound;
using Beamable.Theme;
using Beamable.Tournaments;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Logger = Beamable.Common.Spew.Logger;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.Compilation;
#endif

namespace Beamable
{
	[InitializeOnLoad, BeamContextSystem]
	public static class BeamEditor
	{
		public static CoreConfiguration CoreConfiguration { get; private set; }
		public static ReflectionCache EditorReflectionCache { get; private set; }
		public static IBeamHintGlobalStorage HintGlobalStorage { get; private set; }
		public static IBeamHintPreferencesManager HintPreferencesManager { get; private set; }
		public static bool IsInitialized { get; private set; }

		static BeamEditor()
		{
			Initialize();
		}

		static void Initialize()
		{
			if (IsInitialized) return;

			// Attempts to load all Module Configurations --- If they fail, we delay BeamEditor initialization until they don't fail.
			// The ONLY fail case is:
			//   - On first import or "re-import all", Resources and AssetDatabase don't know about the existence of these instances when this code runs for a couple of frames.
			//   - Empirically, we noticed this takes 2~3 attempts (frames) until this is done. So it's an acceptable and unnoticeable wait.
			// Doing this loading in this manner and making our windows delay their initialization until this is initialized (see BeamableAssistantWindow.OnEnable), we can
			// never have to care about this UnityEditor problem in our code that actually does things and we can have a guarantee that these will never throw.
			CoreConfiguration coreConfiguration;
			try
			{
				coreConfiguration = CoreConfiguration = CoreConfiguration.Instance;
				_ = AccountManagementConfiguration.Instance;
				_ = AvatarConfiguration.Instance;
				_ = BussConfiguration.OptionalInstance;
				_ = ConsoleConfiguration.Instance;
				_ = ContentConfiguration.Instance;
				_ = EditorConfiguration.Instance;
				_ = InventoryConfiguration.Instance;
				_ = SessionConfiguration.Instance;
				_ = ShopConfiguration.Instance;
				_ = SoundConfiguration.Instance;
				_ = ThemeConfiguration.Instance;
				_ = TournamentsConfiguration.Instance;
			}
			// Solves a specific issue on first installation of package ---
			catch (ModuleConfigurationNotReadyException)
			{
				EditorApplication.delayCall += Initialize;
				return;
			}

			// Ensures we have the latest assembly definitions and paths are all correctly setup.
			CoreConfiguration.OnValidate();

			EditorAPI.Instance.Then(_ => { });

			// Initializes the Config database
			// This solves the same problem that the try/catch block around the ModuleConfigurations solves.
			try
			{
				ConfigDatabase.Init();
			}
			catch (FileNotFoundException e)
			{
				if (e.FileName == ConfigDatabase.GetConfigFileName())
				{
					Logger.DoSpew("Config File not found during initialization dodged!");
					EditorApplication.delayCall += Initialize;
					return;
				}
			}

			// If we ever get to this point, we are guaranteed to run the initialization until the end so we...
			// Initialize Editor instances of Reflection and Assistant services
			EditorReflectionCache = new ReflectionCache();
			HintGlobalStorage = new BeamHintGlobalStorage();
			HintPreferencesManager = new BeamHintPreferencesManager(new List<BeamHintHeader>()
			{
				new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_DOCKER, BeamHintIds.ID_CHANGES_NOT_DEPLOYED_TO_LOCAL_DOCKER),
			});

			// Load up all Asset-based IReflectionSystem (injected via ReflectionSystemObject instances). This was made to solve a cross-package injection problem.
			// It doubles as a no-code way for users to inject their own IReflectionSystem into our pipeline.
			var reflectionCacheSystemGuids = BeamableAssetDatabase.FindAssets<ReflectionSystemObject>(
				coreConfiguration.ReflectionSystemPaths
								 .Where(Directory.Exists)
								 .ToArray());

			// Get ReflectionSystemObjects and sort them
			var reflectionSystemObjects = reflectionCacheSystemGuids.Select(reflectionCacheSystemGuid =>
																	{
																		var assetPath = AssetDatabase.GUIDToAssetPath(reflectionCacheSystemGuid);
																		return AssetDatabase.LoadAssetAtPath<ReflectionSystemObject>(assetPath);
																	})
																	.Union(Resources.LoadAll<ReflectionSystemObject>("ReflectionSystems"))
																	.Where(system => system.Enabled)
																	.ToList();
			reflectionSystemObjects.Sort((reflectionSys1, reflectionSys2) => reflectionSys1.Priority.CompareTo(reflectionSys2.Priority));

			// Inject them into the ReflectionCache system in the correct order.
			foreach (var reflectionSystemObject in reflectionSystemObjects)
			{
				EditorReflectionCache.RegisterTypeProvider(reflectionSystemObject.TypeProvider);
				EditorReflectionCache.RegisterReflectionSystem(reflectionSystemObject.System);
			}

			// Also initializes the Reflection Cache system with it's IBeamHintGlobalStorage instance
			// (that gets propagated down to any IReflectionSystem that also implements IBeamHintProvider).
			// Finally, calls the Generate Reflection cache
			EditorReflectionCache.SetStorage(HintGlobalStorage);
			EditorReflectionCache.GenerateReflectionCache(coreConfiguration.AssembliesToSweep);

			// Hook up editor play-mode-warning feature.
			void OnPlayModeStateChanged(PlayModeStateChange change)
			{
				if (!coreConfiguration.EnablePlayModeWarning) return;

				if (change == PlayModeStateChange.ExitingEditMode)
				{
					HintPreferencesManager.SplitHintsByPlayModeWarningPreferences(HintGlobalStorage.All, out var toWarnHints, out _);
					var hintsToWarnAbout = toWarnHints.ToList();
					if (hintsToWarnAbout.Count > 0)
					{
						var msg = string.Join("\n", hintsToWarnAbout.Select(hint => $"- {hint.Header.Id}"));

						var res = EditorUtility.DisplayDialogComplex("Beamable Assistant",
																	 "There are pending Beamable Validations.\n" + "These Hints may cause problems during runtime:\n\n" + $"{msg}\n\n" +
																	 "Do you wish to stop entering playmode and see these validations?", "Yes, I want to stop and go see validations.",
																	 "No, I'll take my chances and don't bother me about these specific hints anymore.",
																	 "No, I'll take my chances and don't bother me ever again about any hints.");

						if (res == 0)
						{
							EditorApplication.isPlaying = false;
							BeamableAssistantWindow.ShowWindow();
						}
						else if (res == 1)
						{
							foreach (var hint in hintsToWarnAbout) HintPreferencesManager.SetHintPlayModeWarningPreferences(hint, BeamHintPlayModeWarningPreference.Disabled);
						}
						else if (res == 2)
						{
							coreConfiguration.EnablePlayModeWarning = false;
						}
					}
				}
			}
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			// Set up Globally Accessible Hint System Dependencies and then call init
			foreach (var hintSystem in GetReflectionSystem<BeamHintReflectionCache.Registry>().GloballyAccessibleHintSystems)
			{
				hintSystem.SetStorage(HintGlobalStorage);
				hintSystem.SetPreferencesManager(HintPreferencesManager);

				hintSystem.OnInitialized();
			}

			IsInitialized = true;

			// Initialize toolbar
			BeamableToolbarExtender.LoadToolbarExtender();


		}

		public static T GetReflectionSystem<T>() where T : IReflectionSystem => EditorReflectionCache.GetFirstSystemOfType<T>();

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		// ReSharper disable once RedundantAssignment
		public static void GetBeamHintSystem<T>(ref T foundProvider) where T : IBeamHintSystem
		{
			var hintReflectionSystem = GetReflectionSystem<BeamHintReflectionCache.Registry>();
			foundProvider = hintReflectionSystem.GloballyAccessibleHintSystems.Where(a => a is T).Cast<T>().FirstOrDefault();
		}

		[RegisterBeamableDependencies(), System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void ConditionallyRegisterBeamHintsAsServices(IDependencyBuilder builder)
		{
			foreach (var hintSystemConstructor in GetReflectionSystem<BeamHintReflectionCache.Registry>().BeamContextAccessibleHintSystems)
			{
				// It'd be nice to do it this way
				builder.AddSingleton(hintSystemConstructor.DeclaringType, () =>
				{
					var builtObj = (IBeamHintSystem)hintSystemConstructor.Invoke(null);
					builtObj.SetPreferencesManager(HintPreferencesManager);
					builtObj.SetStorage(HintGlobalStorage);

					builtObj.OnInitialized();
					return builtObj;
				});
			}
		}
	}
}
