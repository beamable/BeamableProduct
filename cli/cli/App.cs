using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using cli.Content;
using cli.Services;
using cli.Services.Content;
using cli.Unreal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace cli;

public class App
{
	public static LoggingLevelSwitch LogLevel { get; set; }

	public ServiceCollection Services { get; set; }
	public IServiceProvider Provider { get; set; }


	public App()
	{
		Services = new ServiceCollection();
	}

	public bool IsBuilt => Provider != null;

	private static void ConfigureLogging()
	{
		// The LoggingLevelSwitch _could_ be controlled at runtime, if we ever wanted to do that.
		LogLevel = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Information };

		// https://github.com/serilog/serilog/wiki/Configuration-Basics
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console(LogLevel.MinimumLevel)
			.CreateLogger();

		BeamableLogProvider.Provider = new CliSerilogProvider();
		CliSerilogProvider.LogContext.Value = Log.Logger;
	}


	public virtual void Configure(Action<ServiceCollection>? configurator = null)
	{
		if (IsBuilt)
			throw new InvalidOperationException("The app has already been built, and cannot be configured anymore");

		ConfigureLogging();

		// add global options
		Services.AddSingleton<DryRunOption>();
		Services.AddSingleton<CidOption>();
		Services.AddSingleton<PidOption>();
		Services.AddSingleton<PlatformOption>();
		Services.AddSingleton<LimitOption>();
		Services.AddSingleton<SkipOption>();
		Services.AddSingleton<DeployFilePathOption>();
		Services.AddSingleton<AccessTokenOption>();
		Services.AddSingleton<RefreshTokenOption>();
		Services.AddSingleton<LogOption>();
		Services.AddSingleton(provider =>
		{
			var root = new RootCommand();
			root.AddGlobalOption(provider.GetRequiredService<DryRunOption>());
			root.AddGlobalOption(provider.GetRequiredService<CidOption>());
			root.AddGlobalOption(provider.GetRequiredService<PidOption>());
			root.AddGlobalOption(provider.GetRequiredService<PlatformOption>());
			root.AddGlobalOption(provider.GetRequiredService<RefreshTokenOption>());
			root.AddGlobalOption(provider.GetRequiredService<LogOption>());
			root.Description = "A CLI for interacting with the Beamable Cloud.";
			return root;
		});

		// register services
		Services.AddSingleton<IAppContext, DefaultAppContext>();
		Services.AddSingleton<IRealmsApi, RealmsService>();
		Services.AddSingleton<IAliasService, AliasService>();
		Services.AddSingleton<IBeamableRequester>(provider => provider.GetRequiredService<CliRequester>());
		Services.AddSingleton<CliRequester, CliRequester>();
		Services.AddSingleton<IAuthSettings, DefaultAuthSettings>();
		Services.AddSingleton<IAuthApi, AuthApi>();
		Services.AddSingleton<ConfigService>();
		Services.AddSingleton<BeamoService>();
		Services.AddSingleton<BeamoLocalSystem>();
		Services.AddSingleton<ContentLocalCache>();
		Services.AddSingleton<ContentService>();
		Services.AddSingleton<CliEnvironment>();
		Services.AddSingleton<SwaggerService>();
		Services.AddSingleton<ISwaggerStreamDownloader, SwaggerStreamDownloader>();
		Services.AddSingleton<SwaggerService.ISourceGenerator, UnitySourceGenerator>();
		Services.AddSingleton<SwaggerService.ISourceGenerator, UnrealSourceGenerator>();

		// add commands
		Services.AddRootCommand<InitCommand, InitCommandArgs>();
		Services.AddRootCommand<AccountMeCommand, AccountMeCommandArgs>();
		Services.AddRootCommand<BaseRequestGetCommand, BaseRequestArgs>();
		Services.AddRootCommand<BaseRequestPutCommand, BaseRequestArgs>();
		Services.AddRootCommand<BaseRequestPostCommand, BaseRequestArgs>();
		Services.AddRootCommand<BaseRequestDeleteCommand, BaseRequestArgs>();
		Services.AddRootCommand<ConfigCommand, ConfigCommandArgs>();
		Services.AddCommand<ConfigSetCommand, ConfigSetCommandArgs, ConfigCommand>();
		Services.AddRootCommand<LoginCommand, LoginCommandArgs>();
		Services.AddRootCommand<OpenAPICommand, OpenAPICommandArgs>();
		Services.AddCommand<GenerateSdkCommand, GenerateSdkCommandArgs, OpenAPICommand>();
		Services.AddCommand<DownloadOpenAPICommand, DownloadOpenAPICommandArgs, OpenAPICommand>();


		Services.AddRootCommand<ServicesCommand, ServicesCommandArgs>();
		Services.AddCommand<ServicesManifestsCommand, ServicesManifestsArgs, ServicesCommand>();
		Services.AddCommand<ServicesListCommand, ServicesListCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesRegisterCommand, ServicesRegisterCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesModifyCommand, ServicesModifyCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesEnableCommand, ServicesEnableCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesDeployCommand, ServicesDeployCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesResetCommand, ServicesResetCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesTemplatesCommand, ServicesTemplatesCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesRegistryCommand, ServicesRegistryCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesUploadApiCommand, ServicesUploadApiCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesLogsUrlCommand, ServicesLogsUrlCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesMetricsUrlCommand, ServicesMetricsUrlCommandArgs, ServicesCommand>();
		Services.AddCommand<ServicesPromoteCommand, ServicesPromoteCommandArgs, ServicesCommand>();

		Services.AddRootCommand<ContentCommand, ContentCommandArgs>();
		Services.AddCommand<ContentPullCommand, ContentPullCommandArgs, ContentCommand>();
		Services.AddCommand<ContentStatusCommand, ContentStatusCommandArgs, ContentCommand>();
		Services.AddCommand<ContentOpenCommand, ContentOpenCommandArgs, ContentCommand>();
		Services.AddCommand<ContentPublishCommand, ContentPublishCommandArgs, ContentCommand>();
		Services.AddCommand<ContentResetCommand, ContentResetCommandArgs, ContentCommand>();
		// customize
		configurator?.Invoke(Services);
	}

	public virtual void Build()
	{
		if (IsBuilt)
			throw new InvalidOperationException("The app has already been built, and cannot be built again");

		// create the service scope
		Provider = Services.BuildServiceProvider();

		// automatically create all commands
		Provider.GetServices<ICommandFactory>();
	}


	protected virtual Parser GetProgram()
	{
		var root = Provider.GetRequiredService<RootCommand>();
		var commandLineBuilder = new CommandLineBuilder(root);
		commandLineBuilder.AddMiddleware(consoleContext =>
	   {
		   var appContext = Provider.GetRequiredService<IAppContext>();
		   appContext.Apply(consoleContext.BindingContext);
	   }, MiddlewareOrder.Configuration);
		commandLineBuilder.UseDefaults();
		commandLineBuilder.UseSuggestDirective();
		commandLineBuilder.UseTypoCorrections();
		commandLineBuilder.UseExceptionHandler((ex, ctx) =>
		{
			switch (ex)
			{
				case CliException cliException:
					Console.Error.WriteLine(cliException.Message);
					break;
				default:
					Console.Error.WriteLine(ex.Message);
					Console.Error.WriteLine(ex.StackTrace);
					break;
			}
		});
		return commandLineBuilder.Build();
	}

	public virtual int Run(string[] args)
	{
		var prog = GetProgram();
		return prog.Invoke(args);
	}

	public virtual Task<int> RunAsync(string[] args)
	{
		var prog = GetProgram();
		return prog.InvokeAsync(args);
	}
}
