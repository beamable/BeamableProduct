using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{

	// public class CustomImporter : AssetPostprocessor
	// {
	// 	void OnPreprocessAsset()
	// 	{
	// 		if (assetImporter is BeamServiceImporter)
	// 		{
	// 			var customImporter = (BeamServiceImporter)assetImporter;
	// 			customImporter.
	// 			customImporter.isReadOnly = false;
	// 		}
	// 	}
	// }
	
	// [CustomEditor(typeof(BeamServiceAsset), true)]
	// public class BeamServiceImporterEditor : UnityEditor.Editor
	// {
	// 	public override void OnInspectorGUI()
	// 	{
	// 		var rootProperty = serializedObject.FindProperty(nameof(BeamServiceAsset.data));
	// 		var property = rootProperty;
	// 		property.NextVisible(true);
	//
	// 		do
	// 		{
	// 			EditorGUI.BeginChangeCheck();
	// 			EditorGUILayout.PropertyField(property);
	//
	// 			if (EditorGUI.EndChangeCheck())
	// 			{
	// 				Debug.Log("VALUE CHANGED!");
	// 				serializedObject.ApplyModifiedProperties();
	// 				var asset = (BeamServiceAsset)target;
	// 				// asset.Write();
	// 			}
	// 		} while (property.NextVisible(false));
	// 	}
	// 	
	//
	// }

}
