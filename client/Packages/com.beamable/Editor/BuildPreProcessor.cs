using Beamable.Config;
using Beamable.Content;
using Beamable.Editor.Content;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Beamable.Editor
{
	public class BuildPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder { get; }

		public async void OnPreprocessBuild(BuildReport report)
		{
			var messages = new List<string>();
			#if !BEAMABLE_NO_CID_PID_WARNINGS_ON_BUILD
			if (!CheckForConfigDefaultsAlignment(out var message))
			{
				messages.Add(message);
			}
			#endif

			var hasLocalContentChanges = await ContentIO.HasLocalChanges();
			if (hasLocalContentChanges)
			{
				messages.Add("Local Beamable Content has non published changes. ");
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
			ConfigDatabase.Init(forceReload: true);
			if (!ConfigDatabase.TryGetString("cid", out var cid, allowSessionOverrides: false) || string.IsNullOrEmpty(cid))
			{
				var error = $@"BEAMABLE ERROR: No CID was detected!
Without a CID, the Beamable SDK will not be able to connect to any Beamable Cloud. 
Please make sure you have a config-defaults.txt file in Assets/Beamable/Resources. 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.";
				Debug.LogError(error);
				throw new BuildFailedException(error);
			}

			if (!ConfigDatabase.TryGetString("pid", out var pid, allowSessionOverrides: false) || string.IsNullOrEmpty(pid))
			{
				var error = $@"BEAMABLE ERROR: No PID was detected!
Without a PID, the Beamable SDK will not be able to connect to any Beamable Cloud. 
Please make sure you have a config-defaults.txt file in Assets/Beamable/Resources. 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.";
				Debug.LogError(error);
				throw new BuildFailedException(error);
			}

			var runtimeCid = ConfigDatabase.GetString("cid");
			var runtimePid = ConfigDatabase.GetString("pid");

			var cidsMatch = runtimeCid == cid;
			var pidsMatch = runtimePid == pid;

			if (!cidsMatch || !pidsMatch)
			{
				warningMessage = $@"BEAMABLE WARNING: CID/PID Mismatch Detected!
The editor environment is using a cid=[{runtimeCid}] and pid=[{runtimePid}]. These values are assigned in Toolbox.
However, the built target will use a cid=[{cid}] and pid=[{pid}]. These values are assigned in the config-defaults.txt file.
These values do not match. This means that you are building the game
for a different Beamable environment than the editor is currently using. Be careful! 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.";
				Debug.LogWarning(warningMessage);
				return false;
			}

			return true;
		}
	}
}
