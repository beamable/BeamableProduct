using System.CommandLine;

namespace cli;

public class DotnetPathOption : Option<string>
{
	public static DotnetPathOption Instance = new DotnetPathOption();
	private DotnetPathOption() : base(name: "--dotnet-path", 
		description: "a custom location for dotnet",
		getDefaultValue: () => "dotnet")
	{
	}
}
