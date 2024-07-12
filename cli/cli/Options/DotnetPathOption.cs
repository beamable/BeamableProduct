using System.CommandLine;

namespace cli;

public class DotnetPathOption : Option<string>
{
	public DotnetPathOption() : base(name: "--dotnet-path", 
		description: "a custom location for dotnet",
		getDefaultValue: () => "dotnet")
	{
	}
}
