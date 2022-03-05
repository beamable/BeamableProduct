using System;
using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Common;
using Core.Server.Common;
using microservice.Common;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using UnityEngine;

namespace Beamable.Server
{
    public static class MicroserviceBootstrapper
    {
        public static LoggingLevelSwitch LogLevel;

        private static void ConfigureLogging()
        {
            // TODO pull "LOG_LEVEL" into a const?
            var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "debug";
            var envLogLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), logLevel, true);

            // The LoggingLevelSwitch _could_ be controlled at runtime, if we ever wanted to do that.
            LogLevel = new LoggingLevelSwitch { MinimumLevel = envLogLevel };

            // https://github.com/serilog/serilog/wiki/Configuration-Basics
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.ControlledBy(LogLevel)
               .WriteTo.Console(new MicroserviceLogFormatter())
               .CreateLogger();

            // use newtonsoft for JsonUtility
            JsonUtilityConverter.Init();

            BeamableLogProvider.Provider = new BeamableSerilogProvider();
            Debug.Instance = new MicroserviceDebug();
            BeamableSerilogProvider.LogContext.Value = Log.Logger;
        }

        private static void ConfigureUnhandledError()
        {
            PromiseBase.SetPotentialUncaughtErrorHandler((promise, exception) =>
            {
                BeamableLogger.LogError("Uncaught promise error. {promiseType} {message} {stack}", promise?.GetType(), exception.Message, exception.StackTrace);
                throw exception;
            });
        }

        private static void ConfigureDocsProvider()
        {
            XmlDocsHelper.ProviderFactory = XmlDocsHelper.FileIOProvider;
        }

        public static void Start<TMicroService>() where TMicroService : Microservice
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

            var _ = beamableService.Start<TMicroService>(args);

            if (args.WatchToken)
            {
                TokenService.WatchTokenChange(beamableService);
            }

            beamableService.RunForever();
        }
    }
}