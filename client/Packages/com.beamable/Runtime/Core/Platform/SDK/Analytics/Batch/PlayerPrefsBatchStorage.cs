using UnityEngine;

namespace Beamable.Api.Analytics.Batch
{
	/// <summary>The SDK's default, unencrypted persistence. Replace this service to opt into secure storage.</summary>
	public sealed class PlayerPrefsBatchStorage : IAnalyticsBatchStorage
	{
		public bool TryGetString(string key, out string value)
		{
			value = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
			return value != null;
		}

		public bool SetString(string key, string value)
		{
			PlayerPrefs.SetString(key, value);
			return true;
		}

		public bool Flush()
		{
			PlayerPrefs.Save();
			return true;
		}
	}
}
