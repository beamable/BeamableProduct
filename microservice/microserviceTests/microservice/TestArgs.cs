using Beamable.Server;

namespace microserviceTests.microservice
{
   public class TestArgs : IMicroserviceArgs
   {
      public string CustomerID { get; set; } = "testcid";
      public string ProjectName { get; set; } = "testpid";
      public string Host { get; set; } = "testhost";
      public string Secret { get; set; } = "testsecret";
      public string NamePrefix { get; set; } = "";
      public string SdkVersionBaseBuild { get; set; } = "test";
      public string SdkVersionExecution { get; set; } = "test";
      public bool WatchToken { get; }
      public bool DisableCustomInitializationHooks { get; }
      public string LogLevel { get; } = "debug";
      public bool DisableLogTruncate { get; } = false;
      public int LogTruncateLimit { get; } = 1000;
      public int LogMaxCollectionSize { get; } = 5;
      public int LogMaxDepth { get; } = 3;
      public int LogDestructureMaxLength { get; } = 50;
      public bool RateLimitWebsocket { get; } = false;
      public int RateLimitWebsocketTokens { get; } = 10;
      public int RateLimitWebsocketPeriodSeconds { get; } = 1;
      public int RateLimitWebsocketTokensPerPeriod { get; } = 5;
      public int RateLimitWebsocketMaxQueueSize { get; } = 10;
      public double RateLimitCPUMultiplierLow => 0;
      public double RateLimitCPUMultiplierHigh => 0;
      public int RateLimitCPUOffset => 0;
      public int ReceiveChunkSize => 1024;
      public int SendChunkSize => 1024;
   }
}
