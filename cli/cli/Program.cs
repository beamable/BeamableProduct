using cli;

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
			app.Flush();
		}
	}
}
