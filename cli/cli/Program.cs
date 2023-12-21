using cli;
using Serilog;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
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
