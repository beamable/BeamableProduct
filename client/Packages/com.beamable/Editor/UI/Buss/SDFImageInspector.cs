using System.Linq;
using Beamable.UI.SDF;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.Editor.UI.SDF
{
	[CustomEditor(typeof(SDFImage))]
	public class SDFImageInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sprite"), new GUIContent("Sprite"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"), new GUIContent("Color"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("threshold"), new GUIContent("Threshold"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundTexture"),
			                              new GUIContent("Background"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("meshFrame"), new GUIContent("Frame"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Type"), new GUIContent("Type"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("rounding"), new GUIContent("Round Corners"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Outline");
			EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineWidth"),
			                              new GUIContent("Outline Width"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineColor"),
			                              new GUIContent("Outline Color"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Shadow");
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowColor"), new GUIContent("Shadow Color"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowThreshold"),
			                              new GUIContent("Shadow Threshold"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowOffset"),
			                              new GUIContent("Shadow Offset"));

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				foreach (var sdfImage in serializedObject.targetObjects.Cast<SDFImage>())
				{
					sdfImage.Rebuild(CanvasUpdate.Layout);
				}
			}
		}
	}
}
