using cli;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace tests;

public static class Cli
{
	public static int RunWithParams(params string[] args) => RunWithParams(null, args);
	public static int RunWithParams(Action<ServiceCollection>? configurator, params string[] args)
	{
		var app = new App();
		app.Configure(configurator);
		app.Build();
		return app.Run(args);
	}

	public static Task<int> RunAsyncWithParams(params string[] args) => RunAsyncWithParams(null, args);
	public static Task<int> RunAsyncWithParams(Action<ServiceCollection>? configurator, params string[] args)
	{
		var app = new App();
		app.Configure(configurator);
		app.Build();
		return app.RunAsync(args);
	}
}
