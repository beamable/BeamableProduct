namespace cli;

public class OpenAPICommand : CommandGroup, IStandaloneCommand
{
	public OpenAPICommand() : base("oapi", "Commands that integrate the Beamable API and open API")
	{
	}

}
