using cli;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		var app = new App();
		app.Configure();
		app.Build();
		
		return await app.RunAsync(args);
	}
}
