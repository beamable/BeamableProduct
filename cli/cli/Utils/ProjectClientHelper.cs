using Beamable.Common;
using Spectre.Console;

namespace cli.Utils;

public interface IProjectClient
{
	string GetProjectClientTypeName();
}

public class UnityProjectClient : IProjectClient
{
	public string GetProjectClientTypeName() => "Unity";
}

public class UnrealProjectClient : IProjectClient
{
	public string GetProjectClientTypeName() => "Unreal";
}

public class ProjectClientHelper<TProjectClient> where TProjectClient : IProjectClient, new()
{
	public bool SuggestProjectClientTypeCandidates<T>(IEnumerable<string> expectedParentDirectories, T args)
		where T : CommandArgs
	{
		var defaultPaths = GetProjectClientTypeCandidates(expectedParentDirectories).ToList();
		switch (defaultPaths.Count)
		{
			// if there is only one detected file, offer to use that.
			case 1 when AnsiConsole.Confirm($"Automatically found {defaultPaths[0]}. Add as unity project?"):
				AddProjectClient(defaultPaths[0], args);
				return true;
			// if there are many detected files, offer up a list of them
			case > 0:
			{
				defaultPaths.Add("continue");
				var selectionPath = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("Select the Unity project to link, or continue to search manually")
						.AddChoices(defaultPaths)
				);
				if (selectionPath != "continue")
				{
					AddProjectClient(selectionPath, args);
					return true;
				}

				break;
			}
		}

		return false;
	}

	private static void AddProjectClient<T>(string relativePath, T args) where T : CommandArgs
	{
		switch (new TProjectClient())
		{
			case UnityProjectClient:
				args.ProjectService.AddUnityProject(relativePath);
				break;
			case UnrealProjectClient:
				args.ProjectService.AddUnrealProject(relativePath);
				break;
			default:
				throw new InvalidCastException("Invalid value for class generic type");
		}
	}

	public void FindProjectClientInDirectory(string workingDir, ref string directory)
	{
		while (!IsValidProjectClientDirectory(ref directory))
		{
			var projectClientTypeName = new TProjectClient().GetProjectClientTypeName();
			var subDirs = Directory.GetDirectories(directory).ToList();
			subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

			subDirs.Add("..");
			var dirSelection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title($"This doesn't look like a {projectClientTypeName} project. Where is it from here?")
					.AddChoices(subDirs)
			);

			directory = Path.Combine(directory, dirSelection);
			directory = Path.GetRelativePath(workingDir, directory);
		}
	}

	private static IEnumerable<string> GetProjectClientTypeCandidates(IEnumerable<string> paths)
	{
		foreach (var path in paths)
		{
			var projectClientTypeName = new TProjectClient().GetProjectClientTypeName();
			BeamableLogger.Log($"Looking in {path} for default {projectClientTypeName} projects");

			var childPaths = Directory.GetDirectories(path);
			foreach (var childPath in childPaths)
			{
				var childPathRef = childPath;
				BeamableLogger.Log($"-- looking at {childPath} ");

				if (IsValidProjectClientDirectory(ref childPathRef))
				{
					BeamableLogger.Log($"-- I think that {childPath} is a {projectClientTypeName} project");

					yield return childPath;
				}
			}
		}
	}

	private static bool IsValidProjectClientDirectory(ref string path)
	{
		try
		{
			var subDirs = Directory.GetDirectories(path).ToList();
			subDirs = subDirs.Select(x => x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1)).ToList();

			return new TProjectClient() switch
			{
				UnityProjectClient => IsUnityProjectDirectory(subDirs),
				UnrealProjectClient => IsUnrealProjectDirectory(subDirs),
				_ => throw new InvalidCastException("Invalid value for class generic type")
			};
		}
		catch (Exception e) when (e is UnauthorizedAccessException)
		{
			var folderName = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
			path = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
			BeamableLogger.LogWarning($"Skipping folder - “{folderName}” because it is protected");
			return false;
		}
	}

	private static bool IsUnityProjectDirectory(List<string> subDirs)
	{
		var hasAssets = subDirs.Contains("Assets");
		var hasPackages = subDirs.Contains("Packages");
		var hasSettings = subDirs.Contains("ProjectSettings");

		return hasAssets && hasPackages && hasSettings;
	}

	private static bool IsUnrealProjectDirectory(ICollection<string> subDirs)
	{
		var hasContent = subDirs.Contains("Content");
		var hasPlugins = subDirs.Contains("Plugins");
		var hasConfig = subDirs.Contains("Config");

		return hasContent && hasPlugins && hasConfig;
	}
}
