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
			if (_unityOtelManager.OtelStatus == null)
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
				}, new CliWindowToolAction
				{
					name = "Update OTEL Data",
					onClick = () =>
					{
						_ = Scope.GetService<UnityOtelManager>().FetchOtelStatus();
					}
				}
			);
			
			string lastPublished = _unityOtelManager.OtelStatus.LastPublishTimestamp == 0
				? "Never"
				: DateTimeOffset.FromUnixTimeMilliseconds(_unityOtelManager.OtelStatus.LastPublishTimestamp).ToLocalTime().ToString("g");
			CoreConfiguration.Instance.EnableOtelAutoPublish = EditorGUILayout.Toggle("Auto-Publish", CoreConfiguration.Instance.EnableOtelAutoPublish, EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Last Published", lastPublished, EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Size:", $"{(float)_unityOtelManager.OtelStatus.FolderSize/(1024 * 1024):F} MB", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Log Files Count: ", _unityOtelManager.OtelStatus.FileCount.ToString(), EditorStyles.boldLabel);
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
