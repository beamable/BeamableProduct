using cli;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		// starting in net9, we need to manually disable the terminal logger.
		//  this will take effect for sub-invocations of dotnet
		//  https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/9.0/terminal-logger
		Environment.SetEnvironmentVariable("MSBUILDTERMINALLOGGER", "off");
		
		var app = new App();
		app.Configure();
		app.Build();
		try
		{
			return await app.RunAsync(args);
		}
		finally
		{
			app.Flush();
		}
	}
}
