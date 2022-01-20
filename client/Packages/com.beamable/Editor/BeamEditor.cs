using Beamable.Common;
using Beamable.Common.Assistant;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Config;
using Beamable.Editor.Assistant;
using Beamable.Editor.Reflection;
using Beamable.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Beamable
{
	[InitializeOnLoad]
	public static class BeamEditor
	{
		public static readonly CoreConfiguration CoreConfiguration;
		public static readonly ReflectionCache EditorReflectionCache;
		public static readonly IBeamHintGlobalStorage HintGlobalStorage;
		public static readonly IBeamHintPreferencesManager HintPreferencesManager;

		static BeamEditor()
		{
			// Load up core configuration
			CoreConfiguration coreConfiguration;
			try
			{
				coreConfiguration = CoreConfiguration = CoreConfiguration.Instance;
			}
			// Solves a specific issue on first installation of package ---
			catch (ModuleConfigurationNotReadyException)
			{
				coreConfiguration = CoreConfiguration = AssetDatabase.LoadAssetAtPath<CoreConfiguration>("Packages/com.beamable/Editor/Config/CoreConfiguration.asset");
			}
			
			// Initializes the Config database
			ConfigDatabase.Init();

			// Initialize Editor instances of Reflection and Assistant services
			EditorReflectionCache = new ReflectionCache();
			HintGlobalStorage = new BeamHintGlobalStorage();
			HintPreferencesManager = new BeamHintPreferencesManager();

			// Load up all Asset-based IReflectionSystem (injected via ReflectionSystemObject instances). This was made to solve a cross-package injection problem.
			// It doubles as a no-code way for users to inject their own IReflectionSystem into our pipeline.
			var reflectionCacheSystemGuids = AssetDatabase.FindAssets($"t:{nameof(ReflectionSystemObject)}", coreConfiguration.ReflectionSystemPaths
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
						                                             "No, I'll take my chances and don't bother me about these hints anymore.",
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
		}

		public static T GetReflectionSystem<T>() where T : IReflectionSystem => EditorReflectionCache.GetFirstSystemOfType<T>();

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		// ReSharper disable once RedundantAssignment
		public static void GetBeamHintSystem<T>(ref T foundProvider) where T : IBeamHintSystem
		{
			foundProvider = GetReflectionSystem<BeamHintReflectionCache.Registry>().GloballyAccessibleHintSystems.Where(a => a is T).Cast<T>().FirstOrDefault();
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
