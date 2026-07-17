using cli.Docs;

namespace cli.Commands.Docs;

public class SkillTemplateData
{
	public FederationTypeEntry[] FederationTypes { get; set; }
	public UnrealTypeMappingEntry[] UnrealTypeMappings { get; set; }
	public SkillCommandInfo[] ServiceCommands { get; set; }
	public SkillCommandInfo[] ContentCommands { get; set; }
	public Dictionary<string, SkillCommandDetail> Commands { get; set; }
}

public class SkillCommandInfo
{
	public string Name { get; set; }
	public string Description { get; set; }
}

public class SkillCommandDetail
{
	public string Name { get; set; }
	public string Description { get; set; }
	public string ExecutionPath { get; set; }
	public SkillCommandArg[] Arguments { get; set; }
	public SkillCommandOption[] Options { get; set; }
}

public class SkillCommandArg
{
	public string Name { get; set; }
	public string Description { get; set; }
	public string Type { get; set; }
	public bool IsRequired { get; set; }
}

public class SkillCommandOption
{
	public string Name { get; set; }
	public string[] Aliases { get; set; }
	public string Description { get; set; }
	public string Type { get; set; }
	public bool IsRequired { get; set; }
}
