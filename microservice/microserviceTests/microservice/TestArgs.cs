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
   }
}