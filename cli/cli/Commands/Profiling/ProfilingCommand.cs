namespace cli;


public class ProfilingCommand : CommandGroup, IStandaloneCommand
{
	public ProfilingCommand() : base("profile", "Commands for inspecting profiling reports")
	{
	}

}
