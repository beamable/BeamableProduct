using cli.Utils;
using System.CommandLine;

namespace cli.Dotnet;

public class OpenSwaggerCommandArgs : CommandArgs
{
	public bool isRemote;
	public string serviceName;
}

public class OpenSwaggerCommand : AppCommand<OpenSwaggerCommandArgs>
{
	public OpenSwaggerCommand() : base("open-swagger", "opens the swagger page for a given service ")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("service-name", "the name of the service to open swagger to"), (arg, i) => arg.serviceName = i);
		AddOption(new Option<bool>("--remote", "if passed, swagger will open to the remote version of this service. Otherwise, it will try and use the local version."), (arg, i) => arg.isRemote = i);
	}

	public override Task Handle(OpenSwaggerCommandArgs args)
	{
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var url = $"{args.AppContext.Host.Replace("api", "portal")}/{cid}/games/{pid}/realms/{pid}/microservices/{args.serviceName}/docs";
		if (!args.isRemote)
		{
			url += $"?prefix={MachineHelper.GetUniqueDeviceId()}";
		}
		MachineHelper.OpenBrowser(url);
		return Task.CompletedTask;
	}
}
