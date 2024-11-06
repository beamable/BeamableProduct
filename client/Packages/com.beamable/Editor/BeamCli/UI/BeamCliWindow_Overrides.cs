using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	public partial class BeamCliWindow
	{
		[NonSerialized]
		public SerializedObject serializedSettings;

		[NonSerialized]
		public bool hasSerializedChanges;

		void OnOverridesGui()
		{
			if (serializedSettings == null)
			{
				var settings = ActiveContext.ServiceScope.GetService<BeamWebCommandFactoryOptions>();
				var json = JsonUtility.ToJson(settings);
				var clone = ScriptableObject.CreateInstance<BeamWebCommandFactoryOptions>();
				JsonUtility.FromJsonOverwrite(json, clone);
				serializedSettings = new SerializedObject(clone);
			}

			var mainStyle = new GUIStyle();
			mainStyle.padding = new RectOffset(30, 30, 12, 0);

			var labelStyle = new GUIStyle(EditorStyles.label);
			// labelStyle.margin = new RectOffset(0, 0, 0, 0);
			labelStyle.padding = new RectOffset(0, 18, 0, 2);
			labelStyle.alignment = TextAnchor.LowerRight;
			EditorGUILayout.BeginVertical(mainStyle);
			{
				EditorGUI.BeginChangeCheck();

				EditorGUILayout.HelpBox(
					message: "Server logs, server events, and commands will be saved between " +
							 "domain reloads. These settings limit the number of instances that " +
							 "will be saved. The limits are in place to avoid infinite growth.",
					type: MessageType.Info);
				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.serverLogCap)));
				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.serverEventLogCap)));
				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.commandInstanceCap)));

				EditorGUILayout.Space(15, false);
				EditorGUILayout.HelpBox(
					message: "These settings control how the CLI server resolution happens. ",
					type: MessageType.Info);

				EditorGUILayout.SelectableLabel(BeamCliUtil.CLI_PATH.ToLowerInvariant(), labelStyle);
				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.ownerOverride)));


				EditorGUILayout.SelectableLabel(BeamCliUtil.CLI_VERSION, labelStyle);
				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.versionOverride)));

				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.selfDestructOverride)),
					new GUIContent("Destruct Timer (s)", null, "a value of 0 means there is no timeout."));
				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.startPortOverride)));
				EditorGUILayout.PropertyField(
					serializedSettings.FindProperty(nameof(BeamWebCommandFactoryOptions.port)));

				if (EditorGUI.EndChangeCheck())
				{
					hasSerializedChanges = true;
				}



				GUI.enabled = hasSerializedChanges;
				bool apply = false, revert = false;
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.Space(1, true);
					EditorGUILayout.Space(1, true);
					EditorGUILayout.Space(1, true);
					EditorGUILayout.Space(1, true);
					EditorGUILayout.Space(1, true);
					EditorGUILayout.Space(1, true);
					revert = GUILayout.Button("Revert");
					apply = GUILayout.Button("Apply");
				}
				EditorGUILayout.EndHorizontal();
				GUI.enabled = true;

				if (apply)
				{
					serializedSettings.ApplyModifiedProperties();


					var content = JsonUtility.ToJson(serializedSettings.targetObject);
					var settings = ActiveContext.ServiceScope.GetService<BeamWebCommandFactoryOptions>();

					JsonUtility.FromJsonOverwrite(content, settings);
				}

				if (apply || revert)
				{
					hasSerializedChanges = false;
					serializedSettings = null;
					GUIUtility.hotControl = 0;
					GUIUtility.keyboardControl = 0;
				}

			}
			EditorGUILayout.EndVertical();
		}
	}
}
