//
// #if UNITY_EDITOR
//
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEngine.SceneManagement;
//
// public partial class LightBeamSceneConfigObject
// {
// 	public List<LightBeamEditorScene> editorScenes;
//
// 	private void OnValidate()
// 	{
// 		scenes = editorScenes.Select(x =>
// 		{
// 			var scene = new LightBeamRuntimeScene {label = x.name};
// 			if (x.scene)
// 			{
// 				scene.sceneName = x.scene.name;
// 				scene.scenePath = AssetDatabase.GetAssetPath(x.scene);
// 			}
//
// 			return scene;
// 		}).ToList();
// 	}
// }
//
// // [Serializable]
// // public struct LightBeamEditorScene
// // {
// // 	public string name;
// // 	public SceneAsset scene;
// // }
//
// #endif
