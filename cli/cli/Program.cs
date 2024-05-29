using cli;
using Microsoft.Build.Locator;
using Serilog;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		//This is necessary, so we can use MSBuild to read different projects and get their properties later on
		MSBuildLocator.RegisterDefaults();

		var app = new App();
		app.Configure();
		app.Build();

		try
		{
			return await app.RunAsync(args);
		}
		finally
		{
			await Log.CloseAndFlushAsync();
		}
	}
}
