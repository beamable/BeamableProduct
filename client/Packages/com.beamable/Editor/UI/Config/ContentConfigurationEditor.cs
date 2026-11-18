using Beamable.Content;
using Beamable.Editor.ContentService;
using Beamable.Editor.UI.ContentWindow;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Config
{
	[CustomEditor(typeof(ContentConfiguration))]
	internal sealed class ContentConfigurationEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawLocalMode((ContentConfiguration)target);
			DrawDefaultInspector();
		}

		internal static void DrawLocalMode(ContentConfiguration configuration)
		{
			var local = configuration.EnableLocalContentInEditor;
			var requested = EditorGUILayout.Toggle(new GUIContent("Local Mode", "Saved for this developer and project only. Restart Play Mode after changing the source."), local);
			EditorGUILayout.HelpBox("Local preference for this developer and project. Not shared through Git. Restart Play Mode after changing it.", MessageType.Info);
			if (requested == local) return;
			EditorApplication.delayCall += async () =>
			{
				if (configuration == null) return;
				if (requested)
				{
					ContentLocalModeWindow.RequestEnable(configuration, new Rect(100, 100, 520, 400));
					return;
				}
				try
				{
					var context = BeamEditorContext.Default;
					await context.OnReady;
					ContentLocalModeExitWindow.Open(context.ServiceScope.GetService<CliContentService>(), configuration);
				}
				catch (System.Exception error)
				{
					EditorUtility.DisplayDialog("Local Mode is still on", error.Message, "Close");
				}
			};
		}
	}
}
