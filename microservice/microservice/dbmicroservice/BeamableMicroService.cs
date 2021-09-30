/*
 *  Simple CustomMicroservice example
 */
//#define DB_MICROSERVICE  // I sometimes enable this to see code better in rider


using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Server.Api;
using Beamable.Server.Api.Announcements;
using Beamable.Server.Api.Calendars;
using Beamable.Server.Api.Events;
using Beamable.Server.Api.Groups;
using Beamable.Server.Api.Inventory;
using Beamable.Server.Api.Leaderboards;
using Beamable.Server.Api.Mail;
using Beamable.Server.Api.Social;
using Beamable.Server.Api.Stats;
using Beamable.Server.Api.Tournament;
using Beamable.Server.Api.CloudData;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Api.Commerce;
using Core.Server.Common;
using microservice;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ContentService = Beamable.Server.Content.ContentService;
#if DB_MICROSERVICE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Stats;
using Beamable.Server.Api.Content;
using microservice.Common;
using Newtonsoft.Json;
using Serilog;

using Microsoft.Extensions.DependencyInjection;

namespace Beamable.Server
{

   public class MicroserviceNonceResponse
   {
      public string nonce;
   }

   public class MicroserviceAuthRequest
   {
      public string cid, pid, signature;
   }

   public class MicroserviceAuthResponse
   {
      public string result;
   }

   public class MicroserviceProviderRequest
   {
      public string name, type;
   }

   public class MicroserviceProviderResponse
   {

   }

   public class BeamableMicroService
   {
      public string MicroserviceName => _serviceAttribute.MicroserviceName;
      public string QualifiedName => $"{_args.NamePrefix}micro_{MicroserviceName}"; // scope everything behind a feature name "micro"

      public string PublicHost => $"{_args.Host.Replace("wss://", "https://").Replace("/socket", "")}/basic/{_args.CustomerID}.{_args.ProjectName}.{QualifiedName}/";

      private ConcurrentDictionary<long, Task> _runningTaskTable = new ConcurrentDictionary<long, Task>();
      private const int EXIT_CODE_PENDING_TASKS_STILL_RUNNING = 11;
      private const int HTTP_STATUS_GONE = 410;
      private const int ShutdownLimitSeconds = 5;
      private Promise<Unit> _serviceInitialized = new Promise<Unit>();
      private IConnection _connection;
      private readonly IConnectionProvider _connectionProvider;
      private readonly IContentResolver _contentResolver;
      private Promise<IConnection> _webSocketPromise;
      private MicroserviceRequester _requester;
      private SocketRequesterContext _socketRequesterContext;
      public ServiceMethodCollection ServiceMethods { get; private set; }
      private MicroserviceAttribute _serviceAttribute;

      // default is false, set 1 for true.
      private int _refuseNewClientMessageFlag = 0; // https://stackoverflow.com/questions/29411961/c-sharp-and-thread-safety-of-a-bool
      public bool RefuseNewClientMessages
      {
         get => (Interlocked.CompareExchange(ref _refuseNewClientMessageFlag, 1, 1) == 1);
         set
         {
            if (value) Interlocked.CompareExchange(ref _refuseNewClientMessageFlag, 1, 0);
            else Interlocked.CompareExchange(ref _refuseNewClientMessageFlag, 0, 1);
         }
      }

      // default is false, set 1 for true.
      private int _shutdownStarted = 0; // https://stackoverflow.com/questions/29411961/c-sharp-and-thread-safety-of-a-bool
      public bool IsShuttingDown
      {
         get => (Interlocked.CompareExchange(ref _shutdownStarted, 1, 1) == 1);
         set
         {
            if (value) Interlocked.CompareExchange(ref _shutdownStarted, 1, 0);
            else Interlocked.CompareExchange(ref _shutdownStarted, 0, 1);
         }
      }

      // TODO: XXX This is gross. Eventually, I'd like to have an IContentService get injected into a service provider, that the ContentRef can use
      public static ContentService _contentService;

      public bool HasInitialized { get; private set; }

      private IMicroserviceArgs _args;
      private string Host => _args.Host;
      public ServiceCollection ServiceCollection;
      private int[] _retryIntervalsInSeconds = new[]
      {
         5,
         5,
         10,
         10,
         15,
         45,
         60
      };

      private int _connectionAttempt = 0;

      public BeamableMicroService(IConnectionProvider socketProvider=null, IContentResolver contentResolver=null)
      {
         if (socketProvider == null)
         {
            socketProvider = new EasyWebSocketProvider();
         }

         if (contentResolver == null)
         {
            contentResolver = new DefaultContentResolver();
         }

         _connectionProvider = socketProvider;
         _contentResolver = contentResolver;
      }

      public async Task Start<TMicroService>(IMicroserviceArgs args)
         where TMicroService : Microservice
      {
         if (HasInitialized) return;

         _microserviceType = typeof(TMicroService);
         _serviceAttribute = _microserviceType.GetCustomAttribute<MicroserviceAttribute>();
         if (_serviceAttribute == null)
         {
            throw new Exception($"Cannot create service. Missing [{typeof(MicroserviceAttribute).Name}].");
         }

         _args = args.Copy();
         Log.Debug("Starting... {host} {prefix} {cid} {pid} {sdkVersionExecution} {sdkVersionBuild}", args.Host, args.NamePrefix, args.CustomerID, args.ProjectName, args.SdkVersionExecution, args.SdkVersionBaseBuild);



         XmlDocsHelper.ProvideXmlForBaseImage(typeof(AdminRoutes));
         XmlDocsHelper.ProvideXmlForService(_microserviceType);

         ServiceMethods = ServiceMethodHelper.Scan(_serviceAttribute, new ServiceMethodProvider
            {
               instanceType = typeof(AdminRoutes),
               factory = BuildAdminInstance,
               pathPrefix = "admin/"
            },
            new ServiceMethodProvider
            {
               instanceType = _microserviceType,
               factory = BuildServiceInstance,
               pathPrefix = ""
            });

         _socketRequesterContext = new SocketRequesterContext(GetWebsocketPromise);
         _requester = new MicroserviceRequester(_args, null, _socketRequesterContext);

         _contentService = new ContentService(_requester, _socketRequesterContext, _contentResolver);
         ContentApi.Instance.CompleteSuccess(_contentService);
         InitServices();

         _serviceInitialized.Then(_ =>
         {
            _contentService.Init();
         }).Error(ex =>
         {
            Log.Error("Service failed to initialize {message} {stack}", ex.Message, ex.StackTrace);
         });


         // Connect and Run
         _webSocketPromise = AttemptConnection();
         var socket = await _webSocketPromise;

         await SetupWebsocket(socket);
      }

      public void RunForever()
      {
         AppDomain.CurrentDomain.ProcessExit += async (sender, args) => await OnShutdown(sender, args);

         CancellationTokenSource cancelSource = new CancellationTokenSource();
         cancelSource.Token.WaitHandle.WaitOne();
      }

      public async Task OnShutdown(object sender, EventArgs args)
      {
         IsShuttingDown = true;

         // need to wait for all tasks to complete...
         Log.Debug("Shutdown started... {runningTaskCount} tasks running.", _runningTaskTable.Count);

         var sw = new System.Diagnostics.Stopwatch();
         sw.Start();

         // remove the service, so that no new messages are accepted
         try
         {
            var promise = RemoveService(QualifiedName);
            var startedWaitingAt = sw.ElapsedMilliseconds;
            while (!promise.IsCompleted)
            {
               if (sw.ElapsedMilliseconds > startedWaitingAt + 1000 * 5)
               {
                  throw new Exception("Waited for 5 seconds");
               }
               Thread.Sleep(5);
            }
         }
         catch (Exception ex)
         {
            Log.Fatal("Could not drain service provider. {message}", ex.Message);
         }


         var startTime = sw.ElapsedMilliseconds;
         var expectedEndTime = startTime + ShutdownLimitSeconds * 1000;
         while (_runningTaskTable.Count > 0 && sw.ElapsedMilliseconds < expectedEndTime)
         {
            var millisecondsLeft = expectedEndTime - sw.ElapsedMilliseconds;
            var secondsLeft = millisecondsLeft / 1000;
            Log.Debug("Waiting up to {shutdownTimeLimit} seconds for tasks to complete.", secondsLeft);
            var pendingTasks = _runningTaskTable.Values.ToArray();
            Task.WaitAll(pendingTasks, TimeSpan.FromMilliseconds(millisecondsLeft));

         }
         if (_runningTaskTable.Count > 0)
         {
            Log.Fatal("Shutting Down with {runningTaskCount} pending task(s) still processing after the {shutdownTimeLimit} second shutdown grace period. This produces inconsistent behaviour. ", _runningTaskTable.Count, ShutdownLimitSeconds);
            Environment.ExitCode = EXIT_CODE_PENDING_TASKS_STILL_RUNNING;
         }
         else
         {
            Log.Debug("All pending tasks completed.");
         }

         await _connection.Close();

         sw.Stop();
         Log.Information("Service shutting down. Shutdown took {shutdownTime} seconds.", sw.ElapsedMilliseconds / 1000f);

      }

      async Task SetupWebsocket(IConnection socket)
      {
         _connection = socket;

         socket.OnDisconnect(async (s, wasClean) => await CloseConnection(s, wasClean));
         socket.OnMessage( async (s, message, messageNumber) =>
         {
            try
            {
               await MonitorTask(messageNumber, () => HandleWebsocketMessage(socket, message));
            }
            catch (Exception ex)
            {
               BeamableLogger.LogException(ex);
            }
         });

         try
         {
            await _requester.Authenticate();
            await ProvideService(QualifiedName);

            HasInitialized = true;
            Log.Information("Service ready for traffic. baseVersion={baseVersion} executionVersion={executionVersion}", _args.SdkVersionBaseBuild, _args.SdkVersionExecution);
            _serviceInitialized.CompleteSuccess(PromiseBase.Unit);
         }
         catch (Exception ex)
         {
            Log.Error("Service failed to provide services. {message} {stack}", ex.Message, ex.StackTrace);
            _serviceInitialized.CompleteError(ex);
         }

      }

      public Promise<IConnection> GetWebsocketPromise()
      {
         return _webSocketPromise;
      }

      Promise<IConnection> AttemptConnection()
      {
         var connectionAttempt = 0;
         var promise = new Promise<IConnection>();

         Log.Debug("starting ws connection");
         void Attempt()
         {
            Log.Debug("connecting to ws... ");
            var ws = _connectionProvider.Create(Host);
            ws.OnConnect(socket =>
            {
               Log.Debug("connection made.");
               _connectionAttempt = 0;
               promise.CompleteSuccess(socket);
            });
            ws.OnDisconnect(async (socket, wasClean) =>
            {
               if (promise.IsCompleted) return; // ignore, this handler has no purpose anymore.
               if (wasClean) return;

               // try again!
               var retryDelay = connectionAttempt < _retryIntervalsInSeconds.Length
                  ? _retryIntervalsInSeconds[connectionAttempt]
                  : _retryIntervalsInSeconds[^1]; // last one.
               connectionAttempt++;
               Log.Error("connection could not be re-established. Will attempt connection {attempt} in {delay} seconds", connectionAttempt, retryDelay);
               await Task.Delay(retryDelay * 1000);
               Attempt();
            });

            ws.Connect();
         }

         Attempt();
         return promise;
      }

      Task MonitorTask(long messageNumber, Func<Task> taskProducer)
      {
         var task = taskProducer();

         try
         {

            if (!_runningTaskTable.TryAdd(messageNumber, task))
            {
               BeamableLogger.LogWarning("Could not monitor task. {id} {status}", messageNumber, task.Status);
            }

            // watch the task...
            var _ = task.ContinueWith(finishedTask =>
            {
               if (!_runningTaskTable.TryRemove(messageNumber, out var _))
               {
                  BeamableLogger.LogWarning("Could not discard monitored task {id}", messageNumber);
               }

               if (finishedTask.IsFaulted)
               {
                  BeamableLogger.LogException(finishedTask.Exception);
               }
            });
         }
         catch (Exception ex)
         {
            if (!_runningTaskTable.TryRemove(messageNumber, out _))
            {
               Log.Warning("[Exception] Could not discard monitored task {message} {stack}", ex.Message, ex.StackTrace);
            }

            throw;
         }

         return task;
      }


      async Task HandlePlatformMessage(RequestContext ctx, string msg)
      {
         try
         {
            _socketRequesterContext.HandleMessage(ctx, msg);
            await _requester.Acknowledge(ctx);
         }
         catch (Exception ex)
         {
            BeamableLogger.LogException(ex);
            await _requester.Acknowledge(ctx, new WebsocketErrorResponse
            {
               status = 500, // TODO: Catch a special type of exception, NackException?
               error = ex.GetType().Name,
               message = ex.Message,
               service = MicroserviceName
            });
         }
      }


      void InitServices()
      {
         Log.Debug("Registering standard services");
         try
         {
            ServiceCollection = new ServiceCollection();
            ServiceCollection
               .AddScoped(_microserviceType)
               .AddSingleton(_args)
               .AddSingleton<IRealmInfo>(_args)
               .AddSingleton<SocketRequesterContext>(_ => _socketRequesterContext)
               .AddTransient<IBeamableRequester, MicroserviceRequester>()
               .AddTransient<IUserContext>(provider => provider.GetService<RequestContext>())
               .AddTransient<IMicroserviceAuthApi, ServerAuthApi>()
               .AddTransient<IMicroserviceStatsApi, MicroserviceStatsApi>()
               .AddTransient<IStatsApi, MicroserviceStatsApi>()
               .AddSingleton<IMicroserviceContentApi>(_contentService)
               .AddTransient<IMicroserviceInventoryApi, MicroserviceInventoryApi>()
               .AddTransient<IMicroserviceGroupsApi, MicroserviceGroupsApi>()
               .AddTransient<IMicroserviceTournamentApi, MicroserviceTournamentApi>()
               .AddTransient<IMicroserviceLeaderboardsApi, MicroserviceLeaderboardApi>()
               .AddTransient<IMicroserviceAnnouncementsApi, MicroserviceAnnouncementsApi>()
               .AddTransient<IMicroserviceCalendarsApi, MicroserviceCalendarsApi>()
               .AddTransient<IMicroserviceEventsApi, MicroserviceEventsApi>()
               .AddTransient<IMicroserviceMailApi, MicroserviceMailApi>()
               .AddTransient<IMicroserviceSocialApi, MicroserviceSocialApi>()
               .AddTransient<IMicroserviceCloudDataApi, MicroserviceCloudDataApi>()
               .AddTransient<IMicroserviceRealmConfigService, RealmConfigService>()
               .AddTransient<IMicroserviceCommerceApi, MicroserviceCommerceApi>()
               .AddTransient<IStorageObjectConnectionProvider, StorageObjectConnectionProvider>()

               .AddTransient<UserDataCache<Dictionary<string, string>>.FactoryFunction>(provider => StatsCacheFactory)
               .AddTransient<UserDataCache<RankEntry>.FactoryFunction>(provider => LeaderboardRankEntryFactory)
               .AddScoped<IBeamableServices>(ExtractSdks)
               ;

            Log.Debug("Registering custom services");
            var builder = new DefaultServiceBuilder(ServiceCollection);


            var configurationMethods = _microserviceType.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(
               method =>
                  method.GetCustomAttribute<ConfigureServicesAttribute>() != null);
            foreach (var configurationMethod in configurationMethods)
            {
               var parameters = configurationMethod.GetParameters();
               if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IServiceBuilder)) continue;

               try
               {
                  configurationMethod.Invoke(null, new object?[] {builder});
               }
               catch (Exception ex)
               {
                  BeamableLogger.LogError("Configuration method failed. " + ex.Message + " {stacktrace}", ex.StackTrace);
                  BeamableLogger.LogException(ex);
                  throw;
               }
            }
         }
         catch (Exception ex)
         {
            BeamableLogger.LogError("Registering services failed. " + ex.Message);
            BeamableLogger.LogException(ex);
            throw;
         }
      }

      Microservice BuildServiceInstance(RequestContext ctx)
      {

         IServiceProvider Create(RequestContext requestContext)
         {
            var serviceDescriptors = new ServiceDescriptor[ServiceCollection.Count];
            ServiceCollection.CopyTo(serviceDescriptors, 0);
            var requestCollection = new ServiceCollection {serviceDescriptors};
            requestCollection.AddScoped(_ => requestContext);

            var provider = requestCollection.BuildServiceProvider();
            var scope = provider.CreateScope();

            return scope.ServiceProvider;
         }

         var scope = Create(ctx);
         var service = scope.GetRequiredService(_microserviceType) as Microservice;
         service.ProvideDefaultServices(scope, Create);
         return service;
      }

      AdminRoutes BuildAdminInstance(RequestContext ctx)
      {
         var service = new AdminRoutes();
         service.Microservice = this;
         return service;
      }

      async Task HandleClientMessage(RequestContext ctx, string msg)
      {
         if (RefuseNewClientMessages)
         {
            Log.Warning("Received a message after service began draining. id={id}", ctx.Id);
            return; // let this message die.
         }

         try
         {
            var route = ctx.Path.Substring(QualifiedName.Length + 1);

            var parameterProvider = new AdaptiveParameterProvider(ctx);
            var responseJson = await ServiceMethods.Handle(ctx, route, parameterProvider);
            BeamableSerilogProvider.LogContext.Value.Debug("Responding with {json}", responseJson);
            var webSocket = await _webSocketPromise;
            webSocket.SendMessage(responseJson);
         }
         catch (MicroserviceException ex)
         {
            var failResponse = new GatewayErrorResponse
            {
               id = ctx.Id,
               status = ex.ResponseStatus,
               body = ex.GetErrorResponse(_serviceAttribute.MicroserviceName)
            };
            var failResponseJson = JsonConvert.SerializeObject(failResponse);
            BeamableSerilogProvider.LogContext.Value.Error("Exception {type}: {message} - {source} {json} \n {stack}", ex.GetType().Name, ex.Message,
               ex.Source, failResponseJson, ex.StackTrace);
            var webSocket = await _webSocketPromise;
            webSocket.SendMessage(failResponseJson);
         }
         catch (TargetInvocationException ex)
         {
            var inner = ex.InnerException;
            BeamableSerilogProvider.LogContext.Value.Error("Exception {type}: {message} - {source} \n {stack}", inner.GetType().Name,
               inner.Message,
               inner.Source, inner.StackTrace);
            var failResponse = new GatewayResponse
            {
               id = ctx.Id,
               status = 500,
               body = new ClientResponse
               {
                  payload = ""
               }
            };
            var failResponseJson = JsonConvert.SerializeObject(failResponse);
            var webSocket = await _webSocketPromise;
            webSocket.SendMessage(failResponseJson);
         }
         catch (Exception ex) // TODO: Catch a general PlatformException type sort of thing.
         {
            BeamableSerilogProvider.LogContext.Value.Error("Exception {type}: {message} - {source} \n {stack}", ex.GetType().Name, ex.Message,
               ex.Source, ex.StackTrace);
            // var failResponse = new GatewayErrorResponse
            // {
            //    id = ctx.Id,
            //    status = ex.ResponseStatus,
            //    body = ex.GetErrorResponse(_serviceAttribute.MicroserviceName)
            // };
            var failResponse = new GatewayResponse
            {
               id = ctx.Id,
               status = 500,
               body = new ClientResponse
               {
                  payload = ex.Message // TODO: Format this into a better response.
               }
            };
            var failResponseJson = JsonConvert.SerializeObject(failResponse);
            var webSocket = await _webSocketPromise;
            webSocket.SendMessage(failResponseJson);
         }
      }

      async Task HandleWebsocketMessage(IConnection ws, string msg)
      {
         Log.Debug("Handling WS Message. {msg}",msg);

         if (!msg.TryBuildRequestContext(_args, out var ctx))
         {
            Log.Debug("WS Message contains no data. Cannot handle. Skipping message.");
            return;
         }

         var reqLog = Log.ForContext("requestContext", ctx, true);
         BeamableSerilogProvider.LogContext.Value = reqLog;

         if (_socketRequesterContext.IsPlatformMessage(ctx))
         {
            // the request is a platform request.
            await HandlePlatformMessage(ctx, msg);
         }
         else
         {
            // this is a client request. Handle the service method.
            await HandleClientMessage(ctx, msg);
         }
      }

      private IBeamableRequester GenerateRequester(RequestContext ctx)
      {
         return new MicroserviceRequester(_args, ctx, _socketRequesterContext);
      }

      private IBeamableServices ExtractSdks(IServiceProvider provider)
      {
         var services = new BeamableServices
         {
            Auth = provider.GetRequiredService<IMicroserviceAuthApi>(),
            Stats = provider.GetRequiredService<IMicroserviceStatsApi>(),
            Content = provider.GetRequiredService<IMicroserviceContentApi>(),
            Inventory = provider.GetRequiredService<IMicroserviceInventoryApi>(),
            Leaderboards = provider.GetRequiredService<IMicroserviceLeaderboardsApi>(),
            Announcements = provider.GetRequiredService<IMicroserviceAnnouncementsApi>(),
            Calendars = provider.GetRequiredService<IMicroserviceCalendarsApi>(),
            Events = provider.GetRequiredService<IMicroserviceEventsApi>(),
            Groups = provider.GetRequiredService<IMicroserviceGroupsApi>(),
            Mail = provider.GetRequiredService<IMicroserviceMailApi>(),
            Social = provider.GetRequiredService<IMicroserviceSocialApi>(),
            Tournament = provider.GetRequiredService<IMicroserviceTournamentApi>(),
            TrialData = provider.GetRequiredService<IMicroserviceCloudDataApi>(),
            RealmConfig= provider.GetRequiredService<IMicroserviceRealmConfigService>(),
            Commerce = provider.GetRequiredService<IMicroserviceCommerceApi>()
         };
         return services;
      }

      private IBeamableServices GenerateSdks(IBeamableRequester requester, RequestContext ctx)
      {
         var stats = new MicroserviceStatsApi(requester, ctx, StatsCacheFactory);
         var services = new BeamableServices
         {
            Auth = new ServerAuthApi(requester, ctx),
            Stats = stats,
            Content = _contentService,
            Inventory = new MicroserviceInventoryApi(requester, ctx),
            Leaderboards = new MicroserviceLeaderboardApi(requester, ctx, LeaderboardRankEntryFactory),
            Announcements = new MicroserviceAnnouncementsApi(requester, ctx),
            Calendars = new MicroserviceCalendarsApi(requester, ctx),
            Events = new MicroserviceEventsApi(requester, ctx),
            Groups = new MicroserviceGroupsApi(requester, ctx),
            Mail = new MicroserviceMailApi(requester, ctx),
            Social = new MicroserviceSocialApi(requester, ctx),
            Tournament = new MicroserviceTournamentApi(stats, requester, ctx),
            TrialData = new MicroserviceCloudDataApi(requester, ctx),
            RealmConfig= new RealmConfigService(requester),
            Commerce = new MicroserviceCommerceApi(requester)
         };

         return services;
      }

      private UserDataCache<RankEntry> _singleRankyEntryCache;
      private UserDataCache<RankEntry> LeaderboardRankEntryFactory(string name, long ttlms, UserDataCache<RankEntry>.CacheResolver resolver)
      {
         return new EphemeralUserDataCache<RankEntry>(name, resolver);
      }

      private UserDataCache<Dictionary<string, string>> _singleStatsCache;
      private Type _microserviceType;

      private UserDataCache<Dictionary<string, string>> StatsCacheFactory(string name, long ttlms, UserDataCache<Dictionary<string, string>>.CacheResolver resolver)
      {
         return new EphemeralUserDataCache<Dictionary<string, string>>(name, resolver);
      }



      private async Task CloseConnection(IConnection ws, bool wasClean)
      {
         Log.Debug("Closing socket connection... clean=[{clean}] isShuttingDown=[{shuttingDown]", wasClean, IsShuttingDown);
         if (!IsShuttingDown)
         {
            Log.Debug("ws connection dropped...");
            _webSocketPromise = AttemptConnection();
            var socket = await _webSocketPromise;
            await SetupWebsocket(socket);
         }
         else
         {
            _socketRequesterContext.HandleCloseConnection();
         }
      }

      private Promise<Unit> ProvideService(string name)
      {
         var req = new MicroserviceProviderRequest
         {
            type = "basic",
            name = name
         };
         var serviceProvider = _requester.Request<MicroserviceProviderResponse>(Method.POST, "gateway/provider", req).Then(res =>
         {
            Log.Debug("Service provider initialized");
         }).ToUnit();
         var eventProvider = _requester.InitializeSubscription().Then(res =>
         {
            Log.Debug("Event provider initialized");
         }).ToUnit();
         return Promise.Sequence(serviceProvider, eventProvider).ToUnit();
      }

      private Promise<Unit> RemoveService(string name)
      {
         var req = new MicroserviceProviderRequest
         {
            type = "basic",
            name = name
         };
         var serviceProvider = _requester.Request<MicroserviceProviderResponse>(Method.DELETE, "gateway/provider", req).Then(res =>
         {
            RefuseNewClientMessages = true;
            Log.Debug("Service provider removed");
         }).ToUnit();
         return Promise.Sequence(serviceProvider).ToUnit();
      }


   }
}
#endif