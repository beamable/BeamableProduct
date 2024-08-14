using JetBrains.Annotations;

namespace cli.TokenCommands;

public class TokenCommandGroup : CommandGroup
{
	public override bool IsForInternalUse => true;

	public TokenCommandGroup() : base("token", "explore Beamable tokens")
	{
	}
}
