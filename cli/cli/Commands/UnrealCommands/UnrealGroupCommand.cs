using JetBrains.Annotations;

namespace cli.UnrealCommands;

public class UnrealGroupCommand : CommandGroup
{
	public override bool IsForInternalUse => true;

	public UnrealGroupCommand() : base("unreal", "Commands that are specific to Unreal game integrations")
	{
	}
}
