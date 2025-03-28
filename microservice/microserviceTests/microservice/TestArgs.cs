using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Api.RealmConfig;
using System;
using System.Threading.Tasks;
using microservice.Observability;
using microserviceTests.microservice.Util;
using Microsoft.Extensions.Logging;

namespace microserviceTests.microservice
{

	public class TestSetup
	{
		private readonly IConnectionProvider _provider;
		public BeamableMicroService Service;
		private IContentResolver _resolver;

		public bool HasInitialized => Service.HasInitialized;
		public TestSetup(IConnectionProvider provider, IContentResolver resolver=null)
		{
			_resolver = resolver;
			Service = new BeamableMicroService();
			_provider = provider;
			
		}

		public async Task Start<T>(TestArgs dudArgs=null) where T : Microservice
		{
			await Service.TestStart<T>(conf =>
			{
				{ 
					// the activity provider will produce no-op activies unless 
					//  the otel is _also_ set up; which for tests, we don't want
					//  to do.
					conf.Builder.RemoveIfExists<DefaultActivityProvider>();
					conf.Builder.AddSingleton<DefaultActivityProvider>(p => 
						new DefaultActivityProvider(p.GetService<IMicroserviceArgs>(), p.GetService<MicroserviceAttribute>()));
				}
				{ 
					// need to inject a custom log factory to talk to the test logs
					conf.Builder.RemoveIfExists<ILoggerFactory>();
					conf.Builder.AddSingleton<ILoggerFactory>(LoggingUtil.testFactory);

				}
				conf.Builder.RemoveIfExists<SocketRequesterContext>();
				conf.Builder.AddSingleton(_ => Service.SocketContext);
				conf.Builder.RemoveIfExists<IRealmConfigService>();
				conf.Builder.RemoveIfExists<IMicroserviceRealmConfigService>();
				conf.Builder.RemoveIfExists<RealmConfigService>();
				conf.Builder.AddSingleton<MockRealmConfig>();
				conf.Builder.AddSingleton<IMicroserviceRealmConfigService>(p => p.GetService<MockRealmConfig>());
				conf.Builder.AddSingleton<IRealmConfigService>(p => p.GetService<MockRealmConfig>());
				conf.Socket(_provider);
				conf.Content(_resolver);
			});
		}

		public async Task OnShutdown(object sender, EventArgs args)
		{
			await Service.OnShutdown(sender, args);
		}
	}
	
	public class TestArgConfig
	{
		public IDependencyBuilder Builder;

		public TestArgConfig Socket(IConnectionProvider socket)
		{
			Builder.RemoveIfExists<IConnectionProvider>();
			Builder.AddSingleton(socket);
			return this;
		}

		public TestArgConfig Content(IContentResolver resolver)
		{
			if (resolver == null) return this;
			Builder.RemoveIfExists<IContentResolver>();
			Builder.AddSingleton(resolver);
			return this;
		}

	}
	
	public static class TestServiceExtensions
	{
		public static async Task TestStart<T>(this BeamableMicroService service,
			Action<TestArgConfig> configurator = null) where T: Microservice
		{
			var args = TestArgs.Build<T>(configurator);
			await service.Start<T>(args);
		}
	}
	
	public class TestArgs : IMicroserviceArgs
   {
	   public static TestArgs Build<T>(Action<TestArgConfig> configurator = null) where T: Microservice
	   {
		   var args = new TestArgs();
		   MicroserviceBootstrapper._logger = LoggingUtil.testLogger;
		   var reflectionCache = MicroserviceBootstrapper.ConfigureReflectionCache();
		   var builder = MicroserviceBootstrapper.ConfigureServices<T>(args);
		   builder.RemoveIfExists<ReflectionCache>();
		   builder.AddSingleton<ReflectionCache>(reflectionCache);
		   var config = new TestArgConfig { Builder = builder };
		   configurator?.Invoke(config);
		   args.ServiceScope = builder.Build();
		   return args;
	   }
	   
      public string CustomerID { get; set; } = "testcid";
      public string ProjectName { get; set; } = "testpid";
      public IDependencyProviderScope ServiceScope { get; set; }
      public int HealthPort => 6565;
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
      public int SendChunkSize { get; set; } = 1024;
      public int BeamInstanceCount => 1;
      public int RequestCancellationTimeoutSeconds => 10;
      public LogOutputType LogOutputType => LogOutputType.DEFAULT;
      public string LogOutputPath { get; }
      public bool EnableDangerousDeflateOptions => false;
      public string MetadataUrl { get; }
	  public string RefreshToken { get; }
	  public long AccountId => 0;
	  public int RequireProcessId { get; }
	  public string OtelExporterOtlpProtocol { get; }
	  public string OtelExporterOtlpEndpoint { get; }
	  public string OtelExporterOtlpHeaders { get; }
	  public void SetResolvedCid(string resolvedCid)
	  {
		  throw new NotImplementedException();
	  }
   }
}
