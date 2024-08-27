using Beamable.Common.Dependencies;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
	public class SessionStorageLayer : IStorageLayer
	{
		public void Save<T>(string key, T content)
		{
			var json = JsonUtility.ToJson(content);
			var fileName = GetSessionKey(key);
			SessionState.SetString(fileName, json);
		}

		public void Apply<T>(string key, T instance)
		{
			var fileName = GetSessionKey(key);
			var json = SessionState.GetString(fileName, null);
			if (string.IsNullOrEmpty(json)) return;
			JsonUtility.FromJsonOverwrite(json, instance);
		}

		private string GetSessionKey(string key)
		{
			return "beamable-session-data-" + key;
		}
	}
}
