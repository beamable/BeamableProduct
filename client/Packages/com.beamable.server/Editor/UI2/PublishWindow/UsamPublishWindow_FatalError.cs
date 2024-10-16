using Beamable.Editor.BeamCli.UI;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.PublishWindow
{
	public partial class UsamPublishWindow
	{
		private Vector2 _errorScroll;
		void DrawFatalError()
		{
			DrawHeader("A fatal error has occured! Please contact Beamable.");
			
			EditorGUILayout.LabelField("Error: ");
			_errorScroll = EditorGUILayout.BeginScrollView(_errorScroll, false, true, GUILayout.Height(400));
			EditorGUILayout.BeginVertical();
			BeamCliWindow.DrawJsonBlock(_fatalError);
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();


			DoFlexHeight();

			{
				// render the action buttons

				EditorGUILayout.BeginHorizontal(new GUIStyle {padding = new RectOffset(padding, padding, 0, 0)});

				GUILayout.FlexibleSpace();

				var btnStyle = new GUIStyle(GUI.skin.button) {padding = new RectOffset(6, 6, 6, 6)};

				isCancelPressed = GUILayout.Button("Okay", btnStyle);
				EditorGUILayout.EndHorizontal();

			}
		}
	}
}
