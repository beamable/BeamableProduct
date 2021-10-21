
using System;
using System.IO;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;

namespace Beamable
{
   public static class BeamableEnvironment
   {
      private const string FilePath = "Packages/com.beamable/Runtime/Environment/env-default.json";

      private const string ENV_STAGING = "staging";
      private const string ENV_DEV = "dev";
      private const string ENV_PROD = "prod";

      static BeamableEnvironment()
      {
         // load the env on startup.
         ReloadEnvironment();
      }

#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
      [InitializeOnEnterPlayMode]
#endif
      static void Init()
      {
         Data = new EnvironmentData();
         ReloadEnvironment();
      }

      public static EnvironmentData Data { get; private set; } = new EnvironmentData();

      public static string ApiUrl => Data.ApiUrl;
      public static string PortalUrl => Data.PortalUrl;
      public static string Environment => Data.Environment;
      public static string SdkVersion => Data.SdkVersion;
      public static string DockerRegistryUrl => Data.DockerRegistryUrl;

      // See https://disruptorbeam.atlassian.net/browse/PLAT-3838
      public static string SocketUrl => $"{Data.ApiUrl.Replace("http://", "wss://").Replace("https://", "wss://")}/socket";
      public static string BeamServiceTag => $"{Environment}_{SdkVersion}";

      public static bool IsProduction => string.Equals(Environment, ENV_PROD);
      public static bool IsReleaseCandidate => string.Equals(Environment, ENV_STAGING);
      public static bool IsNightly => string.Equals(Environment, ENV_DEV);

      public static void ReloadEnvironment()
      {
         var envText = File.ReadAllText(FilePath);
         var rawDict = Json.Deserialize(envText) as ArrayDict;
         JsonSerializable.Deserialize(Data, rawDict);
      }
   }

   [Serializable]
   public class EnvironmentData : JsonSerializable.ISerializable
   {
      private const string BUILD__SDK__VERSION__STRING = "BUILD__SDK__VERSION__STRING";

      public string Environment;
      public string ApiUrl;
      public string PortalUrl;
      public string SdkVersion;
      public string DockerRegistryUrl;
      public void Serialize(JsonSerializable.IStreamSerializer s)
      {
         s.Serialize("environment", ref Environment);
         s.Serialize("apiUrl", ref ApiUrl);
         s.Serialize("portalUrl", ref PortalUrl);
         s.Serialize("sdkVersion", ref SdkVersion);
         s.Serialize("dockerRegistryUrl", ref DockerRegistryUrl);

         if (SdkVersion.Equals(BUILD__SDK__VERSION__STRING))
         {
            SdkVersion = "0.0.0";
         }
      }
   }
}