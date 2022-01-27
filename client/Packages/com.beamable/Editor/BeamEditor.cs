using Beamable.Common;
using Beamable.Common.Assistant;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Config;
using Beamable.Editor;
using Beamable.Editor.Assistant;
using Beamable.Editor.Reflection;
using Beamable.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.Compilation;
#endif


namespace Beamable
{
	[InitializeOnLoad]
	public static class BeamEditor
	{
		public static readonly CoreConfiguration CoreConfiguration;
		public static readonly ReflectionCache EditorReflectionCache;
		public static readonly IBeamHintGlobalStorage HintGlobalStorage;
		public static readonly IBeamHintPreferencesManager HintPreferencesManager;

		public static readonly bool IsInitialized;

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

			// This is needed ONLY for the Re-Import All case. When a re-import all happens, the first time this is InitializeOnLoad --- the AssetDatabase fails to find the CoreConfiguration object.
			// There's not really a lot of ways we can avoid get around this.
			// This means CANNOT have an internal guarantee that BeamEditor is always fully initialized --- which means, we need to null-check the stuff we get from BeamEditor 😭
			// TODO: Maybe we can talk to Unity about this and hope that by Unity 2027 LTS we get an InitializeOnLoadAfterAssets callback 🤷‍ or something.
			if (coreConfiguration == null)
			{
				Spew.Logger.DoSpew("Triggering Script Recompile From Core Config check");
				TriggerScriptRecompile();
				return;
			}

			// Ensures we have the latest assembly definitions and paths are all correctly setup.
			coreConfiguration.OnValidate();

			// Initializes the Config database
			try
			{
				ConfigDatabase.Init();
			}
			catch (FileNotFoundException e)
			{
				if (e.FileName == ConfigDatabase.GetConfigFileName())
				{
					Spew.Logger.DoSpew("Triggering Script Recompile From Config Database check");
					TriggerScriptRecompile();
					return;
				}
			}

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

			IsInitialized = true;
		}

		public static T GetReflectionSystem<T>() where T : IReflectionSystem => EditorReflectionCache == null ? default : EditorReflectionCache.GetFirstSystemOfType<T>();

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		// ReSharper disable once RedundantAssignment
		public static void GetBeamHintSystem<T>(ref T foundProvider) where T : IBeamHintSystem
		{
			var hintReflectionSystem = GetReflectionSystem<BeamHintReflectionCache.Registry>();

			if (hintReflectionSystem == null)
				foundProvider = default;
			else
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



#if UNITY_EDITOR
		public static void TriggerScriptRecompile()
		{
#if UNITY_2019_3_OR_NEWER
            CompilationPipeline.RequestScriptCompilation();
#elif UNITY_2017_1_OR_NEWER
			var editorAssembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
			var editorCompilationInterfaceType = editorAssembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
			
			var isCompilationPendingMethod = editorCompilationInterfaceType.GetMethod("IsCompilationPending", BindingFlags.Static | BindingFlags.Public);
			var isCompilingMethod = editorCompilationInterfaceType.GetMethod("IsCompiling", BindingFlags.Static | BindingFlags.Public);

			var isCompilationPending = (bool)isCompilationPendingMethod.Invoke(editorCompilationInterfaceType, null);
			var isCompiling = (bool)isCompilingMethod.Invoke(editorCompilationInterfaceType, null);
			if (isCompilationPending || isCompiling)
				return;
			
			Spew.Logger.DoSpew("Actually requesting recompilation to happen!");

			var dirtyAllScriptsMethod = editorCompilationInterfaceType.GetMethod("DirtyAllScripts", BindingFlags.Static | BindingFlags.Public);
			dirtyAllScriptsMethod?.Invoke(editorCompilationInterfaceType, null);
#endif
		}
#endif
	}
}
