using cli.Commands.Project;
using JetBrains.Annotations;

namespace cli.DockerCommands;

public class DockerGroupCommand : CommandGroup
{
	public DockerGroupCommand() : base("docker", "Commands for managing docker")
	{
	}
}