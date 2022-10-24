using System;
using System.IO;

namespace Beamable.Server
{
   public interface IMicroserviceArgs : IRealmInfo
   {
      string Host { get; }
      string Secret { get; }
      string NamePrefix { get; }
      string SdkVersionBaseBuild { get; }
      string SdkVersionExecution { get; }
      bool WatchToken { get; }
      public bool DisableCustomInitializationHooks { get; }
      public bool EmitOtel { get; }
      public bool EmitOtelMetrics { get; }
      public bool OtelMetricsIncludeRuntimeInstrumentation { get; }
      public bool OtelMetricsIncludeProcessInstrumentation { get; }
   }

   public class MicroserviceArgs : IMicroserviceArgs
   {
      public string CustomerID { get; set; }
      public string ProjectName { get; set; }
      public string Secret { get; set; }
      public string Host { get; set; }
      public string NamePrefix { get; set; }
      public string SdkVersionBaseBuild { get; set; }
      public string SdkVersionExecution { get; set; }
      public bool WatchToken { get; set; }
      public bool DisableCustomInitializationHooks { get; set; }
      public bool EmitOtel { get; set; }
      public bool EmitOtelMetrics { get; set; }
      public bool OtelMetricsIncludeRuntimeInstrumentation { get; set; }
      public bool OtelMetricsIncludeProcessInstrumentation { get; set; }
   }

   public static class MicroserviceArgsExtensions
   {
      public static IMicroserviceArgs Copy(this IMicroserviceArgs args)
      {
         return new MicroserviceArgs
         {
            CustomerID = args.CustomerID,
            ProjectName = args.ProjectName,
            Secret = args.Secret,
            Host = args.Host,
            NamePrefix = args.NamePrefix,
            SdkVersionBaseBuild = args.SdkVersionBaseBuild,
            SdkVersionExecution = args.SdkVersionExecution,
            WatchToken = args.WatchToken,
            DisableCustomInitializationHooks = args.DisableCustomInitializationHooks,
            EmitOtel = args.EmitOtel,
            EmitOtelMetrics = args.EmitOtelMetrics,
            OtelMetricsIncludeProcessInstrumentation = args.OtelMetricsIncludeProcessInstrumentation,
            OtelMetricsIncludeRuntimeInstrumentation = args.OtelMetricsIncludeRuntimeInstrumentation
         };
      }
   }

   public class EnviornmentArgs : IMicroserviceArgs
   {
      public string CustomerID => Environment.GetEnvironmentVariable("CID");
      public string ProjectName => Environment.GetEnvironmentVariable("PID");
      public string Host => Environment.GetEnvironmentVariable("HOST");
      public string Secret => Environment.GetEnvironmentVariable("SECRET");
      public string NamePrefix => Environment.GetEnvironmentVariable("NAME_PREFIX") ?? "";
      public string SdkVersionExecution => Environment.GetEnvironmentVariable("BEAMABLE_SDK_VERSION_EXECUTION") ?? "";
      public bool WatchToken => (Environment.GetEnvironmentVariable("WATCH_TOKEN")?.ToLowerInvariant() ?? "") == "true";
      public bool DisableCustomInitializationHooks => (Environment.GetEnvironmentVariable("DISABLE_CUSTOM_INITIALIZATION_HOOKS")?.ToLowerInvariant() ?? "") == "true";
      public bool EmitOtel => (Environment.GetEnvironmentVariable("EMIT_OTEL")?.ToLowerInvariant() ?? "") == "false";
      public bool EmitOtelMetrics => (Environment.GetEnvironmentVariable("EMIT_OTEL_METRICS")?.ToLowerInvariant() ?? "") == "false";
      public bool OtelMetricsIncludeRuntimeInstrumentation => (Environment.GetEnvironmentVariable("OTEL_INCLUDE_RUNTIME_INSTRUMENTATION")?.ToLowerInvariant() ?? "") == "true";
      public bool OtelMetricsIncludeProcessInstrumentation => (Environment.GetEnvironmentVariable("OTEL_INCLUDE_PROCESS_INSTRUMENTATION")?.ToLowerInvariant() ?? "") == "true";
      public string SdkVersionBaseBuild => File.ReadAllText(".beamablesdkversion").Trim();
   }
}
