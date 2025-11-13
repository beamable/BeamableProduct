
// using UnityEditor;

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

[CreateAssetMenu]
public class LightBeamSceneConfigObject : ScriptableObject
{
	// [HideInInspector]
	public List<LightBeamRuntimeScene> scenes;

	public bool TryGetScene(string name, out LightBeamRuntimeScene scene)
	{
		scene = default;
		foreach (var x in scenes)
		{
			if (string.Equals(x.label, name))
			{
				scene = x;
				return true;
			}
		}

		return false;
	}

#if UNITY_EDITOR
	public List<LightBeamEditorScene> editorScenes;

	private void OnValidate()
	{
		scenes = editorScenes.Select(x =>
		{
			var scene = new LightBeamRuntimeScene {label = x.name};
			if (x.scene)
			{
				scene.sceneName = x.scene.name;
				scene.about = x.about;
				scene.title = x.title;
				scene.includeInToc = x.includeInToc;
				scene.realmRequirements = x.realmRequirements;
				scene.scenePath = AssetDatabase.GetAssetPath(x.scene);
			}

			return scene;
		}).ToList();
	}
#endif

}

[Serializable]
public struct LightBeamRuntimeScene
{
	public string label;
	public string about;
	public string title;
	public string sceneName;
	public string scenePath;
	public bool includeInToc;
	public TextAsset realmRequirements;
}

#if UNITY_EDITOR
[Serializable]
public struct LightBeamEditorScene
{
	public string name;
	public string title;
	public bool includeInToc;
	public TextAsset realmRequirements;
	[TextArea]
	public string about;
	public SceneAsset scene;
}

#endif
