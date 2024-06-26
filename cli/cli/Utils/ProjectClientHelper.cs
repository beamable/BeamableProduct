using Beamable.Common;
using cli.Dotnet;
using cli.Services;
using Spectre.Console;

namespace cli.Utils;

public interface IProjectClient
{
	IEnumerable<string> TypicalFolders { get; }

	string ProjectClientTypeName { get; }

	void AddProject<T>(string relativePath, T args) where T : CommandArgs;
}

public class UnityProjectClient : IProjectClient
{
	public IEnumerable<string> TypicalFolders { get; } = new[] { "Assets", "Packages", "ProjectSettings" };

	public string ProjectClientTypeName => "Unity";

	public void AddProject<T>(string relativePath, T args) where T : CommandArgs
	{
		args.ProjectService.AddUnityProject(relativePath);
	}
}

public class UnrealProjectClient : IProjectClient
{
	public IEnumerable<string> TypicalFolders { get; } = new[] { "Content", "Plugins", "Config" };

	public string ProjectClientTypeName => "Unreal";

	public void AddProject<T>(string relativePath, T args) where T : CommandArgs
	{
		var unrealArgs = args as UnrealAddProjectClientOutputCommandArgs;
		if (unrealArgs == null) throw new Exception("Something went wrong. Please report this to Beamable customer service.");

		var unrealRootDir = new DirectoryInfo(relativePath);
		var unrealProject = unrealRootDir.GetFiles().FirstOrDefault(f => f.Extension.Contains(".uproject"));
		var isUnrealRoot = unrealRootDir.GetFiles().Any(f => f.Extension.Contains(".uproject"));
		if (!isUnrealRoot) throw new Exception("Found an invalid Unreal root directory. Something went wrong. Please report this to Beamable customer service.");

		// Get the name of the project
		var projectName = unrealProject.Name[..unrealProject.Name.IndexOf('.')];
		var microservicePluginName = $"{projectName}MicroserviceClients";
		var microservicePluginBpName = $"{projectName}MicroserviceClientsBp";

		unrealArgs.ProjectService.AddUnrealProject(relativePath, microservicePluginName, microservicePluginBpName);
	}
}

public class ProjectClientHelper<TProjectClient> where TProjectClient : IProjectClient, new()
{
	private readonly TProjectClient _client;
	private readonly string _projectClientTypeName;

	public ProjectClientHelper()
	{
		_client = new TProjectClient();
		_projectClientTypeName = _client.ProjectClientTypeName;
	}

	public bool SuggestProjectClientTypeCandidates<T>(IEnumerable<string> expectedParentDirectories, T args)
		where T : AddProjectClientOutputCommandArgs
	{
		var defaultPaths = GetProjectClientTypeCandidates(expectedParentDirectories).ToList();
		switch (defaultPaths.Count)
		{
			// if there is only one detected file, offer to use that.
			case 1 when !args.Quiet && AnsiConsole.Confirm($"Automatically found {defaultPaths[0]}. Add as {_projectClientTypeName} project?"):
				_client.AddProject(defaultPaths[0], args);
				return true;
			case 1 when args.Quiet:
				_client.AddProject(defaultPaths[0], args);
				return true;
			// if there are many detected files, offer up a list of them
			case > 0:
			{
				defaultPaths.Add("continue");
				var selectionPath = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title($"Select the {_projectClientTypeName} project to link, or continue to search manually")
						.AddChoices(defaultPaths)
						.AddBeamHightlight()
				);
				if (selectionPath != "continue")
				{
					_client.AddProject(selectionPath, args);
					return true;
				}

				break;
			}
		}

		return false;
	}

	public void FindProjectClientInDirectory(string workingDir, ref string directory)
	{
		while (!IsValidProjectClientDirectory(ref directory))
		{
			var subDirs = Directory.GetDirectories(directory).ToList();
			subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

			subDirs.Add("..");
			var dirSelection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title($"This doesn't look like a {_projectClientTypeName} project. Where is it from here?")
					.AddChoices(subDirs)
					.AddBeamHightlight()
			);

			directory = Path.Combine(directory, dirSelection);
			directory = Path.GetRelativePath(workingDir, directory);
		}
	}

	public IEnumerable<string> GetProjectClientTypeCandidates(IEnumerable<string> paths)
	{
		foreach (var path in paths)
		{
			BeamableLogger.Log($"Looking in {path} for default {_projectClientTypeName} projects");

			var childPaths = Directory.GetDirectories(path);
			foreach (var childPath in childPaths)
			{
				var childPathRef = childPath;
				BeamableLogger.Log($"-- looking at {childPath} ");

				if (IsValidProjectClientDirectory(ref childPathRef))
				{
					BeamableLogger.Log($"-- I think that {childPath} is a {_projectClientTypeName} project");

					yield return childPath;
				}
			}
		}
	}

	public bool IsValidProjectClientDirectory(ref string path)
	{
		try
		{
			var subDirs = Directory.GetDirectories(path).ToList();
			subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

			return _client.TypicalFolders.All(folder => subDirs.Contains(folder));
		}
		catch (Exception e) when (e is UnauthorizedAccessException)
		{
			var folderName = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
			path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
			BeamableLogger.LogWarning($"Skipping folder - “{folderName}” because it is protected");
			return false;
		}
	}

	public void AddProject<T>(string directory, T args) where T : AddProjectClientOutputCommandArgs
	{
		_client.AddProject(directory, args);
	}
}
