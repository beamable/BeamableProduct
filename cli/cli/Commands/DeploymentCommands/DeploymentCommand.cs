using JetBrains.Annotations;

namespace cli.DeploymentCommands;

public class DeploymentCommand : CommandGroup
{
	public DeploymentCommand() : base("deployment", "Commands for interacting with microservice and microstorage deployments")
	{
		AddAlias("deployments");
		AddAlias("deploy");
		AddAlias("deploys");
	}
}
