using CliWrap;

namespace cli;

public class OrganizationCommand : CommandGroup, IStandaloneCommand
{
	public OrganizationCommand() : base("org", "Commands related to beamable organizations")
	{
	}
}
