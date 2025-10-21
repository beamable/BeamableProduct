using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using Beamable.Editor.Utility;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

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
			if (_unityOtelManager.OtelFileStatus == null)
			{
				DrawBlockLoading("Fetching OTEL status...");
				return;
			}

			DrawTools(new CliWindowToolAction
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
				}
			);
			
			string lastPublished = _unityOtelManager.OtelFileStatus.LastPublishTimestamp == 0
				? "Never"
				: DateTimeOffset.FromUnixTimeMilliseconds(_unityOtelManager.OtelFileStatus.LastPublishTimestamp).ToLocalTime().ToString("g");

			bool otelEnabled = CoreConfiguration.Instance.EnableOtel.HasValue
				? CoreConfiguration.Instance.EnableOtel.Value
				: _unityOtelManager.TelemetryEnabled;

			CoreConfiguration.Instance.EnableOtelAutoPublish = EditorGUILayout.Toggle("Auto-Publish", CoreConfiguration.Instance.EnableOtelAutoPublish, EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Otel Enabled: ", otelEnabled.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Last Published", lastPublished, EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Size:", $"{(float)_unityOtelManager.OtelFileStatus.FolderSize/(1024 * 1024):F} MB", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Log Files Count: ", _unityOtelManager.OtelFileStatus.FileCount.ToString(), EditorStyles.boldLabel);
			var hasCollectors = _unityOtelManager.CollectorStatus != null && _unityOtelManager.CollectorStatus.collectorsStatus.Count > 0;
			EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
			EditorGUILayout.LabelField("Collectors", EditorStyles.boldLabel);
			if (hasCollectors)
			{
				DrawJsonBlock(_unityOtelManager.CollectorStatus);
			}
			else
			{
				EditorGUILayout.LabelField("There are no collectors running...");
			}
		}
		
	}
	
}
