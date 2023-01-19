using Beamable.Common.Content;
using Beamable.UI.Buss;
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
		public List<BussComponentCategoryLoaderElement> categories;

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
		public GameObject prefab; // TODO: could this be a specific type of a parent easy-feature?
	}

	[Serializable]
	public class BussComponentCategoryLoaderElement
	{
		public string category;
		public OptionalInt minComponentSpaceWidth;
		public List<BussComponentLoaderElement> components;
	}
	
	[Serializable]
	public class BussComponentLoaderElement
	{
		public string label;
		public BussElement prefab;
		public OptionalInt forcedWidth;
		public OptionalInt forcedHeight;
		public BussComponentLoaderElementVariant[] extraVariants;
	}

	[Serializable]
	public class BussComponentLoaderElementVariant
	{
		public string[] classesToAdd;
		public string[] classesToRemove;
	}
}
