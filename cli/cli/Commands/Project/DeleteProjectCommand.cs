using cli.Dotnet;
using cli.Services;

namespace cli.Commands.Project;

[Serializable]
public class DeleteProjectCommandArgs : SolutionCommandArgs
{

	public List<string> services = new List<string>();
	public List<string> withServiceTags = new List<string>();
	public List<string> withoutServiceTags = new List<string>();

}

[Serializable]
public class DeleteProjectCommandOutput
{
	
}

public class DeleteProjectCommand : AtomicCommand<DeleteProjectCommandArgs, DeleteProjectCommandOutput>
{
	public DeleteProjectCommand() : base("remove", "removes the local source code for a service or storage.")
	{
	}

	public override void Configure()
	{
		SolutionCommandArgs.Configure(this);
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		ProjectCommand.AddServiceTagsOption(this, 
			bindWithTags: (args, i) => args.withServiceTags = i, 
			bindWithoutTags: (args, i) => args.withoutServiceTags = i);
	}

	public override async Task<DeleteProjectCommandOutput> GetResult(DeleteProjectCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, 
			args.withServiceTags, 
			args.withoutServiceTags, 
			true, 
			ref args.services);

		var output = new DeleteProjectCommandOutput();

		var slnFile = args.SlnFilePath;
		var tasks = new List<Task>();
		foreach (var service in args.services)
		{
			if (!args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(service, out var sd))
			{
				throw new CliException($"no definition exists for service=[{service}]");
			}
			
			Directory.Delete(sd.ProjectDirectory, recursive: true);
			
			var runTask = CliExtensions
				.GetDotnetCommand(args.AppContext.DotnetPath, $"sln {slnFile} remove {sd.ProjectPath}")
				.ExecuteAsyncAndLog();
			
			tasks.Add(runTask);
		}

		await Task.WhenAll(tasks);
		return output;
	}
}
