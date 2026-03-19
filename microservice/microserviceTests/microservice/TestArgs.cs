using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Api.Logs;
using Beamable.Server.Api.RealmConfig;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using microserviceTests.microservice.Util;
using Microsoft.Extensions.Logging;

namespace microserviceTests.microservice
{

	public class TestSetup
	{
		private readonly IConnectionProvider _provider;
		public IBeamableService Service { get; set; }
		private IContentResolver _resolver;
		private MicroserviceResult _instances;

		public bool HasInitialized => Service.HasInitialized;
		public TestSetup(IConnectionProvider provider, IContentResolver resolver=null)
		{
			_resolver = resolver;
			_provider = provider;
		}

		public async Task Start<T>(TestArgs dudArgs=null, Action<IDependencyBuilder> configurator=null) where T : Microservice
		{
			var args = new TestArgs();
		

			var attr = typeof(T).GetCustomAttribute<MicroserviceAttribute>();
			_instances = await new BeamServiceConfigBuilder(attr)
				.IncludeRoutes<T>(routePrefix: "") // for legacy reasons; all our tests are written without a prefix.
				.ConfigureServices(builder =>
				{
					// builder.RemoveIfExists<ReflectionCache>();
					// var reflectionCache = MicroserviceBootstrapper.ConfigureReflectionCache();
					// builder.AddSingleton<ReflectionCache>(reflectionCache);
					{ 
						// the activity provider will produce no-op activies unless 
						//  the otel is _also_ set up; which for tests, we don't want
						//  to do.
						builder.RemoveIfExists<IActivityProvider>();
						builder.AddSingleton<IActivityProvider, NoopActivityProvider>();
						// conf.Builder.AddSingleton<DefaultActivityProvider>(p => 
						// 	new DefaultActivityProvider(p.GetService<IMicroserviceArgs>(), p.GetService<MicroserviceAttribute>()));
					}
					{ 
						// need to inject a custom log factory to talk to the test logs
						builder.RemoveIfExists<ILoggerFactory>();
						builder.AddSingleton<ILoggerFactory>(LoggingUtil.testFactory);

					}
					// conf.Builder.RemoveIfExists<SocketRequesterContext>();
					// conf.Builder.AddSingleton(_ => Service.SocketContext);
					builder.RemoveIfExists<IRealmConfigService>();
					builder.RemoveIfExists<IMicroserviceRealmConfigService>();
					builder.RemoveIfExists<RealmConfigService>();
					builder.RemoveIfExists<ILoggingContextService>();
					builder.AddSingleton<MockRealmConfig>();
					builder.AddSingleton<IMicroserviceRealmConfigService>(p => p.GetService<MockRealmConfig>());
					builder.AddSingleton<IRealmConfigService>(p => p.GetService<MockRealmConfig>());
					builder.AddSingleton<ILoggingContextService>(_ => new MockLoggingContextService());
					builder.RemoveIfExists<IConnectionProvider>();
					builder.AddSingleton(_provider);
					
					builder.RemoveIfExists<IContentResolver>();
					builder.AddSingleton(_resolver);
					configurator?.Invoke(builder);

				})
				.OverrideConfig(c =>
				{
					c.Args = args;
					c.LogFactory = () => LoggingUtil.testLogger;
					c.FirstConnectionHandler = (service) =>
					{
						Service = service;
					};
				})
				.Run();
			
		}

		public async Task OnShutdown(object sender, EventArgs args)
		{
			Service.OnShutdown(sender, args);
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
	
	public class TestArgs : IMicroserviceArgs
   {
	   
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
      public string OapiGenLogLevel => "debug";
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
      public bool DisableOutboundWsCompression => false;
      public string MetadataUrl { get; }
	  public string RefreshToken { get; }
	  public long AccountId => 0;
	  public string AccountEmail => "test@test.test";
	  public int RequireProcessId { get; }
	  public string OtelExporterOtlpProtocol { get; }
	  public string OtelExporterOtlpEndpoint { get; }
	  public string OtelExporterOtlpHeaders { get; }
	  public bool UseLocalOtel => false;
	  public bool OtelExporterShouldRetry { get; }
	  public bool OtelExporterStandardEnabled => false;
	  public string OtelExporterRetryMaxSize { get; }
	  public bool AllowStartupWithoutBeamableSettings => false;
	  public bool SkipLocalEnv => true;
	  public bool SkipAliasResolve => true;

	  public void SetResolvedCid(string resolvedCid)
	  {
		  throw new NotImplementedException();
	  }
   }
}
