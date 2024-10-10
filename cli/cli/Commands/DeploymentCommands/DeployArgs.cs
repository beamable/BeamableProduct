using Beamable.Common.Content;
using Beamable.Serialization;
using cli.Deployment.Services;
using Serilog;
using System.CommandLine;

namespace cli.DeploymentCommands;

public delegate void ProgressHandler(string name, float ratio, bool isKnownLength=true, string serviceName=null);

public class DeployArgs
{
	
	public static void AddPlanOptions<TArgs>(AppCommand<TArgs> command)
		where TArgs : CommandArgs, IHasDeployPlanArgs
	{
		
		command.AddOption(new Option<string>(new string[]{"--comment", "-c"}, () => "", $"Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal"),
			(args, i) => args.Comment = i);
		command.AddOption(new Option<string[]>(new string[]{"--service-comments", "-sc"}, Array.Empty<string>, $"Any number of strings in the format BeamoId::Comment" +
				$"\nAssociates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal")
			{
				AllowMultipleArgumentsPerToken = true
			},
			(args, i) => args.ServiceComments = i);

		command.AddOption(new Option<string>(new string[] { "--from-manifest", "--manifest", "-m" }, "a manifest json file to use to create a plan"),
			(args, i) => args.FromManifestFile = i);
		command.AddOption(new Option<string>(new string[] { "--from-manifest-id", "--manifest-id", "-i" }, "a manifest id to download and use to create a plan"),
			(args, i) => args.ManifestId = i);
		
		
		command.AddOption(new Option<bool>(new string[] { "--run-health-checks", "--health", "-h" }, "run health checks on services"),
			(args, i) => args.RunHealthChecks = i);
		
		command.AddOption(new Option<bool>(new string[] { "--restart", "--redeploy", "--roll" }, "restart existing deployed services"),
			(args, i) => args.UseLatestDeployedManifest = i);
		
		AddModeOption(command, (args, i) => args.DeployMode = i);
	}
	
	

	public static void AddArchivedOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, OptionalBool> binder)
		where TArgs : CommandArgs
	{
		command.AddOption(
			new Option<bool>(new string[] { "--show-archived", "-a" },
				"include archived (removed) services"), (args, showArchived) =>
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
		var additiveOption = new Option<bool>("--additive",  "use an additive method for deployment.");
		additiveOption.AddAlias("-a");
		
		var replacementOption = new Option<bool>("--replace", "use a complete replacement method for deployment.");
		replacementOption.AddAlias("-r");

		command.AddOption(additiveOption, (_, _) => { });
		command.AddOption(replacementOption, ((args, ctx, replace) =>
		{
			var additive = ctx.ParseResult.GetValueForOption(additiveOption);
			if (additive && replace)
			{
				throw new CliException("Cannot pass both --additive and --replace");
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
