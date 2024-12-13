using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Beamable.Editor
{
	public static class PlayerSettingsHelper
	{
		public static HashSet<string> GetDefines()
		{
#if UNITY_6000_0_OR_NEWER
			var definesString = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
#else
			var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
			var allDefines = definesString.Split(';').ToList();
			return new HashSet<string>(allDefines);
		}

		public static void SetDefines(HashSet<string> allDefines)
		{
#if UNITY_6000_0_OR_NEWER
			PlayerSettings.SetScriptingDefineSymbols(
				NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
			   string.Join(";", allDefines.ToArray()));
#else
			PlayerSettings.SetScriptingDefineSymbolsForGroup(
			   EditorUserBuildSettings.selectedBuildTargetGroup,
				string.Join(";", allDefines.ToArray()));
#endif
		}

		public static void EnableFlag(string flag)
		{
			var allDefines = GetDefines();

			if (allDefines.Contains(flag)) return;

			allDefines.Add(flag);
			SetDefines(allDefines);
		}

		public static void DisableFlag(string flag)
		{
			var allDefines = GetDefines();
			if (!allDefines.Contains(flag)) return;

			allDefines.Remove(flag);
			SetDefines(allDefines);
		}

	}
}
