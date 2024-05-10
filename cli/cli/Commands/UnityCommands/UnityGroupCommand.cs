using JetBrains.Annotations;

namespace cli.UnityCommands;

public class UnityGroupCommand : CommandGroup
{
	public UnityGroupCommand() : base("unity", "Commands that are specific to Unity game integrations")
	{
	}
}
