﻿/// FROM https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/src/master/

using UnityEngine;

namespace Beamable.UnityEngineClone.UI.Extensions
{
	public static class ExtentionMethods
	{
		public static T GetOrAddComponent<T>(this GameObject child) where T : Component
		{
			T result = child.GetComponent<T>();
			if (result == null)
			{
				result = child.AddComponent<T>();
			}
			return result;
		}
	}
}
