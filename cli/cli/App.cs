using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace cli;

public class App
{
	public ServiceCollection Services { get; set; }
	public IServiceProvider Provider { get; set; }

	public App()
	{
		Services = new ServiceCollection();
	}

	public bool IsBuilt => Provider != null;

	public virtual void Configure(Action<ServiceCollection>? configurator=null)
	{
		if (IsBuilt)
			throw new InvalidOperationException("The app has already been built, and cannot be configured anymore");

		// add global options
		Services.AddSingleton<DryRunOption>();
		Services.AddSingleton<CidOption>();
		Services.AddSingleton<PidOption>();
		Services.AddSingleton<PlatformOption>();
		Services.AddSingleton(provider =>
		{
			var root = new RootCommand();
			root.AddOption(provider.GetRequiredService<DryRunOption>());
			root.AddOption(provider.GetRequiredService<CidOption>());
			root.AddOption(provider.GetRequiredService<PidOption>());
			root.AddOption(provider.GetRequiredService<PlatformOption>());
			root.Description = "A CLI for interacting with the Beamable Cloud.";
			return root;
		});

		// register services
		Services.AddSingleton<IAppContext, DefaultAppContext>();
		Services.AddSingleton<IFakeService, FakeService>();
		Services.AddSingleton<IBeamableRequester>(provider => provider.GetRequiredService<CliRequester>());
		Services.AddSingleton<CliRequester, CliRequester>();
		Services.AddSingleton<IAuthSettings, DefaultAuthSettings>();
		Services.AddSingleton<IAuthApi, AuthApi>();
		Services.AddSingleton<ConfigService>();

		// add commands
		Services.AddRootCommand<AddCommand, AddCommandArgs>();
		Services.AddRootCommand<ConfigCommand, ConfigCommandArgs>();
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
		commandLineBuilder.AddMiddleware( consoleContext =>
		{
			var appContext = Provider.GetRequiredService<IAppContext>();
			appContext.Apply(consoleContext.BindingContext);
		}, MiddlewareOrder.Configuration);
		commandLineBuilder.UseDefaults();
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
