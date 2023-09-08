
// using UnityEditor;

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu]
public partial class LightBeamSceneConfigObject : ScriptableObject
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
	
}

[Serializable]
public struct LightBeamRuntimeScene
{
	public string label;
	public string sceneName;
	public string scenePath;
}
