#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Beamable.Security.Editor
{
	internal static class SecureStorageIOSBuild
	{
		[PostProcessBuild(100)]
		private static void LinkFrameworks(BuildTarget target, string buildPath)
		{
			if (target != BuildTarget.iOS) { return; }
			string projectPath = PBXProject.GetPBXProjectPath(buildPath);
			var project = new PBXProject();
			project.ReadFromFile(projectPath);
			string frameworkTarget = project.GetUnityFrameworkTargetGuid();
			project.AddFrameworkToProject(frameworkTarget, "Security.framework", false);
			project.AddFrameworkToProject(frameworkTarget, "CoreFoundation.framework", false);
			File.WriteAllText(projectPath, project.WriteToString());
		}
	}
#endif
