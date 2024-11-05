using cli.Services;
using System.CommandLine;

namespace cli.UnityCommands;

public class UpdateServiceDllsReferenceCommandArgs : CommandArgs
{
	public string ServiceName;
	public List<string> DllsPaths = new List<string>();
}

public class UpdateServiceDllsReferenceCommand : AppCommand<UpdateServiceDllsReferenceCommandArgs>, IEmptyResult
{
	public override bool IsForInternalUse => true;

	public UpdateServiceDllsReferenceCommand() : base("update-dlls", "Update all DLLs references of the service")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("service", "The name of the service to update the dlls"), (x, i) => x.ServiceName = i);

		var referencePaths = new Option<List<string>>("--paths", "The path of the dll that will be referenced")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true,
		};
		AddOption(referencePaths, (x, i) => x.DllsPaths = i);
	}

	public override Task Handle(UpdateServiceDllsReferenceCommandArgs args)
	{
		var manifest = args.BeamoLocalSystem.BeamoManifest;
		var definition = manifest.ServiceDefinitions.FirstOrDefault(s => s.BeamoId.Equals(args.ServiceName));
		if (definition == null)
		{
			throw new CliException($"Unknown service id=[{args.ServiceName}]");
		}

		ProjectContextUtil.UpdateProjectDlls(definition, args.DllsPaths);

		return Task.CompletedTask;
	}
}
