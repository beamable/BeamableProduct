using JetBrains.Annotations;

namespace cli.CheckCommands;

public class CheckCommandCommandGroup : CommandGroup
{
    public CheckCommandCommandGroup() : base("checks", "Check your projects for known issues")
    {
    }
}