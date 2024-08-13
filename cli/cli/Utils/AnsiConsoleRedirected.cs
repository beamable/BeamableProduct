using Spectre.Console;
using Spectre.Console.Rendering;

namespace cli.Utils;

public class AnsiConsoleRedirected : IAnsiConsole
{
	public IAnsiConsole DefaultConsole;

	public Profile Profile => DefaultConsole.Profile;
	public IAnsiConsoleCursor Cursor => DefaultConsole.Cursor;
	public IAnsiConsoleInput Input { get; }
	public IExclusivityMode ExclusivityMode => DefaultConsole.ExclusivityMode;
	public RenderPipeline Pipeline => DefaultConsole.Pipeline;

	public AnsiConsoleRedirected(IAnsiConsole defaultConsole)
	{
		DefaultConsole = defaultConsole;
		Input = new StreamConsoleInput();
		Profile.Capabilities.Interactive = true;
	}
	
	public void Clear(bool home)
	{
		DefaultConsole.Clear(home);
	}

	public void Write(IRenderable renderable)
	{
		DefaultConsole.Write(renderable);
	}
}
