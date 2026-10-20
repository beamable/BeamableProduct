using Beamable.Content;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	// Confirmation owns no watcher lifecycle: changing the existing setting is observed by CliContentService.
	internal sealed class ContentLocalModeWindow : EditorWindow
	{
		internal const string SuppressionKey = "Beamable.Content.LocalMode.SuppressWarning";
		internal const string Warning = "Local Mode On. Microservices and some beamable service can still use published content";
		private ContentConfiguration _configuration;
		private bool _suppress;
		private GUIStyle _bodyStyle;

		internal static void RequestEnable(ContentConfiguration configuration, Rect ownerPosition)
		{
			if (configuration == null || configuration.EnableLocalContentInEditor) return;
			if (SessionState.GetBool(SuppressionKey, false))
			{
				SetEnabled(configuration, true);
				return;
			}
			var window = CreateInstance<ContentLocalModeWindow>();
			window._configuration = configuration;
			window.titleContent = new GUIContent("Enable Local Mode?");
			window.minSize = window.maxSize = new Vector2(520, 320);
			window.position = new Rect(ownerPosition.center - window.minSize * .5f, window.minSize);
			window.ShowModalUtility();
		}

		internal static void SetEnabled(ContentConfiguration configuration, bool enabled)
		{
			if (configuration == null || configuration.EnableLocalContentInEditor == enabled) return;
			var serialized = new SerializedObject(configuration);
			serialized.FindProperty(nameof(ContentConfiguration.EnableLocalContentInEditor)).boolValue = enabled;
			serialized.ApplyModifiedProperties();
			// Save just this configuration, never unrelated dirty scenes or assets.
			if (AssetDatabase.Contains(configuration)) AssetDatabase.SaveAssetIfDirty(configuration);
		}

		private static void ApplyChoice(ContentConfiguration configuration, bool confirmed, bool suppress)
		{
			if (!confirmed || configuration == null) return;
			SetEnabled(configuration, true);
			if (suppress) SessionState.SetBool(SuppressionKey, true);
		}

		private void OnGUI()
		{
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
			{
				Event.current.Use();
				Close();
				return;
			}
			_bodyStyle ??= new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 12 };
			GUILayout.Space(14);
			GUILayout.BeginHorizontal();
			GUILayout.Space(18);
			GUILayout.BeginVertical();
			GUILayout.Label("Enable Local Mode?", EditorStyles.boldLabel);
			GUILayout.Space(10);
			GUILayout.Label("The Unity client uses your local content snapshot for the selected namespace, including unpublished edits. Auto-sync pauses; History continues receiving publications.", _bodyStyle);
			GUILayout.Space(10);
			EditorGUILayout.HelpBox("Microservices, inventory, and commerce still use published realm content. Client and server values can differ.", MessageType.Warning);
			GUILayout.Space(10);
			GUILayout.Label("Publishing still changes the realm. Restart Play Mode after changing the content source.", _bodyStyle);
			GUILayout.FlexibleSpace();
			_suppress = EditorGUILayout.ToggleLeft("Don’t show again until Unity restarts", _suppress);
			GUILayout.Space(12);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Cancel", GUILayout.Width(95), GUILayout.Height(26))) Close();
			if (GUILayout.Button("Enable Local Mode", GUILayout.Width(155), GUILayout.Height(26)))
			{
				ApplyChoice(_configuration, true, _suppress);
				Close();
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(16);
			GUILayout.EndVertical();
			GUILayout.Space(18);
			GUILayout.EndHorizontal();
		}
	}
}
