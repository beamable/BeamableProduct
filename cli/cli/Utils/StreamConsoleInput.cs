using Spectre.Console;

namespace cli.Utils;

public class StreamConsoleInput : IAnsiConsoleInput
{
	public StreamConsoleInput()
	{
		// Console.in
	} 
	
	public bool IsKeyAvailable()
	{
		return Console.In.Peek() > 0;
	}

	public ConsoleKeyInfo? ReadKey(bool intercept)
	{
		var data = Console.In.Read();
		return new ConsoleKeyInfo((char)data, (ConsoleKey)data, false, false, false);
	}

	public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
	{
		var result = ReadKey(intercept);
		return Task.FromResult(result);
	}
}
