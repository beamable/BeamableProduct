using Beamable.Common.Content;
using Beamable.Serialization;
using cli.Deployment.Services;
using System.CommandLine;
using Beamable.Server;

namespace cli.DeploymentCommands;

public delegate void ProgressHandler(string name, float ratio, bool isKnownLength=true, string serviceName=null);

public class DeployArgs
{

	public static void AddDockerComposeOutputOptions<TArgs>(AppCommand<TArgs> command)
		where TArgs : CommandArgs, IHasDockerComposeArgs
	{
		command.AddOption(new Option<string>(new string[]{"--docker-compose-dir", "-dcd"}, () => "", 
				description: $"Specify an output path where a new docker-compose project will be created. " +
				             $"The compose file can be used to run services locally. " +
				             $"(Note, existing files in this folder will be overwritten)"),
			(args, i) => args.DockerComposeDirectoryPath = i);
	}
	
	public static void AddPlanOptions<TArgs>(AppCommand<TArgs> command)
		where TArgs : CommandArgs, IHasDeployPlanArgs
	{
		
		command.AddOption(new Option<string>(new string[]{"--comment", "-c"}, () => "", $"Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal"),
			(args, i) => args.Comment = i);
		
		// TODO: is it ACTUALLY helpful to have service level comments anymore? Push on this again. 
		command.AddOption(new Option<string[]>(new string[]{"--service-comments", "-sc"}, Array.Empty<string>, $"Any number of strings in the format BeamoId::Comment" +
				$"\nAssociates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal")
			{
				AllowMultipleArgumentsPerToken = true
			},
			(args, i) => args.ServiceComments = i);

		command.AddOption(new Option<string>(new string[] { "--from-manifest", "--manifest", "-m" }, "A manifest json file to use to create a plan"),
			(args, i) => args.FromManifestFile = i);
		command.AddOption(new Option<string>(new string[] { "--from-manifest-id", "--manifest-id", "-i" }, "A manifest id to download and use to create a plan"),
			(args, i) => args.ManifestId = i);
		
		
		command.AddOption(new Option<bool>(new string[] { "--run-health-checks", "--health", "-h" }, "Run health checks on services"),
			(args, i) => args.RunHealthChecks = i);
		
		command.AddOption(new Option<bool>(new string[] { "--restart", "--redeploy", "--roll" }, "Restart existing deployed services"),
			(args, i) => args.UseLatestDeployedManifest = i);
		
		command.AddOption(new Option<bool>(new string[] { "--build-sequentially", "-bs" }, "Build services sequentially instead of all together"),
			(args, i) => args.UseSequentialBuild = i);

		command.AddOption(new Option<int>(new string[] { "--max-parallel-count" }, () => 8, "Maximum number of services to build in parallel (default: 8)"),
			(args, i) => args.MaxParallelCount = i);

		
		AddModeOption(command, (args, i) => args.DeployMode = i);
	}
	
	

	public static void AddArchivedOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, OptionalBool> binder)
		where TArgs : CommandArgs
	{
		command.AddOption(
			new Option<bool>(new string[] { "--show-archived", "-a" },
				"Include archived (removed) services"), (args, showArchived) =>
			{
				var archivedOption = new OptionalBool(false);
				if (showArchived)
				{
					// the API semantics are a bit fuzzy.
					//  if you set the "archived" option to TRUE or FALSE, then you'll
					//  ONLY get services&storages whose "archived" setting matches your given filter. 
					// The default case of this command is to only show things that are not archived, 
					//  hence the false.
					// However, if the user has specified the -a flag, then they want to see all the stuff, 
					//  which means we want to send NEITHER true OR false. 
					archivedOption.Clear();
				}
				binder?.Invoke(args, archivedOption);
			});
	}
	public static void AddModeOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, DeployMode> binder)
		where TArgs : CommandArgs
	{
		var additiveOption = new Option<bool>("--merge",  "Create a Release that merges your current local environment to the existing remote services. Existing deployed services will not be removed");
		var replacementOption = new Option<bool>("--replace", "Create a Release that completely overrides the existing remote services. Existing deployed services that are not present locally will be removed (default)");
		
		command.AddOption(additiveOption, (_, _) => { });
		command.AddOption(replacementOption, ((args, ctx, replace) =>
		{
			var additive = ctx.ParseResult.GetValueForOption(additiveOption);
			if (additive && replace)
			{
				throw new CliException("Cannot pass both --merge and --replace");
			}

			if (additive)
			{
				binder?.Invoke(args, DeployMode.Additive);
			} else if (replace)
			{
				binder?.Invoke(args, DeployMode.Replace);
			}
			else
			{
				// default mode, use replace.
				binder?.Invoke(args, DeployMode.Replace);
			}
		}));
	}
	
	public static async Task MaybeSaveToFile<T>(string toFile, T instance, string note="saving to file")
		where T : JsonSerializable.ISerializable
	{
		if (string.IsNullOrEmpty(toFile))
		{
			// TODO: put this check at the call-sites.
			return;
		}
		
		var path = Path.GetFullPath(toFile);
		var dir = Path.GetDirectoryName(path);
		Directory.CreateDirectory(dir);

		var json = JsonSerializable.ToJson(instance);
		Log.Information($"{note}: " + path);
		await File.WriteAllTextAsync(path, json);

	}
}
