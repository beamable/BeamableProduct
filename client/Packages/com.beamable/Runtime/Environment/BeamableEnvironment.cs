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

		/// <inheritdoc cref="EnvironmentData.ApiUrl"/>
		public static string ApiUrl => Data.ApiUrl;

		/// <inheritdoc cref="EnvironmentData.PortalUrl"/>
		public static string PortalUrl => Data.PortalUrl;

		/// <inheritdoc cref="EnvironmentData.BeamMongoExpressUrl"/>
		public static string BeamMongoExpressUrl => Data.BeamMongoExpressUrl;

		/// <inheritdoc cref="EnvironmentData.Environment"/>
		public static string Environment => Data.Environment;

		/// <inheritdoc cref="EnvironmentData.SdkVersion"/>
		public static PackageVersion SdkVersion => Data.SdkVersion;

		/// <inheritdoc cref="EnvironmentData.DockerRegistryUrl"/>
		public static string DockerRegistryUrl => Data.DockerRegistryUrl;

		/// <inheritdoc cref="EnvironmentData.IsUnityVsp"/>
		public static bool IsUnityVsp => Data.IsUnityVsp;

		// See https://disruptorbeam.atlassian.net/browse/PLAT-3838
		/// <summary>
		/// The websocket address for Beamable
		/// </summary>
		public static string SocketUrl => $"{Data.ApiUrl.Replace("http://", "wss://").Replace("https://", "wss://")}/socket";

		/// <summary>
		/// The Docker tag for the beam service base image that will be used to build microservices.
		/// </summary>
		public static string BeamServiceTag => $"{Environment}_{SdkVersion}";

		/// <summary>
		/// True if the <see cref="Environment"/> property is "prod"
		/// </summary>
		public static bool IsProduction => string.Equals(Environment, ENV_PROD);

		/// <summary>
		/// True if the <see cref="Environment"/> property is "staging"
		/// </summary>
		public static bool IsReleaseCandidate => string.Equals(Environment, ENV_STAGING);

		/// <summary>
		/// True if the <see cref="Environment"/> property is "dev"
		/// </summary>
		public static bool IsNightly => string.Equals(Environment, ENV_DEV);

		/// <summary>
		/// Read the data from the env-default.json file in Resources and reload all environment properties.
		/// This function is called automatically by Beamable's initialization process. You shouldn't need to call it unless
		/// you have somehow modified the embedded resource file.
		/// </summary>
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
		[SerializeField] private string isUnityVspStr;

		private PackageVersion _version;

		/// <summary>
		/// The Beamable Cloud environment the game is using. For games, this should always be "prod"
		/// </summary>
		public string Environment => environment;

		/// <summary>
		/// The Beamable Cloud API url.
		/// </summary>
		public string ApiUrl => apiUrl;

		/// <summary>
		/// The Beamable Portal url
		/// </summary>
		public string PortalUrl => portalUrl;

		/// <summary>
		/// The Beamable Mongo Express url
		/// </summary>
		public string BeamMongoExpressUrl => beamMongoExpressUrl;

		/// <summary>
		/// The currently installed <see cref="PackageVersion"/> for Beamable
		/// </summary>
		public PackageVersion SdkVersion => _version ?? (_version = sdkVersion);

		/// <summary>
		/// The Beamable Docker Registry url
		/// </summary>
		public string DockerRegistryUrl => dockerRegistryUrl;

		/// <summary>
		/// True if the current version of Beamable was installed via the Unity Asset Store; false otherwise.
		/// </summary>
		public bool IsUnityVsp => isUnityVsp;


		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize("environment", ref environment);
			s.Serialize("apiUrl", ref apiUrl);
			s.Serialize("portalUrl", ref portalUrl);
			s.Serialize("sdkVersion", ref sdkVersion);
			s.Serialize("beamMongoExpressUrl", ref beamMongoExpressUrl);
			s.Serialize("dockerRegistryUrl", ref dockerRegistryUrl);
			s.Serialize("isUnityVsp", ref isUnityVspStr);
			bool.TryParse(isUnityVspStr, out isUnityVsp);

			if (sdkVersion.Equals(Constants.Environment.BUILD__SDK__VERSION__STRING))
			{
				sdkVersion = "0.0.0";
			}
		}
	}
}
