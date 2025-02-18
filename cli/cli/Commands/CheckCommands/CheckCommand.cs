using JetBrains.Annotations;

namespace cli.CheckCommands;

public class CheckCommandCommandGroup : CommandGroup
{
    public CheckCommandCommandGroup() : base("checks", "Check if your projects for known issues ")
    {
    }
}