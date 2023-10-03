
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
	public string sceneName;
	public string scenePath;
}

#if UNITY_EDITOR
[Serializable]
public struct LightBeamEditorScene
{
	public string name;
	public SceneAsset scene;
}

#endif
