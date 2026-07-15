namespace cli.Sandbox;

public class SandboxCommand : CommandGroup
{
	public SandboxCommand() : base("sandbox", "Manage local sandbox sessions that let Portal author code, content, and microservice changes against this repo")
	{
	}
}
