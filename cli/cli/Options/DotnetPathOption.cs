using System.CommandLine;

namespace cli;

public class DotnetPathOption : Option<string>
{
	public DotnetPathOption() : base("--dotnet-path", "a custom location for dotnet")
	{
	}
}
