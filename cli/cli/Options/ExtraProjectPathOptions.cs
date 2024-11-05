using System.CommandLine;

namespace cli.Options;

public class ExtraProjectPathOptions : ConfigurableOptionList
{
	public static ExtraProjectPathOptions Instance = new ExtraProjectPathOptions();

	private ExtraProjectPathOptions()
		: base("add-project-path", "additional file paths to be included when building a local project manifest. ")
	{
	}
}
