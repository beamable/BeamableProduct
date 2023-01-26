using System;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Api.Stats;
using Beamable.Common.Assistant;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Server;
using Beamable.Server.Api;
using Beamable.Server.Api.Announcements;
using Beamable.Server.Api.Calendars;
using Beamable.Server.Api.Chat;
using Beamable.Server.Api.CloudData;
using Beamable.Server.Api.Commerce;
using Beamable.Server.Api.Content;
using Beamable.Server.Api.Events;
using Beamable.Server.Api.Groups;
using Beamable.Server.Api.Inventory;
using Beamable.Server.Api.Leaderboards;
using Beamable.Server.Api.Mail;
using Beamable.Server.Api.Notifications;
using Beamable.Server.Api.Payments;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Api.Social;
using Beamable.Server.Api.Stats;
using Beamable.Server.Api.Tournament;
using Beamable.Server.Common;
using Beamable.Server.Content;
using Core.Server.Common;
using microservice;
using microservice.Common;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Constants = Beamable.Common.Constants;

namespace Beamable.Server
{
    public static class MicroserviceBootstrapper
    {
	    private const int MSG_SIZE_LIMIT = 1000;

	    public static LoggingLevelSwitch LogLevel;
	    public static ReflectionCache ReflectionCache;
	    public static ContentService ContentService;
	    public static List<BeamableMicroService> Instances = new List<BeamableMicroService>();
	    
        private static void ConfigureLogging(IMicroserviceArgs args)
        {
            // TODO pull "LOG_LEVEL" into a const?
            var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "debug";
			var disableLogTruncate = (Environment.GetEnvironmentVariable("DISABLE_LOG_TRUNCATE")?.ToLowerInvariant() ?? "") == "true";
			
            var envLogLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), logLevel, true);

            // The LoggingLevelSwitch _could_ be controlled at runtime, if we ever wanted to do that.
            LogLevel = new LoggingLevelSwitch { MinimumLevel = envLogLevel };

            // https://github.com/serilog/serilog/wiki/Configuration-Basics
            var logConfig = new LoggerConfiguration()
	            .MinimumLevel.ControlledBy(LogLevel)
	            .Enrich.FromLogContext()
	            .Destructure.ByTransforming<MicroserviceRequestContext>(ctx => new
	            {
		            cid = ctx.Cid,
		            pid = ctx.Pid,
		            path = ctx.Path,
		            status = ctx.Status,
		            id = ctx.Id,
		            isEvent = ctx.IsEvent,
		            userId = ctx.UserId,
		            scopes = ctx.Scopes
	            });

            if (!disableLogTruncate)
            {
	            logConfig = logConfig
		            .Enrich.With(new LogMsgSizeEnricher(args.LogTruncateLimit))
		            .Destructure.ToMaximumCollectionCount(args.LogMaxCollectionSize)
		            .Destructure.ToMaximumDepth(args.LogMaxDepth)
		            .Destructure.ToMaximumStringLength(args.LogDestructureMaxLength);
            }
            
            Log.Logger = logConfig
               .WriteTo.Console(new MicroserviceLogFormatter())
               .CreateLogger();

            // use newtonsoft for JsonUtility
            JsonUtilityConverter.Init();

            BeamableLogProvider.Provider = new BeamableSerilogProvider();
            Debug.Instance = new MicroserviceDebug();
            BeamableSerilogProvider.LogContext.Value = Log.Logger;
        }

        public static void ConfigureUnhandledError()
        {
            PromiseBase.SetPotentialUncaughtErrorHandler((promise, exception) =>
            {
	            async Task DelayedCheck()
	            {
		            await Task.Delay(1);;
		            if (promise?.HadAnyErrbacks ?? true) return;

		            BeamableLogger.LogError("Uncaught promise error. {promiseType} {message} {stack}", promise.GetType(), exception.Message, exception.StackTrace);
		            throw exception;
	            }

	            _ = Task.Run(DelayedCheck);
            });
        }

        private static void ConfigureDocsProvider()
        {
            XmlDocsHelper.ProviderFactory = XmlDocsHelper.FileIOProvider;
        }

        public static void ConfigureUncaughtExceptions()
        {
	        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
	        {
		        Console.Error.WriteLine($"Uncaught exception error!!! {args.ExceptionObject.GetType().Name}");
		        if (args.ExceptionObject is Exception ex)
		        {
			        Console.Error.WriteLine($"{ex.Message} -- {ex.StackTrace}");
		        }
		        
				Log.Fatal($"Unhandled exception. type=[{args.ExceptionObject?.GetType()?.Name}]");
	        };
        }

        public static ReflectionCache ConfigureReflectionCache()
        {
	        
	        var reflectionCache = new ReflectionCache();
	        var contentTypeReflectionCache = new ContentTypeReflectionCache();
	        reflectionCache.RegisterTypeProvider(contentTypeReflectionCache);
	        reflectionCache.RegisterReflectionSystem(contentTypeReflectionCache);
	        reflectionCache.SetStorage(new BeamHintGlobalStorage());

	        var relevantAssemblyNames = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.GetName().Name.StartsWith("System.") &&
			        !asm.GetName().Name.StartsWith("nunit.") &&
			        !asm.GetName().Name.StartsWith("JetBrains.") &&
			        !asm.GetName().Name.StartsWith("Microsoft.") &&
			        !asm.GetName().Name.StartsWith("Serilog."))
		        .Select(asm => asm.GetName().Name)
		        .ToList();
	        Log.Debug($"Generating Reflection Cache over Assemblies => {string.Join('\n', relevantAssemblyNames)}");
	        reflectionCache.GenerateReflectionCache(relevantAssemblyNames);

	        return reflectionCache;
        }

        public static IDependencyBuilder ConfigureServices<T>(IMicroserviceArgs envArgs) where T : Microservice
        {
	        Log.Debug(Constants.Features.Services.Logs.REGISTERING_STANDARD_SERVICES);
	        var attribute = typeof(T).GetCustomAttribute<MicroserviceAttribute>();
	        
	        try
	        {
		        var collection = new DependencyBuilder();
		        collection
			        .AddScoped<T>()
			        .AddScoped<IDependencyProvider>(provider => new MicrosoftServiceProviderWrapper(provider))
			        .AddScoped<IRealmInfo>(provider => provider.GetService<IMicroserviceArgs>())
			        .AddScoped<IBeamableRequester>(p => p.GetService<MicroserviceRequester>())
			        .AddSingleton<IMicroserviceArgs>(envArgs)
			        .AddSingleton<SocketRequesterContext>(_ =>
			        {
				        return Instances[0].SocketContext;
			        })
			        .AddScoped(provider =>
				        new MicroserviceRequester(provider.GetService<IMicroserviceArgs>(), provider.GetService<RequestContext>(), provider.GetService<SocketRequesterContext>(), true))
			        .AddScoped<IUserContext>(provider => provider.GetService<RequestContext>())
			        .AddScoped<IMicroserviceAuthApi, ServerAuthApi>()
			        .AddScoped<IMicroserviceStatsApi, MicroserviceStatsApi>()
			        .AddScoped<IStatsApi, MicroserviceStatsApi>()
			        .AddSingleton(new RequestContext(envArgs.CustomerID, envArgs.ProjectName))
			        .AddSingleton<ContentService>(p => CreateNewContentService(
				        p.GetService<MicroserviceRequester>(),
				        p.GetService<SocketRequesterContext>(),
				        p.GetService<IContentResolver>(),
				        p.GetService<ReflectionCache>()
			        ))
			        .AddSingleton<IMicroserviceContentApi>(p => p.GetService<ContentService>())
			        .AddSingleton<IContentApi>(p => p.GetService<ContentService>())
			        .AddSingleton<IContentResolver, DefaultContentResolver>()
			        .AddSingleton<IConnectionProvider, EasyWebSocketProvider>()
			        .AddScoped<IMicroserviceInventoryApi, MicroserviceInventoryApi>()
			        .AddScoped<IMicroserviceGroupsApi, MicroserviceGroupsApi>()
			        .AddScoped<IMicroserviceTournamentApi, MicroserviceTournamentApi>()
			        .AddScoped<IMicroserviceLeaderboardsApi, MicroserviceLeaderboardApi>()
			        .AddScoped<IMicroserviceAnnouncementsApi, MicroserviceAnnouncementsApi>()
			        .AddScoped<IMicroserviceCalendarsApi, MicroserviceCalendarsApi>()
			        .AddScoped<IMicroserviceEventsApi, MicroserviceEventsApi>()
			        .AddScoped<IMicroserviceMailApi, MicroserviceMailApi>()
			        .AddScoped<IMicroserviceNotificationsApi, MicroserviceNotificationApi>()
			        .AddScoped<IMicroserviceSocialApi, MicroserviceSocialApi>()
			        .AddScoped<IMicroserviceCloudDataApi, MicroserviceCloudDataApi>()
			        .AddScoped<IMicroserviceRealmConfigService, RealmConfigService>()
			        .AddScoped<IMicroserviceCommerceApi, MicroserviceCommerceApi>()
			        .AddScoped<IMicroservicePaymentsApi, MicroservicePaymentsApi>()
			        .AddSingleton<IStorageObjectConnectionProvider, StorageObjectConnectionProvider>()
			        .AddSingleton<MongoSerializationService>()
			        .AddSingleton<IMongoSerializationService>(p => p.GetService<MongoSerializationService>())
			        .AddScoped<IMicroserviceChatApi, MicroserviceChatApi>()
			        .AddSingleton(ReflectionCache)

			        .AddScoped<UserDataCache<Dictionary<string, string>>.FactoryFunction>(provider =>
				        StatsCacheFactory)
			        .AddScoped<UserDataCache<RankEntry>.FactoryFunction>(provider => LeaderboardRankEntryFactory)
			        .AddScoped<IBeamableServices>(ExtractSdks)
			        ;
		        
		        
		        Log.Debug(Constants.Features.Services.Logs.REGISTERING_CUSTOM_SERVICES);
		        var builder = new DefaultServiceBuilder(collection);

		        // Gets Service Configuration Methods
		        var configurationMethods = typeof(T)
			        .GetMethods(BindingFlags.Static | BindingFlags.Public)
			        .Where(method => method.GetCustomAttribute<ConfigureServicesAttribute>() != null)
			        .Select(method =>
			        {
				        var attr = method.GetCustomAttribute<ConfigureServicesAttribute>();
				        return (method, attr);
			        })
			        .ToList();

		        // Sorts them by an user-defined order. By default (and tie-breaking), is sorted in file declaration order.
		        configurationMethods.Sort(delegate((MethodInfo method, ConfigureServicesAttribute attr) t1,
			        (MethodInfo method, ConfigureServicesAttribute attr) t2)
		        {
			        var (_, attr1) = t1;
			        var (_, attr2) = t2;
			        return attr1.ExecutionOrder.CompareTo(attr2.ExecutionOrder);
		        });

		        // Invokes each Service Configuration Method --- skips any that do not match the void(IServiceBuilder) signature.
		        foreach (var (configurationMethod, _) in configurationMethods)
		        {
			        // TODO: Add compile-time check for this signature
			        var parameters = configurationMethod.GetParameters();
			        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IServiceBuilder)) continue;

			        try
			        {
				        configurationMethod.Invoke(null, new object?[] { builder });
			        }
			        catch (Exception ex)
			        {
				        BeamableLogger.LogError("Configuration method failed. " + ex.Message + " {stacktrace}",
					        ex.StackTrace);
				        BeamableLogger.LogException(ex);
				        throw;
			        }
		        }
		        return collection;
	        }
	        catch (Exception ex)
	        {
		        BeamableLogger.LogError("Registering services failed. " + ex.Message);
		        BeamableLogger.LogException(ex);
		        throw;
	        }

	        ContentService CreateNewContentService(MicroserviceRequester requester, SocketRequesterContext socket, IContentResolver contentResolver, ReflectionCache reflectionCache)
	        {
		        return attribute.DisableAllBeamableEvents
			        ? new UnreliableContentService(requester, socket, contentResolver, reflectionCache)
			        : new ContentService(requester, socket, contentResolver, reflectionCache);
	        }
	        
	        UserDataCache<Dictionary<string, string>> StatsCacheFactory(string name, long ttlms, UserDataCache<Dictionary<string, string>>.CacheResolver resolver, IDependencyProvider provider)
	        {
		        return new EphemeralUserDataCache<Dictionary<string, string>>(name, resolver);
	        }
	        
	        UserDataCache<RankEntry> LeaderboardRankEntryFactory(string name, long ttlms, UserDataCache<RankEntry>.CacheResolver resolver, IDependencyProvider provider)
	        {
		        return new EphemeralUserDataCache<RankEntry>(name, resolver);
	        }
	        
	        IBeamableServices ExtractSdks(IServiceProvider provider)
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
			        Notifications = provider.GetRequiredService<IMicroserviceNotificationsApi>(),
			        Social = provider.GetRequiredService<IMicroserviceSocialApi>(),
			        Tournament = provider.GetRequiredService<IMicroserviceTournamentApi>(),
			        TrialData = provider.GetRequiredService<IMicroserviceCloudDataApi>(),
			        RealmConfig= provider.GetRequiredService<IMicroserviceRealmConfigService>(),
			        Commerce = provider.GetRequiredService<IMicroserviceCommerceApi>(),
			        Chat = provider.GetRequiredService<IMicroserviceChatApi>(),
			        Payments = provider.GetRequiredService<IMicroservicePaymentsApi>(),
		        };
		        return services;
	        }
        }

        public static void InitializeServices(IServiceProvider provider)
        {
	        // we need to init the mongo serialization service
	        var mongo = provider.GetService<MongoSerializationService>();
	        mongo.Init();
        }

        public static async Task Start<TMicroService>() where TMicroService : Microservice
        {
	        var envArgs = new EnviornmentArgs();
	        ConfigureLogging(envArgs);
	        ConfigureUncaughtExceptions();
	        ConfigureUnhandledError();
	        ConfigureDocsProvider();
	        
	        ReflectionCache = ConfigureReflectionCache();
	        
	        // configure the root service scope, and then build the root service provider.
	        var serviceBuilder = ConfigureServices<TMicroService>(envArgs);
	        var rootServiceScope = serviceBuilder.Build(new BuildOptions
	        {
		        allowHydration = false
	        });
	        InitializeServices(rootServiceScope);
	        
	        
	        var args = envArgs.Copy(conf =>
	        {
		        conf.ServiceScope = rootServiceScope;
	        });

	        var attribute = typeof(TMicroService).GetCustomAttribute<MicroserviceAttribute>();
	        for (var i = 0; i < args.BeamInstanceCount; i++)
            {
	            var isFirstInstance = i == 0;
	            var beamableService = new BeamableMicroService();
				Instances.Add(beamableService);
				
				var instanceArgs = args.Copy(conf =>
				{
					// only the first instance needs to run, if anything should run at all.
					conf.DisableCustomInitializationHooks |= !isFirstInstance;
				});
				
	            if (isFirstInstance)
	            {
		            var localDebug = new ContainerDiagnosticService(instanceArgs, beamableService);
		            var runningDebugTask = localDebug.Run();
	            }
	            
	            if (!string.Equals(args.SdkVersionExecution, args.SdkVersionBaseBuild))
	            {
		            Log.Fatal(
			            "Version mismatch. Image built with {buildVersion}, but is executing with {executionVersion}. This is a fatal mistake.",
			            args.SdkVersionBaseBuild, args.SdkVersionExecution);
		            throw new Exception(
			            $"Version mismatch. Image built with {args.SdkVersionBaseBuild}, but is executing with {args.SdkVersionExecution}. This is a fatal mistake.");
	            }

	            try
	            {
		            await beamableService.Start<TMicroService>(instanceArgs);
		            if (isFirstInstance && (attribute?.EnableEagerContentLoading ?? false))
		            {
			            await rootServiceScope.GetService<ContentService>().initializedPromise;
		            }
	            }
	            catch (Exception ex)
	            {
		            var message = new StringBuilder(1024 * 10);

		            if (ex is not BeamableMicroserviceException beamEx)
			            message.AppendLine(
				            $"[BeamErrorCode=BMS{BeamableMicroserviceException.kBMS_UNHANDLED_EXCEPTION_ERROR_CODE}]" +
				            $" Unhandled Exception Found! Please notify Beamable of your use case that led to this.");
		            else
			            message.AppendLine($"[BeamErrorCode=BMS{beamEx.ErrorCode}] " +
			                               $"Beamable Exception Found! If the message is unclear, please contact Beamable with your feedback.");

		            message.AppendLine("Exception Info:");
		            message.AppendLine($"Name={ex.GetType().Name}, Message={ex.Message}");
		            message.AppendLine("Stack Trace:");
		            message.AppendLine(ex.StackTrace);
		            Log.Fatal(message.ToString());
		            throw;
	            }

	            if (args.WatchToken)
		            HotReloadMetadataUpdateHandler.ServicesToRebuild.Add(beamableService);

	            var _ = beamableService.RunForever();
            }

            await Task.Delay(-1);
        }
    }

    internal class LogMsgSizeEnricher : ILogEventEnricher
    {
	    private readonly int _width;
	    
	    public LogMsgSizeEnricher(int width)
	    {
		    _width = width;
	    }

	    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	    {
		    foreach (var singleProp in logEvent.Properties)
		    {
			    var typeName = singleProp.Value?.ToString();

			    if (typeName != null && typeName.Length > _width)
			    {
				    typeName = typeName.Substring(0, _width) + "...";
			    }

			    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(singleProp.Key, typeName));
		    }

	    }
    }
}
