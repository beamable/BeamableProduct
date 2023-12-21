using Beamable.Common;
using cli.Dotnet;
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
		if (unrealArgs == null) throw new Exception("didn't work");

		var msModuleName = unrealArgs.msModuleName;
		if (string.IsNullOrEmpty(msModuleName)) msModuleName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the Runtime module name to which we'll add the generated Microservice Client subsystem:"));
		var msModulePath = Path.Combine(unrealArgs.ConfigService.BaseDirectory, relativePath + @"\Source\", msModuleName);
		if (!Directory.Exists(msModulePath))
		{
			AnsiConsole.Write(new Text($"Module entered was not found in {msModulePath}.", new Style(Color.Red)));
			return;
		}
		// TODO: It'd be nice to add a validation that the entered module was in-fact a Runtime module.
		// TODO: We can do this by looking at the .uproject file.

		var bpNodesModuleName = unrealArgs.bpModuleName;
		if (string.IsNullOrEmpty(bpNodesModuleName)) bpNodesModuleName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the UncookedOnly module name to which we'll add the generated Microservice Client's helper BP Nodes:").DefaultValue($"{msModuleName}BlueprintNodes"));
		var bpNodesModulePath = Path.Combine(unrealArgs.ConfigService.BaseDirectory, relativePath + @"\Source\", bpNodesModuleName);
		if (!Directory.Exists(bpNodesModulePath))
		{
			AnsiConsole.Write(new Text($"Module entered was not found in {bpNodesModulePath}.", new Style(Color.Red)));
			return;
		}
		// TODO: It'd be nice to add a validation that the entered module was in-fact an UncookedOnly module.
		// TODO: We can do this by looking at the .uproject file.

		var msModulePublicPrivate = unrealArgs.msModulePublicPrivate ?? AnsiConsole.Prompt(new ConfirmationPrompt($"Does the selected Runtime module ({msModuleName}) split files between Public/Private folders?"));
		var bpNodesPublicPrivate = unrealArgs.bpModulePublicPrivate ?? AnsiConsole.Prompt(new ConfirmationPrompt($"Does the selected UncookedOnly module ({bpNodesModuleName}) split files between Public/Private folders?"));

		unrealArgs.ProjectService.AddUnrealProject(relativePath, msModuleName, bpNodesModuleName, msModulePublicPrivate, bpNodesPublicPrivate);
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
			case 1 when !args.quiet && AnsiConsole.Confirm($"Automatically found {defaultPaths[0]}. Add as {_projectClientTypeName} project?"):
				_client.AddProject(defaultPaths[0], args);
				return true;
			case 1 when args.quiet:
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

	private bool IsValidProjectClientDirectory(ref string path)
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
