using cli;

public static class Program
{
	public static int Main(string[] args)
	{
		var app = new App();
		app.Configure();
		app.Build();
		var task = app.RunAsync(args);
		task.Wait();
		return task.Result;
	}
}