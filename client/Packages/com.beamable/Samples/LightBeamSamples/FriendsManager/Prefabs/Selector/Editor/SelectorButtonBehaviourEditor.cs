using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine.UI;


[CustomEditor(typeof(SelectorButtonBehaviour))]
public class SelectorButtonBehaviourEditor: ButtonEditor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
			
		var button = (SelectorButtonBehaviour)target;
			
		button.selectedImage =
			(Image)EditorGUILayout.ObjectField("Selector Image:", button.selectedImage, typeof(Image), true);

		if (GUI.changed)
		{
			EditorUtility.SetDirty(button);
			EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
		}
	}
}

