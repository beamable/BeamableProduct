/*
 *  Simple CustomMicroservice example
 */
//#define DB_MICROSERVICE  // I sometimes enable this to see code better in rider


using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Dependencies;
using Beamable.Server.Api.RealmConfig;
using beamable.tooling.common.Microservice;
using Core.Server.Common;
using microservice;
using ContentService = Beamable.Server.Content.ContentService;
#if DB_MICROSERVICE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Realms;
using Beamable.Common.Reflection;
using microservice.Common;
using Newtonsoft.Json;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using static Beamable.Common.Constants.Features.Services;

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

   public class MicroserviceServiceProviderRequest
   {
	   public string type = "basic";
	   public string name;
   }

   public class MicroserviceEventProviderRequest
   {
	   public string type = "event";
	   public string[] evtWhitelist;
	   public string name; // We can remove this field after the platform no longer needs it. Maybe mid August 2022?
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
      private const int EXIT_CODE_FAILED_AUTH = 12;
      private const int EXIT_CODE_FAILED_CUSTOM_INITIALIZATION_HOOK = 110;
      private const int HTTP_STATUS_GONE = 410;
      private const int ShutdownLimitSeconds = 5;
      private const int ShutdownMinCycleTimeMilliseconds = 100;
      private Promise<Unit> _serviceInitialized = new Promise<Unit>();
      private IConnection _connection;
      private Promise<IConnection> _webSocketPromise;
      private MicroserviceRequester _requester;
      public SocketRequesterContext SocketContext => _socketRequesterContext;
      private SocketRequesterContext _socketRequesterContext;
      public ServiceMethodCollection ServiceMethods { get; private set; }
      public MicroserviceAuthenticationDaemon AuthenticationDaemon => _socketRequesterContext?.Daemon;
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

      public bool HasInitialized { get; private set; }


      private IMicroserviceArgs _args;
      private CancellationTokenSource _serviceShutdownTokenSource;
      private Task _socketDaemen;
      private string Host => _args.Host;
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

      /// <summary>
      /// We need to guarantee <see cref="ResolveCustomInitializationHook"/> only gets run once when we <see cref="SetupWebsocket"/>.
      /// </summary>
      private bool _ranCustomUserInitializationHooks = false;

      public IServiceProvider Provider => _args.ServiceScope;
      

      public async Task Start<TMicroService>(IMicroserviceArgs args)
         where TMicroService : Microservice
      {
         if (HasInitialized) return;

         MicroserviceType = typeof(TMicroService);
         _serviceAttribute = MicroserviceType.GetCustomAttribute<MicroserviceAttribute>();
         if (_serviceAttribute == null)
         {
            throw new Exception($"Cannot create service. Missing [{typeof(MicroserviceAttribute).Name}].");
         }

         _socketRequesterContext = new SocketRequesterContext(GetWebsocketPromise);
         _args = args.Copy(conf =>
         {
	         conf.ServiceScope = conf.ServiceScope.Fork(builder =>
	         {
				// do we need instance specific services? They'd go here.
				builder.AddScoped(_socketRequesterContext);
				builder.AddScoped(_socketRequesterContext.Daemon);
	         });
         });

         Log.Debug(Logs.STARTING_PREFIX + " {host} {prefix} {cid} {pid} {sdkVersionExecution} {sdkVersionBuild} {disableCustomHooks}", args.Host, args.NamePrefix, args.CustomerID, args.ProjectName, args.SdkVersionExecution, args.SdkVersionBaseBuild, args.DisableCustomInitializationHooks);
         
         RebuildRouteTable();

         _requester = new MicroserviceRequester(_args, null, _socketRequesterContext, false);
         _serviceShutdownTokenSource = new CancellationTokenSource();
         (_socketDaemen, _socketRequesterContext.Daemon) = MicroserviceAuthenticationDaemon.Start(_args, _requester, _serviceShutdownTokenSource);

         _serviceInitialized.Error(ex =>
         {
            Log.Error("Service failed to initialize {message} {stack}", ex.Message, ex.StackTrace);
         });
         
         // the first time this runs, it'll complete, but due to how promises work, all of the next times, it'll no-op.
         var contentService = Provider.GetService<ContentService>();
         ContentApi.Instance.CompleteSuccess(contentService);

         // Connect and Run
         _webSocketPromise = AttemptConnection();
         var socket = await _webSocketPromise;
         
         if (!_args.DisableCustomInitializationHooks && !_ranCustomUserInitializationHooks)
         {
	         await SetupStorage();
         }

         await SetupWebsocket(socket, _serviceAttribute.EnableEagerContentLoading);
         if (!_serviceAttribute.EnableEagerContentLoading)
         {
	         var _ = contentService.Init();
         }
      }
      
      private async Promise SetupStorage()
      {
	      var reflectionCache = Provider.GetService<ReflectionCache>();
	      var mongoIndexesReflectionCache = reflectionCache.GetFirstSystemOfType<MongoIndexesReflectionCache>();
		
	      IStorageObjectConnectionProvider connectionProvider =
		      Provider.GetService<IStorageObjectConnectionProvider>();

	      await mongoIndexesReflectionCache.SetupStorage(connectionProvider);
      }

      public async Task RunForever()
      {
         AppDomain.CurrentDomain.ProcessExit += async (sender, args) =>
         {
            await OnShutdown(sender, args);
         };

         await Task.Delay(-1);
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
            var startedWaitingAt = sw.ElapsedMilliseconds;
            Task.WaitAll(pendingTasks, TimeSpan.FromMilliseconds(millisecondsLeft));
            /* we need to wait a minimum number of moments in this loop so we don't exhaust the task cycle.
             What can happen is that all the tasks are DONE, so, the WaitAll completes. However, the task hasn't removed itself from the _runningTaskTable yet,
             because its continuation hasn't been executed. And then, if that happens, we enter a scenario where this method just loops and loops, and since the Task.WaitAll
             is always complete, the other continuation takes a long time to get scheduled.
             */
            var stoppedWaitingAt = sw.ElapsedMilliseconds;
            var waitedForMilliseconds = stoppedWaitingAt - startedWaitingAt;
            var requiredWaitTimeLeft = Math.Max(0, ShutdownMinCycleTimeMilliseconds - waitedForMilliseconds);
            await Task.Delay(TimeSpan.FromMilliseconds(requiredWaitTimeLeft));
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

         // stop the daemon from trying to re-authenticate
         _socketRequesterContext.Daemon.KillAuthThread();
         await _socketDaemen;

         // close the connection itself
         await _connection.Close();

         sw.Stop();
         Log.Information("Service shutting down. Shutdown took {shutdownTime} seconds.", sw.ElapsedMilliseconds / 1000f);

      }

      public void RebuildRouteTable()
      {
	      var adminRoutes = new AdminRoutes
	      {
		      MicroserviceAttribute = _serviceAttribute, 
		      MicroserviceType = MicroserviceType,
		      PublicHost = $"{_args.Host.Replace("wss://", "https://").Replace("/socket", "")}/basic/{_args.CustomerID}.{_args.ProjectName}.{QualifiedName}/"
	      };
	      ServiceMethods = RouteTableGeneration.BuildRoutes(MicroserviceType, _serviceAttribute, adminRoutes, BuildServiceInstance);
      }

      async Task SetupWebsocket(IConnection socket, bool initContent = false)
      {
         _connection = socket;

         socket.OnDisconnect((s, wasClean) => CloseConnection(s, wasClean).Wait());

         socket.OnMessage(async (s, message, messageNumber, sw) =>
         {
	         try
	         {
		         await MonitorTask(messageNumber, () => HandleWebsocketMessage(socket, message, sw));
	         }
	         catch (Exception ex)
	         {
		         BeamableLogger.LogException(ex);
	         }
         });

         try
         {
	         _socketRequesterContext.Daemon.WakeAuthThread();
            await _requester.WaitForAuthorization(TimeSpan.FromMinutes(2), "Registering services event.");
            // We can disable custom initialization hooks from running. This is so we can verify the image works (outside of the custom hooks) before a publish.
            // TODO This is not ideal. There's an open ticket with some ideas on how we can improve the publish process to guarantee it's impossible to publish an image
            // TODO that will not boot correctly.
            if (!_args.DisableCustomInitializationHooks)
            {
                // Custom Initialization hook for C#MS --- will terminate MS user-code throws.
                // Only gets run once --- if we need to setup the websocket again, we don't run this a second time.
                if (!_ranCustomUserInitializationHooks)
                {
	                if (initContent)
	                {
		                await Provider.GetService<ContentService>().Init(preload:true);
	                }

                    await ResolveCustomInitializationHook();
                    _ranCustomUserInitializationHooks = true;
                }
            }

            var realmService = _args.ServiceScope.GetService<IRealmConfigService>();
            await realmService.GetRealmConfigSettings();
            
            await ProvideService(QualifiedName);

            HasInitialized = true;
            var url = BuildSwaggerUrl();
            var portalUrlLogline = string.IsNullOrEmpty(url) ? url : $"portalURL={url}";
            Log.Information(Logs.READY_FOR_TRAFFIC_PREFIX + "baseVersion={baseVersion} executionVersion={executionVersion} {portalUrlLogline}", _args.SdkVersionBaseBuild, _args.SdkVersionExecution, portalUrlLogline);
            realmService.UpdateLogLevel();

            _serviceInitialized.CompleteSuccess(PromiseBase.Unit);
         }
         catch (Exception ex)
         {
            Log.Fatal("Service failed to provide services. {message} {stack}", ex.Message, ex.StackTrace);
            _serviceInitialized.CompleteError(ex);
            Environment.Exit(EXIT_CODE_FAILED_AUTH);
         }

      }

      private string BuildSwaggerUrl()
      {
	      var cid = _args.CustomerID;
	      var pid = _args.ProjectName;
	      var microName = _serviceAttribute.MicroserviceName;
	      var refreshToken = _args.RefreshToken;

	      if (string.IsNullOrEmpty(refreshToken))
	      {
		      return "";
	      }
	      
	      var queryArgs = new List<string>
	      {
		      $"refresh_token={refreshToken}",
		      $"prefix={_args.NamePrefix}"
	      };
	      var joinedQueryString = string.Join("&", queryArgs);
	      var treatedHost = _args.Host.Replace("/socket", "")
		      .Replace("wss", "https")
		      .Replace("dev.", "dev-")
		      .Replace("api", "portal");
	      var url = $"{treatedHost}/{cid}/games/{pid}/realms/{pid}/microservices/{microName}/docs?{joinedQueryString}";
	      
	      return url;
      }



      /// <summary>
      /// Handles custom initialization hooks. Makes the following assumptions:
      ///   - User defined at least one <see cref="InitializeServicesAttribute"/> over a static async method that returns a <see cref="Promise{Unit}"/> or a <see cref="Promise"/> and receives a <see cref="IServiceInitializer"/>.
      ///   - Any exception will fail loudly and prevent the C#MS from receiving traffic.
      /// <para/>
      /// </summary>
      private async Task ResolveCustomInitializationHook()
      {
         // Gets Service Initialization Methods
         var serviceInitialization = MicroserviceType
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<InitializeServicesAttribute>() != null)
            .Select(method =>
            {
               var attr = method.GetCustomAttribute<InitializeServicesAttribute>();
               return (method, attr);
            })
            .ToList();

         // Sorts them by an user-defined order. By default (and tie-breaking), is sorted in file declaration order.
         // TODO: Add reflection utility that sorts (MemberInfo, ISortableByType<>) tuples to ReflectionCache and replace this usage.
         serviceInitialization.Sort(delegate((MethodInfo method, InitializeServicesAttribute attr) t1, (MethodInfo method, InitializeServicesAttribute attr) t2)
         {
            var (_, attr1) = t1;
            var (_, attr2) = t2;
            return attr1.ExecutionOrder.CompareTo(attr2.ExecutionOrder);
         });

         // Invokes each Service Initialization Method --- skips any that do not match the void(IServiceInitializer) signature.
         var serviceInitializers = new DefaultServiceInitializer(_args.ServiceScope.Parent, _args);
         foreach (var (initializationMethod, _) in serviceInitialization)
         {
            // TODO: Add compile-time check for this signature so we can educate our users on this without them having to deep dive into docs
            var parameters = initializationMethod.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IServiceInitializer))
            {
               BeamableLogger.LogWarning($"Skipping method with [{nameof(InitializeServicesAttribute)}] since it does not take a single [{nameof(IServiceInitializer)}] parameter.");
               continue;

            }

            var resultType = initializationMethod.ReturnType;
            Promise<Unit> promise;
            if (resultType == typeof(void))
            {
               var isAsync = null != initializationMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
               if (isAsync)
               {
                  BeamableLogger.LogWarning($"Skipping method [{initializationMethod.DeclaringType?.FullName}.{initializationMethod.Name}] " +
                                            $"with [{nameof(InitializeServicesAttribute)}] since it is an async void method. Since these do not return a Task or Promise, " +
                                            $"we can't await it's return and using this may cause non-deterministic behaviour depending on your implementation. " +
                                            $"We recommend not using this unless you know exactly what you are doing.");
                  continue;
               }

               promise = Task.FromResult(initializationMethod.Invoke(null, new object[] { serviceInitializers })).ToPromise().ToUnit();
            }
            else if (resultType == typeof(Task))
            {
               promise = ((Task)initializationMethod.Invoke(null, new object[] { serviceInitializers })).ToPromise();
            }
            else if (resultType == typeof(Promise<Unit>))
            {
               promise = (Promise<Unit>)initializationMethod.Invoke(null, new object[] { serviceInitializers });
            }
            else if (resultType == typeof(Promise))
            {
	            promise = (Promise)initializationMethod.Invoke(null, new object[] { serviceInitializers });
            }
            else
            {
               BeamableLogger.LogWarning($"Skipping method with [{nameof(InitializeServicesAttribute)}] since it isn't a synchronous [void] method, a [{nameof(Task)}], a [{nameof(Promise)}] or a [{nameof(Promise<Unit>)}]");
               continue;
            }

            try
            {
               await promise;
               BeamableLogger.Log($"Custom service initializer [{initializationMethod.DeclaringType?.FullName}.{initializationMethod.Name}] succeeded.\n");
            }
            catch (Exception ex)
            {
               BeamableLogger.LogError($"Custom service initializer [{initializationMethod.DeclaringType?.FullName}.{initializationMethod.Name}] failed.\n" +
                                       $"{ex.Message}\n" +
                                       $"{{stacktrace}}", ex.StackTrace);

               BeamableLogger.LogException(ex);
               Environment.Exit(EXIT_CODE_FAILED_CUSTOM_INITIALIZATION_HOOK);
            }
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
            Log.Debug($"connecting to ws ({Host}) ... ");
            var ws = Provider.GetService<IConnectionProvider>().Create(Host, _args);
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


      async Task HandlePlatformMessage(RequestContext ctx)
      {
         try
         {
            _socketRequesterContext.HandleMessage(ctx);
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
      

      Microservice BuildServiceInstance(RequestContext ctx)
      {

	      IDependencyProviderScope Create(RequestContext requestContext)
	      {
		      var scope = _args.ServiceScope.Fork(builder =>
		      {
			      // each _request_ gets its own service scope, so we fork the provider again and override certain services. 
			      builder.AddScoped(requestContext);
			      builder.AddScoped(_args);
		      });
		      
		      return scope;
	      }

	      var scope = Create(ctx);
	      var service = scope.GetRequiredService(MicroserviceType) as Microservice;
	      service.ProvideDefaultServices(scope, Create);
	      return service;
      }

      async Task HandleClientMessage(MicroserviceRequestContext ctx, Stopwatch sw)
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
            BeamableSerilogProvider.LogContext.Value.Verbose("Responding with " + responseJson);
            await _socketRequesterContext.SendMessageSafely(responseJson, sw: sw);
            // TODO: Kill Scope
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
            await _socketRequesterContext.SendMessageSafely(failResponseJson, sw: sw);
         }
         catch (TargetInvocationException ex)
         {
            var inner = ex.InnerException;
            var failResponse = new GatewayResponse()
            {
               id = ctx.Id,
            };

            string failResponseJson;

            if (inner is MicroserviceException msException)
            {
               failResponse.status = msException.ResponseStatus;
               failResponse.body = msException.GetErrorResponse(_serviceAttribute.MicroserviceName);

               failResponseJson = JsonConvert.SerializeObject(failResponse);
               BeamableSerilogProvider.LogContext.Value.Error("Exception {type}: {message} - {source} {json} \n {stack}", msException.GetType().Name, msException.Message,
                  msException.Source, failResponseJson, msException.StackTrace);
            }
            else
            {
               failResponse = new GatewayResponse
               {
                  id = ctx.Id,
                  status = 500,
                  body = new ClientResponse
                  {
                     payload = ""
                  }
               };

               failResponseJson = JsonConvert.SerializeObject(failResponse);
               BeamableSerilogProvider.LogContext.Value.Error("Exception {type}: {message} - {source} \n {stack}", inner.GetType().Name,
                  inner.Message,
                  inner.Source, inner.StackTrace);
            }

            await _socketRequesterContext.SendMessageSafely(failResponseJson, sw: sw);
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
            await _socketRequesterContext.SendMessageSafely(failResponseJson, sw: sw);
         }
      }

      async Task HandleWebsocketMessage(IConnection ws, JsonDocument document, Stopwatch sw)
      {
	      if (!document.TryBuildRequestContext(_args, out var ctx))
         {
            Log.Debug("WS Message contains no data. Cannot handle. Skipping message.");
            return;
         }
         
         var reqLog = Log.ForContext("requestContext", ctx, true);
         BeamableSerilogProvider.LogContext.Value = reqLog;

         try
         {
	         using var tokenSource = new CancellationTokenSource();
	         ctx.CancellationToken = tokenSource.Token;
	         tokenSource.CancelAfter(TimeSpan.FromSeconds(_args.RequestCancellationTimeoutSeconds));
	         if (_socketRequesterContext.IsPlatformMessage(ctx))
	         {
		         // the request is a platform request.
		         await HandlePlatformMessage(ctx);
	         }
	         else
	         {
		         // this is a client request. Handle the service method.
		         await HandleClientMessage(ctx, sw);
	         }
         }
         finally
         {
	         (BeamableSerilogProvider.LogContext.Value as IDisposable)?.Dispose();
	         BeamableSerilogProvider.LogContext.Value = null;
         }
      }

      private UserDataCache<RankEntry> _singleRankyEntryCache;
      

      private UserDataCache<Dictionary<string, string>> _singleStatsCache;
      public Type MicroserviceType { get; private set; }


      private async Task CloseConnection(IConnection ws, bool wasClean)
      {
         Log.Debug("Closing socket connection... clean=[{clean}] isShuttingDown=[{shuttingDown}]", wasClean, IsShuttingDown);
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
         var req = new MicroserviceServiceProviderRequest
         {
            type = "basic",
            name = name
         };
         var serviceProvider = _requester.Request<MicroserviceProviderResponse>(Method.POST, "gateway/provider", req).Then(res =>
         {
            Log.Debug(Logs.SERVICE_PROVIDER_INITIALIZED);
         }).ToUnit();
         var eventProvider = _serviceAttribute.DisableAllBeamableEvents
	         ? PromiseBase.SuccessfulUnit
	         : _requester.InitializeSubscription().Then(res =>
	         {
		         Log.Debug(Logs.EVENT_PROVIDER_INITIALIZED);
	         }).ToUnit();
         return Promise.Sequence(serviceProvider, eventProvider).ToUnit();
      }

      private Promise<Unit> RemoveService(string name)
      {
         var req = new MicroserviceServiceProviderRequest
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
