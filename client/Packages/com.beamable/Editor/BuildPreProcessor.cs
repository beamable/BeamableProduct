using Beamable.Common;
using Beamable.Config;
using Beamable.Content;
using Beamable.Editor.Content;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Beamable.Editor
{
	public class BuildPreProcessor : IPreprocessBuildWithReport
	{
		/// <summary>
		/// Dictionary where key represents define symbol
		/// and value is list of proguard rules required by that define symbol
		/// </summary>
		private static readonly Dictionary<string, string[]> RulesPerDefineSymbol = new Dictionary<string, string[]>
		{
			{"BEAMABLE_GPGS", new[] {"com.beamable.googlesignin.**", "com.google.unity.**"}}
		};

		public int callbackOrder { get; }

		public async void OnPreprocessBuild(BuildReport report)
		{
#if !BEAMABLE_FORCE_BUILD_PREPROCESSOR
			var isHeadless = Application.isBatchMode;
			if (isHeadless)
			{
				return;
			}
#endif

			var messages = new List<string>();
#if !BEAMABLE_NO_CID_PID_WARNINGS_ON_BUILD
			if (!CheckForConfigDefaultsAlignment(out var message))
			{
				messages.Add(message);
			}
#endif
			if (!CheckForCorrectProguardRules(out var proguardMessage))
			{
				messages.Add(proguardMessage);
			}

			var hasLocalContentChangesPromise = ContentIO.HasLocalChanges();
			if (hasLocalContentChangesPromise.IsCompleted)
			{
				var hasLocalContentChanges = hasLocalContentChangesPromise.GetResult();
				if (hasLocalContentChanges)
				{
					messages.Add("Local Beamable Content has non published changes. ");
				}
			}
			else
			{
				Debug.LogWarning("Beamable wasn't able to detect if there was unpublished content, because the Beamable SDK wasn't initialized yet.");
			}

			if (messages.Count > 0)
			{
				// EditorUtility.Dis
				var title = messages.Count == 1 ? "Beamable Warning" : $"{messages.Count} Beamable Warnings";
				if (!EditorUtility.DisplayDialog(title, string.Join("\n\n", messages), "Continue Build",
												 "Abort Build"))
				{
					throw new BuildFailedException("Aborted build due to Beamable checks");
				}
			}

			if (ContentConfiguration.Instance.BakeContentOnBuild)
			{
				await ContentIO.BakeContent(skipCheck: true);
			}
			if (CoreConfiguration.Instance.PreventCodeStripping)
			{
				BeamableLinker.GenerateLinkFile();
			}
			if (CoreConfiguration.Instance.PreventAddressableCodeStripping)
			{
				BeamableLinker.GenerateAddressablesLinkFile();
			}
		}

		/// <summary>
		/// it is possible that the developer may have config-defaults set to cid/pid 1,
		/// but have their toolbox set to cid/pid 2.
		///
		/// In this scenario, the built game is going to use the config-default data.
		/// However, if the cid/pids are different, we need to log a message explaining
		/// why the built game has a different cid/pid than the toolbox configuration. 
		/// </summary>
		/// <returns>
		/// True if the cid/pids are the same
		/// </returns>
		private static bool CheckForConfigDefaultsAlignment(out string warningMessage)
		{
			warningMessage = string.Empty;

			var provider = new ConfigDatabaseProvider();
			var runtimeCid = provider.Cid;
			var runtimePid = provider.Pid;
			
			var editorCtx = BeamEditorContext.Default;
			var editorProvider = editorCtx.ServiceScope.GetService<IRuntimeConfigProvider>();
			
			if (string.IsNullOrEmpty(runtimeCid))
			{
				var error = $@"BEAMABLE ERROR: No CID was detected!
Without a CID, the Beamable SDK will not be able to connect to any Beamable Cloud. 
Please make sure you have a config-defaults.txt file in Assets/Beamable/Resources. 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.";
				Debug.LogError(error);
				throw new BuildFailedException(error);
			}

			if (string.IsNullOrEmpty(runtimePid))
			{
				var error = $@"BEAMABLE ERROR: No PID was detected!
Without a PID, the Beamable SDK will not be able to connect to any Beamable Cloud. 
Please make sure you have a config-defaults.txt file in Assets/Beamable/Resources. 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.";
				Debug.LogError(error);
				throw new BuildFailedException(error);
			}

			var editorCid = editorProvider.Cid;
			var editorPid = editorProvider.Pid;

			var cidsMatch = runtimeCid == editorCid;
			var pidsMatch = runtimePid == editorPid;

			if (!cidsMatch || !pidsMatch)
			{
				warningMessage = $@"BEAMABLE WARNING: CID/PID Mismatch Detected!
The editor environment is using a cid=[{editorCid}] and pid=[{editorPid}]. These values are assigned in Toolbox.
However, the built target will use a cid=[{runtimeCid}] and pid=[{runtimePid}]. These values are assigned in the config-defaults.txt file.
These values do not match. This means that you are building the game
for a different Beamable environment than the editor is currently using. Be careful! 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.";
				Debug.LogWarning(warningMessage);
				return false;
			}

			return true;
		}


		/// <summary>
		/// It performs checks for each symbol specified in <see cref="RulesPerDefineSymbol"/>
		/// if project contains all of the rules that are required in case of define symbol presence.
		/// </summary>
		/// <returns>
		/// True if all required rules are defined.
		/// </returns>
		private static bool CheckForCorrectProguardRules(out string warningMessage)
		{
			warningMessage = string.Empty;
#if UNITY_ANDROID && !BEAMABLE_NO_CHECKS_FOR_PROGUARD
			var proguardFilesGuids =
				AssetDatabase.FindAssets("t:TextAsset proguard-user", new[] {"Assets/Plugins/Android"});

			var doesProguardFileExists = proguardFilesGuids.Length > 0;

			foreach (var defineSymbolRules in RulesPerDefineSymbol)
			{
				if (!PlayerSettingsHelper.GetDefines().Contains(defineSymbolRules.Key))
				{
					continue;
				}

				var rules = defineSymbolRules.Value;
				if (!doesProguardFileExists)
				{
					warningMessage = "There is no Proguard File";
					var shouldCreateProguardFile = !EditorGUIExtension.IsInHeadlessMode() &&
					                               EditorUtility.DisplayDialog("Create proguard file",
					                                                           $"{warningMessage}.\nDo you want to create the proguard file?",
					                                                           "Yes", "No, cancel build");
					if (shouldCreateProguardFile)
					{
						var proguardDir = Path.Combine("Assets", "Plugins", "Android");
						if (!Directory.Exists(proguardDir))
						{
							Directory.CreateDirectory(proguardDir);
						}
						var filePath = Path.Combine(proguardDir, "proguard-user.txt");

						File.WriteAllText(filePath, string.Empty);
						AssetDatabase.SaveAssets();
						AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
						
						proguardFilesGuids =
							AssetDatabase.FindAssets("t:TextAsset proguard-user", new[] {"Assets/Plugins/Android"});
						doesProguardFileExists = proguardFilesGuids.Length > 0;
					}
					else
					{
						return false;
					}
				}

				var path = AssetDatabase.GUIDToAssetPath(proguardFilesGuids[0]);
				var proguardContent = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

				var missingRules = rules.Where(r => !proguardContent.text.Contains(r)).ToList();
				var doesProguardFileHaveCorrectRules = missingRules.Count == 0;
				if (doesProguardFileHaveCorrectRules)
				{
					continue;
				}

				var rulesString = $"{string.Join(" { *; }\n", missingRules)} {{ *; }}";
				warningMessage = $"Proguard File does not have this rules:\n{rulesString}";
				if (!EditorGUIExtension.IsInHeadlessMode() && EditorUtility.DisplayDialog("Update proguard file",
					    $"{warningMessage}.\nDo you want to update the proguard file?", 
					    "Yes", "No, cancel build"))
				{
					string newContentText = $"{proguardContent.text}\n{rulesString}\n";
					File.WriteAllText(AssetDatabase.GetAssetPath(proguardContent), newContentText);
					EditorUtility.SetDirty(proguardContent);
					AssetDatabase.SaveAssets();
					AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(proguardContent),
					                          ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
					warningMessage = string.Empty;
					continue;
				}

				return false;
			}
#endif
			return true;
		}
	}
}
