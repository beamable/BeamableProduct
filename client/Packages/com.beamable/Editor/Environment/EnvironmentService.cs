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
		public EnvironmentData GetDev() => EnvironmentData.BeamableDev;
		public EnvironmentData GetStaging() => EnvironmentData.BeamableStaging;
		public EnvironmentData GetProd() => EnvironmentData.BeamableProduction;

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
				ConfigDatabase.DeleteConfigDatabase();
				EditorUtility.RequestScriptReload();
				AssetDatabase.Refresh();
			}
		}

		/// <summary>
		/// Create an overrides file, and reload the editor.
		/// After this method is called, Beamable will use the given <see cref="EnvironmentData"/> instead of whatever is in env-defaults.
		/// </summary>
		/// <param name="data"></param>
		public void SetOverrides(EnvironmentData data)
		{
			var json = JsonSerializable.ToJson(data);
			File.WriteAllText(OVERRIDE_PATH, json);
			ConfigDatabase.DeleteConfigDatabase();
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
