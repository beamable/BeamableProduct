using System.CommandLine;

namespace cli.Options;

public class PreferRemoteFederationOption : Option<bool>
{
	public static readonly PreferRemoteFederationOption Instance = new PreferRemoteFederationOption();
	private PreferRemoteFederationOption() : base(
		name: "--prefer-remote-federation", 
		description: "By default, any local CLI invocation that should trigger a Federation of any type will prefer locally " +
		             "running Microservices. However, if you need the CLI to use the remotely running Microservices, use this option " +
		             "to ignore locally running services. ")
	{
		AddAlias("-prf");
	}
}
