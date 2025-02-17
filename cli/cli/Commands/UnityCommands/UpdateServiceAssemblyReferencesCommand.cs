using cli.Commands.Project;
using cli.Services;
using System.CommandLine;

namespace cli.UnityCommands;

public class UpdateServiceAssemblyReferencesCommandArgs : CommandArgs, IHasSolutionFileArg
{
	public string ServiceName;
	public List<string> ReferencesPaths = new List<string>();
	public List<string> ReferencesNames = new List<string>();
	
	public string SlnFilePath;

	public string SolutionFilePath
	{
		get => SlnFilePath;
		set => SlnFilePath = value;
	}
}

public class UpdateServiceAssemblyReferencesCommand : AppCommand<UpdateServiceAssemblyReferencesCommandArgs>, IEmptyResult
{
	public override bool IsForInternalUse => true;

	public UpdateServiceAssemblyReferencesCommand() : base("update-references", "Updates all Unity Assembly Definition references of the specified service")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("service", "The name of the service to update the references"), (x, i) => x.ServiceName = i);

		SolutionCommandArgs.ConfigureSolutionFlag(this, _ => throw new CliException("Must have a beamable workspace"));
		var referencePaths = new Option<List<string>>("--paths", "The path of the project that will be referenced")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true,
		};
		AddOption(referencePaths, (x, i) => x.ReferencesPaths = i);

		var referenceNames = new Option<List<string>>("--names", "The name of the Assembly Definition")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true,
		};
		AddOption(referenceNames, (x, i) => x.ReferencesNames = i);
	}

	public override async Task Handle(UpdateServiceAssemblyReferencesCommandArgs args)
	{
		if (args.ReferencesPaths.Count != args.ReferencesNames.Count)
		{
			throw new CliException("The amount of paths must be the same as of assembly definition names");
		}

		var manifest = args.BeamoLocalSystem.BeamoManifest;
		var definition = manifest.ServiceDefinitions.FirstOrDefault(s => s.BeamoId.Equals(args.ServiceName));
		if (definition == null)
		{
			throw new CliException($"Unknown service id=[{args.ServiceName}]");
		}

		await ProjectContextUtil.UpdateUnityProjectReferences(args, args.SolutionFilePath, definition, args.ReferencesPaths, args.ReferencesNames);
	}
}
