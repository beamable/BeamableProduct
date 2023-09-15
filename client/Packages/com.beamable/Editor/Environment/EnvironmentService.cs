using Beamable.Common;
using Beamable.Config;
using Beamable.Serialization;
using System.IO;
using UnityEditor;
using static Beamable.Common.Constants.Features.Environment;

namespace Beamable.Editor.Environment
{
	public class EnvironmentService
	{
		private readonly BeamEditorContext _context;
		public EnvironmentData GetDev(PackageVersion v) => EnvironmentData.BeamableDev(v);
		public EnvironmentData GetStaging(PackageVersion v) => EnvironmentData.BeamableStaging(v);
		public EnvironmentData GetProd(PackageVersion v) => EnvironmentData.BeamableProduction(v);

		public EnvironmentService(BeamEditorContext context)
		{
			_context = context;
		}

		/// <summary>
		/// Erase the overrides file, and reload the editor.
		/// After this method is called, whatever is in env-defaults will be used.
		/// </summary>
		public void ClearOverrides()
		{
			if (File.Exists(OVERRIDE_PATH))
			{
				FileUtil.DeleteFileOrDirectory(OVERRIDE_PATH);
				FileUtil.DeleteFileOrDirectory(OVERRIDE_PATH + ".meta");
				Logout();
				EditorUtility.RequestScriptReload();
				AssetDatabase.Refresh();
			}
		}

		/// <summary>
		/// Create an overrides file, and reload the editor.
		/// After this method is called, Beamable will use the given <see cref="EnvironmentData"/> instead of whatever is in env-defaults.
		/// </summary>
		/// <param name="data"></param>
		public void SetOverrides(EnvironmentOverridesData data)
		{
			var json = JsonSerializable.ToJson(data);
			File.WriteAllText(OVERRIDE_PATH, json);
			Logout();
			EditorUtility.RequestScriptReload();
			AssetDatabase.Refresh();
		}

		void Logout()
		{
			_context.Logout(false);
			_context.EditorAccountService.Clear();
		}
	}
}
