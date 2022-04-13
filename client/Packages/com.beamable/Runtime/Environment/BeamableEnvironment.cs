using Beamable.Common;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using System;
using System.IO;
using UnityEngine;

namespace Beamable
{
	public static class BeamableEnvironment
	{
		public const string FilePath = "Packages/com.beamable/Runtime/Environment/Resources/env-default.json";
		private const string ResourcesPath = "env-default";

		private const string ENV_STAGING = "staging";
		private const string ENV_DEV = "dev";
		private const string ENV_PROD = "prod";

		static BeamableEnvironment()
		{
			// load the env on startup.
			ReloadEnvironment();
		}

#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
      [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
		static void Init()
		{
			Data = new EnvironmentData();
			ReloadEnvironment();
		}

		public static EnvironmentData Data { get; private set; } = new EnvironmentData();

		public static string ApiUrl => Data.ApiUrl;
		public static string PortalUrl => Data.PortalUrl;
		public static string BeamMongoExpressUrl => Data.BeamMongoExpressUrl;
		public static string Environment => Data.Environment;
		public static PackageVersion SdkVersion => Data.SdkVersion;
		public static string DockerRegistryUrl => Data.DockerRegistryUrl;
		public static bool IsUnityVsp => Data.IsUnityVsp;
		public static string UnityVspId => Data.UnityVspId;

		// See https://disruptorbeam.atlassian.net/browse/PLAT-3838
		public static string SocketUrl => $"{Data.ApiUrl.Replace("http://", "wss://").Replace("https://", "wss://")}/socket";
		public static string BeamServiceTag => $"{Environment}_{SdkVersion}";

		public static bool IsProduction => string.Equals(Environment, ENV_PROD);
		public static bool IsReleaseCandidate => string.Equals(Environment, ENV_STAGING);
		public static bool IsNightly => string.Equals(Environment, ENV_DEV);

		public static void ReloadEnvironment()
		{
			string envText = "";
#if UNITY_EDITOR
			envText = File.ReadAllText(FilePath);
#else
			envText = Resources.Load<TextAsset>(ResourcesPath).text;
#endif
			var rawDict = Json.Deserialize(envText) as ArrayDict;
			JsonSerializable.Deserialize(Data, rawDict);
		}
	}

	[Serializable]
	public class EnvironmentData : JsonSerializable.ISerializable
	{
		[SerializeField] private string environment;
		[SerializeField] private string apiUrl;
		[SerializeField] private string portalUrl;
		[SerializeField] private string beamMongoExpressUrl;
		[SerializeField] private string sdkVersion;
		[SerializeField] private string dockerRegistryUrl;
		[SerializeField] private bool isUnityVsp;
		[SerializeField] private string unityVspId;

		private PackageVersion _version;

		public string Environment => environment;
		public string ApiUrl => apiUrl;
		public string PortalUrl => portalUrl;
		public string BeamMongoExpressUrl => beamMongoExpressUrl;
		public PackageVersion SdkVersion => _version ?? (_version = sdkVersion);
		public string DockerRegistryUrl => dockerRegistryUrl;
		public bool IsUnityVsp => isUnityVsp;
		public string UnityVspId => unityVspId;

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize("environment", ref environment);
			s.Serialize("apiUrl", ref apiUrl);
			s.Serialize("portalUrl", ref portalUrl);
			s.Serialize("sdkVersion", ref sdkVersion);
			s.Serialize("beamMongoExpressUrl", ref beamMongoExpressUrl);
			s.Serialize("dockerRegistryUrl", ref dockerRegistryUrl);
			s.Serialize("unityVspId", ref unityVspId);

			isUnityVsp = !unityVspId.Equals(Constants.Environment.UNITY__VSP__UID);
			if (sdkVersion.Equals(Constants.Environment.BUILD__SDK__VERSION__STRING))
			{
				sdkVersion = "0.0.0";
			}
		}
	}
}
