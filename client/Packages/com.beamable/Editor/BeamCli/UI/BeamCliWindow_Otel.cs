using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using Beamable.Editor.Utility;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		private UnityOtelManager _unityOtelManager;
		private void OnOtelGui()
		{
			if (_unityOtelManager == null)
			{
				_unityOtelManager = Scope.GetService<UnityOtelManager>();
			}
			if (_unityOtelManager.OtelStatus == null)
			{
				DrawBlockLoading("Fetching OTEL status...");
				return;
			}

			BeamGUI.ShowDisabled(_unityOtelManager.OtelManagerStatus is OtelManagerStatus.Normal, () => DrawTools(new CliWindowToolAction
			{
				name = "Publish Logs",
				onClick = () =>
				{
					_ = Scope.GetService<UnityOtelManager>().PublishLogs();
				}
			}, new CliWindowToolAction
			{
				name = "Prune Logs",
				onClick = () =>
				{
					_ = Scope.GetService<UnityOtelManager>().PruneLogs();
				}
			}));
			
			switch (_unityOtelManager.OtelManagerStatus)
			{
				case OtelManagerStatus.Publishing:
					EditorGUILayout.LabelField("Publishing Logs...");
					break;
				case OtelManagerStatus.Pruning:
					EditorGUILayout.LabelField("Pruning Logs...");
					break;
			}

			string lastPublished = _unityOtelManager.OtelStatus.LastPublishTimestamp == 0
				? "Never"
				: DateTimeOffset.FromUnixTimeMilliseconds(_unityOtelManager.OtelStatus.LastPublishTimestamp).ToLocalTime().ToString("g");
			CoreConfiguration.Instance.EnableOtelAutoPublish = EditorGUILayout.Toggle("Auto-Publish", CoreConfiguration.Instance.EnableOtelAutoPublish);
			EditorGUILayout.LabelField("Last Published", lastPublished);
			EditorGUILayout.LabelField("Size:", $"{(float)_unityOtelManager.OtelStatus.FolderSize/(1024 * 1024):F} MB");
			EditorGUILayout.LabelField("Log Files Count: ", _unityOtelManager.OtelStatus.FileCount.ToString());
			// TODO: List all collector running with watch
		}
		
	}
	
}
