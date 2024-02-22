namespace cli;


public class ProfilingCommand : CommandGroup, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public ProfilingCommand() : base("profile", "Commands for inspecting profiling reports")
	{
	}

}
