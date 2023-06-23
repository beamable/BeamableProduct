using Beamable.Common.Dependencies;
using cli;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace tests;

public static class Cli
{
	public static int RunWithParams(params string[] args) => RunWithParams(null, args);
	public static int RunWithParams(Action<IDependencyBuilder>? configurator, params string[] args)
	{
		var app = new App();
		app.Configure(configurator);
		app.Build();
		return app.Run(args);
	}
	public static int RunWithParams(Action<IDependencyBuilder>? configurator, Func<LoggerConfiguration, ILogger> configureLogger, params string[] args)
	{
		var app = new App();
		app.Configure(configurator, configureLogger: configureLogger);
		app.Build();
		return app.Run(args);
	}


	public static Task<int> RunAsyncWithParams(params string[] args) => RunAsyncWithParams(null, args);
	public static Task<int> RunAsyncWithParams(Action<IDependencyBuilder>? configurator, params string[] args)
	{
		var app = new App();
		app.Configure(configurator);
		app.Build();
		return app.RunAsync(args);
	}
}
