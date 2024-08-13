using cli.Services;
using cli.Utils;
using System.CommandLine;

namespace cli;

public class ServicesGenerateTarballCommandArgs : CommandArgs
{
	public string beamoId;
	public string outputPath;
}

public class ServicesGenerateTarballCommandOutput
{
	public string outputPath;
}
public class ServicesGenerateTarballCommand : AtomicCommand<ServicesGenerateTarballCommandArgs, ServicesGenerateTarballCommandOutput>
{
	public ServicesGenerateTarballCommand() : base("bundle", "Create a bundle .tar file for the given service")
	{
	}

	public override void Configure()
	{
		var serviceIdOption = new Option<string>("--id", "The beamo id of the service to bundle");
		serviceIdOption.AddAlias("-i");
		AddOption(serviceIdOption, (args, i) => args.beamoId = i);
		
		
		var outputOption = new Option<string>("--output", "The location of the output tarball file");
		outputOption.AddAlias("-o");
		AddOption(outputOption, (args, i) => args.outputPath = i);
	}

	public override async Task<ServicesGenerateTarballCommandOutput> GetResult(ServicesGenerateTarballCommandArgs args)
	{
		return await BeamoLocalSystem.WriteTarfileToDisk(args.outputPath, args.beamoId, args);
	}
}
