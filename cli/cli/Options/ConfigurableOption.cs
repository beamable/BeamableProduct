using System.CommandLine;

namespace cli;

public class ConfigurableOption : Option<String>
{
	public string OptionName { get; }

	public ConfigurableOption(string optionName, string desc)
		: base($"--{optionName}", desc)
	{
		OptionName = optionName;
	}
}

public class ConfigurableOptionFlag : Option<bool>
{
	public string OptionName { get; }

	public ConfigurableOptionFlag(string optionName, string desc)
		: base($"--{optionName}", desc)
	{
		OptionName = optionName;
	}
}

public class ConfigurableOptionList : Option<IEnumerable<string>>
{
	public string OptionName { get; }

	public ConfigurableOptionList(string optionName, string desc)
		: base($"--{optionName}", desc)
	{
		OptionName = optionName;
		AllowMultipleArgumentsPerToken = true;
	}
}
