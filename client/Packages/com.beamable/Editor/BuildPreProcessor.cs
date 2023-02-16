using Beamable.Config;
using Beamable.Content;
using Beamable.Editor.Content;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Beamable.Editor
{
	public class BuildPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder { get; }

#if !UNITY_STANDALONE
		public void OnPreprocessBuild(BuildReport report)
		{
			CheckForConfigDefaultsAlignment();
			if (CoreConfiguration.Instance.PreventCodeStripping)
			{
				BeamableLinker.GenerateLinkFile();
			}

			if (CoreConfiguration.Instance.PreventAddressableCodeStripping)
			{
				BeamableLinker.GenerateAddressablesLinkFile();
			}
		}
#else
        public async void OnPreprocessBuild(BuildReport report)
        {
	        CheckForConfigDefaultsAlignment();

            if (ContentConfiguration.Instance.BakeContentOnBuild)
            {
                await ContentIO.BakeContent();
            }
			if (CoreConfiguration.Instance.PreventCodeStripping)
            {
				BeamableLinker.GenerateLinkFile();
            }
        }
#endif

		/// <summary>
		/// it is possible that the developer may have config-defaults set to cid/pid 1,
		/// but have their toolbox set to cid/pid 2.
		///
		/// In this scenario, the built game is going to use the config-default data.
		/// However, if the cid/pids are different, we need to log a message explaining
		/// why the built game has a different cid/pid than the toolbox configuration. 
		/// </summary>
		private static void CheckForConfigDefaultsAlignment()
		{

			ConfigDatabase.Init(forceReload:true);
			if (!ConfigDatabase.TryGetString("cid", out var cid, allowSessionOverrides: false) || string.IsNullOrEmpty(cid))
			{
				Debug.LogError($@"BEAMABLE ERROR: No CID was detected!
Without a CID, the Beamable SDK will not be able to connect to any Beamable Cloud. 
Please make sure you have a config-defaults.txt file in Assets/Beamable/Resources. 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.");
			}
			
			if (!ConfigDatabase.TryGetString("pid", out var pid, allowSessionOverrides: false) || string.IsNullOrEmpty(pid))
			{
				Debug.LogError($@"BEAMABLE ERROR: No PID was detected!
Without a PID, the Beamable SDK will not be able to connect to any Beamable Cloud. 
Please make sure you have a config-defaults.txt file in Assets/Beamable/Resources. 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.");
			}

			var runtimeCid = ConfigDatabase.GetString("cid");
			var runtimePid = ConfigDatabase.GetString("pid");

			var cidsMatch = runtimeCid == cid;
			var pidsMatch = runtimePid == pid;

			if (!cidsMatch || !pidsMatch)
			{
				Debug.LogWarning($@"BEAMABLE WARNING: CID/PID Mismatch Detected!
The editor environment is using a cid=[{runtimeCid}] and pid=[{runtimePid}]. These values are assigned in Toolbox.
However, the built target will use a cid=[{cid}] and pid=[{pid}]. These values are assigned in the config-defaults.txt file.
These values do not match. This means that you are building the game
for a different Beamable environment than the editor is currently using. Be careful! 
In the Beamable Toolbox, click on the account icon, and then in the account summary
popup, click the 'Save Config-Defaults' button.");
			}
		}
	}
}
