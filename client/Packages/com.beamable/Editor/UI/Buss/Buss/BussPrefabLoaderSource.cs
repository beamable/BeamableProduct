using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Buss
{
	[CreateAssetMenu]
	[Serializable]
	public class BussPrefabLoaderSource : ScriptableObject
	{
		public List<BussPrefabLoaderElement> easyFeatures;
	}

	public class BussPrefabLoaderSourceProvider
	{
		public BussPrefabLoaderSource LoadSource()
		{
			return AssetDatabase.LoadAssetAtPath<BussPrefabLoaderSource>("Packages/com.beamable/Editor/UI/Buss/Buss/bussPrefabLoader.asset");
		}
	}

	[Serializable]
	public class BussPrefabLoaderElement
	{
		public string label;
		public GameObject prefab;
	}
}
