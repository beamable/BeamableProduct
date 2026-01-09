using Beamable.Api;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using System;
using System.IO;
using UnityEngine;
using static Beamable.Common.Constants.Features.Environment;

namespace Beamable
{
	public static class BeamableEnvironment
	{
		public const string FilePath = "Packages/com.beamable/Runtime/Environment/Resources/env-default.json";
		public const string VersionPath = "Packages/com.beamable/Runtime/Environment/Resources/versions-default.json";
		private const string ResourcesPath = "env-default";
		private const string VersionsResourcePath = "versions-default";

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
			// Data = new EnvironmentData();
			ReloadEnvironment();
		}

		public static EnvironmentData Data { get; private set; } = new EnvironmentData();

		
		public static EnvironmentVersionData VersionData { get; private set; } = new EnvironmentVersionData();
		//
		/// <inheritdoc cref="EnvironmentData.ApiUrl"/>
		public static string ApiUrl => Beam.RuntimeConfigProvider.HostUrl;
		//
		/// <inheritdoc cref="EnvironmentData.PortalUrl"/>
		public static string PortalUrl => Beam.RuntimeConfigProvider.PortalUrl;
		
		/// <inheritdoc cref="EnvironmentData.Environment"/>
		public static string Environment => Data.Environment;
		//
		/// <inheritdoc cref="EnvironmentData.SdkVersion"/>
		public static PackageVersion SdkVersion => Data.SdkVersion;
		
		/// <summary>
		/// True if the <see cref="Environment"/> property is "prod"
		/// </summary>
		public static bool IsProduction => !SdkVersion.IsNightly && SdkVersion.Major != 0;
		
		/// <summary>
		/// True if the <see cref="Environment"/> property is "staging"
		/// </summary>
		public static bool IsReleaseCandidate => string.Equals(Environment, ENV_STAGING);
		
		/// <summary>
		/// True if the <see cref="Environment"/> property is "dev"
		/// </summary>
		public static bool IsNightly => string.Equals(Environment, ENV_DEV);
		
		/// <summary>
		/// The version of Beamable nuget packages that this Beamable SDK requires
		/// </summary>
		public static string NugetPackageVersion => VersionData.nugetPackageVersion;
		
		/// <summary>
		/// Returns if the SDK is being run in a beamable developer environment
		/// </summary>
		public static bool IsBeamableDeveloper => VersionData.nugetPackageVersion.Contains("0.0.123");
		
		
		/// <summary>
		/// Read the data from the env-default.json file in Resources and reload all environment properties.
		/// This function is called automatically by Beamable's initialization process. You shouldn't need to call it unless
		/// you have somehow modified the embedded resource file.
		/// </summary>
		public static void ReloadEnvironment()
		{
			string envText = string.Empty;
			string versionText = string.Empty;

#if UNITY_EDITOR
			envText = File.ReadAllText(FilePath);
			versionText = File.ReadAllText(VersionPath);
#else
			envText = Resources.Load<TextAsset>(ResourcesPath).text;
			versionText = Resources.Load<TextAsset>(VersionsResourcePath).text;
#endif

			var rawDict = Json.Deserialize(envText) as ArrayDict;

			JsonSerializable.Deserialize(Data, rawDict);
			VersionData = JsonUtility.FromJson<EnvironmentVersionData>(versionText);
		}
	}
	

	[Serializable]
	public class EnvironmentData : JsonSerializable.ISerializable
	{
		[SerializeField] private string sdkVersion;
		private PackageVersion _version;

		/// <summary>
		/// The Beamable Cloud environment the game is using. For games, this should always be "prod"
		/// </summary>
		public string Environment => _version.IsNightly ? "dev" : "prod";

		/// <summary>
		/// The currently installed <see cref="PackageVersion"/> for Beamable
		/// </summary>
		public PackageVersion SdkVersion => _version ?? (_version = sdkVersion);


		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize("sdkVersion", ref sdkVersion);
			if (sdkVersion.Equals(Constants.Environment.BUILD__SDK__VERSION__STRING))
			{
				sdkVersion = "0.0.0";
			}
		}
	}
}
