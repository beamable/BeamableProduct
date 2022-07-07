using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Microsoft.Extensions.DependencyInjection;
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
		LogLevel = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Warning };

		// https://github.com/serilog/serilog/wiki/Configuration-Basics
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.ControlledBy(LogLevel)
			.WriteTo.Console()
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
		Services.AddSingleton<AccessTokenOption>();
		Services.AddSingleton<RefreshTokenOption>();
		Services.AddSingleton<LogOption>();
		Services.AddSingleton(provider =>
		{
			var root = new RootCommand();
			root.AddOption(provider.GetRequiredService<DryRunOption>());
			root.AddOption(provider.GetRequiredService<CidOption>());
			root.AddOption(provider.GetRequiredService<PidOption>());
			root.AddOption(provider.GetRequiredService<PlatformOption>());
			root.AddOption(provider.GetRequiredService<AccessTokenOption>());
			root.AddOption(provider.GetRequiredService<RefreshTokenOption>());
			root.AddOption(provider.GetRequiredService<LogOption>());
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
		Services.AddSingleton<CliEnvironment>();

		// add commands
		Services.AddRootCommand<InitCommand, InitCommandArgs>();
		Services.AddRootCommand<AccountMeCommand, AccountMeCommandArgs>();
		Services.AddRootCommand<ConfigCommand, ConfigCommandArgs>();
		Services.AddCommand<ConfigSetCommand, ConfigSetCommandArgs, ConfigCommand>();
		Services.AddRootCommand<BeamoCommand, BeamoCommandArgs>();
		Services.AddCommand<BeamoCurrentManifestCommand, BeamoManifestArgs, BeamoCommand>();
		Services.AddCommand<BeamoManifestsCommand, BeamoManifestsArgs, BeamoCommand>();
		Services.AddRootCommand<LoginCommand, LoginCommandArgs>();

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
