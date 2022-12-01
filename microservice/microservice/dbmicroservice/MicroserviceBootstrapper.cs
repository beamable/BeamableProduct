using System;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Common;
using Core.Server.Common;
using microservice.Common;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Server
{
    public static class MicroserviceBootstrapper
    {
	    private const int MSG_SIZE_LIMIT = 1000;

	    public static LoggingLevelSwitch LogLevel;

        private static void ConfigureLogging()
        {
            // TODO pull "LOG_LEVEL" into a const?
            var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "debug";
			var disableLogTruncate = (Environment.GetEnvironmentVariable("DISABLE_LOG_TRUNCATE")?.ToLowerInvariant() ?? "") == "true";
			
            var envLogLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), logLevel, true);

            // The LoggingLevelSwitch _could_ be controlled at runtime, if we ever wanted to do that.
            LogLevel = new LoggingLevelSwitch { MinimumLevel = envLogLevel };

            // https://github.com/serilog/serilog/wiki/Configuration-Basics
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.ControlledBy(LogLevel).Enrich.FromLogContext().Enrich.With(new LogMsgSizeEnricher(MSG_SIZE_LIMIT, !disableLogTruncate))
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

        public static async Task Start<TMicroService>() where TMicroService : Microservice
        {
            ConfigureLogging();
            ConfigureUnhandledError();
            ConfigureDocsProvider();

            var beamableService = new BeamableMicroService();
            var args = new EnviornmentArgs();

            var localDebug = new LocalDebugService(beamableService);

            if (!string.Equals(args.SdkVersionExecution, args.SdkVersionBaseBuild))
            {
                Log.Fatal("Version mismatch. Image built with {buildVersion}, but is executing with {executionVersion}. This is a fatal mistake.", args.SdkVersionBaseBuild, args.SdkVersionExecution);
                throw new Exception($"Version mismatch. Image built with {args.SdkVersionBaseBuild}, but is executing with {args.SdkVersionExecution}. This is a fatal mistake.");
            }

            try
            {
                await beamableService.Start<TMicroService>(args);
            }
            catch (Exception ex)
            {
                var message = new StringBuilder(1024 * 10);

                if (ex is not BeamableMicroserviceException beamEx)
                    message.AppendLine($"[BeamErrorCode=BMS{BeamableMicroserviceException.kBMS_UNHANDLED_EXCEPTION_ERROR_CODE}]" +
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

            beamableService.RunForever();
        }
    }

    internal class LogMsgSizeEnricher : ILogEventEnricher
    {
	    private readonly int _width;
	    private readonly bool _isEnabled;
	    
	    public LogMsgSizeEnricher(int width, bool isEnabled)
	    {
		    _width = width;
		    _isEnabled = isEnabled;
	    }
	    
	    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	    {
		    if (_isEnabled)
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
}
