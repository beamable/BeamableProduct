using cli.Services;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli.Mcp;

public class InstallAISkillsCommandArgs : CommandArgs
{
	public bool claude;
	public bool cursor;
	public bool windsurf;
	public bool opencode;
	public bool force;
}

[Serializable]
public class InstallAISkillsCommandResult
{
	public string[] targetDirectories;
	public int installedCount;
	public int skippedCount;
	public string[] installedFiles;
	public string[] skippedFiles;
	public string fallbackMessage;
}

public class InstallAISkillsCommand
	: AtomicCommand<InstallAISkillsCommandArgs, InstallAISkillsCommandResult>
	, IStandaloneCommand
	, ISkipManifest
{
	private static readonly (string flag, string dirName, string skillsSubPath)[] KnownAgents =
	{
		("claude", ".claude", Path.Combine(".claude", "skills")),
		("cursor", ".cursor", Path.Combine(".cursor", "rules")),
		("windsurf", ".windsurf", Path.Combine(".windsurf", "rules")),
		("opencode", ".opencode", Path.Combine(".opencode", "skills")),
	};

	public InstallAISkillsCommand()
		: base("install-ai-skill", "Install Beamable skill guides into AI agent directories for the current project")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<bool>(
				"--claude",
				"Install to .claude/skills/"),
			(args, v) => args.claude = v);

		AddOption(new Option<bool>(
				"--cursor",
				"Install to .cursor/rules/"),
			(args, v) => args.cursor = v);

		AddOption(new Option<bool>(
				"--windsurf",
				"Install to .windsurf/rules/"),
			(args, v) => args.windsurf = v);

		AddOption(new Option<bool>(
				"--opencode",
				"Install to .opencode/skills/"),
			(args, v) => args.opencode = v);

		AddOption(new Option<bool>(
				"--force",
				"Overwrite existing skill files even if they have been customized"),
			(args, v) => args.force = v);
	}

	public override Task<InstallAISkillsCommandResult> GetResult(InstallAISkillsCommandArgs args)
	{
		var configDir = args.ConfigService?.ConfigDirectoryPath;
		if (string.IsNullOrWhiteSpace(configDir) || args.ConfigService?.DirectoryExists != true)
			throw new CliException(
				"No .beamable workspace found. Run 'beam init' first, or run this command from inside a Beamable project.");

		var projectRoot = args.ConfigService.BeamableWorkspace;
		var embedded = McpToolExecutor.GetEmbeddedSkills();
		var installed = new List<string>();
		var skipped = new List<string>();

		var explicitFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
		{
			["claude"] = args.claude,
			["cursor"] = args.cursor,
			["windsurf"] = args.windsurf,
			["opencode"] = args.opencode,
		};

		var hasExplicitFlag = explicitFlags.Values.Any(v => v);
		var targetDirs = new List<string>();
		string fallbackMessage = null;

		if (hasExplicitFlag)
		{
			foreach (var (flag, _, skillsSubPath) in KnownAgents)
			{
				if (!explicitFlags[flag]) continue;
				var dir = Path.Combine(projectRoot, skillsSubPath);
				targetDirs.Add(dir);
			}
		}
		else
		{
			foreach (var (_, dirName, skillsSubPath) in KnownAgents)
			{
				if (Directory.Exists(Path.Combine(projectRoot, dirName)))
					targetDirs.Add(Path.Combine(projectRoot, skillsSubPath));
			}

			if (targetDirs.Count == 0)
			{
				var fallbackDir = Path.Combine(configDir, ConfigService.LOCAL_FOLDER_NAME, "skills");
				targetDirs.Add(fallbackDir);
				fallbackMessage =
					$"Could not detect an AI agent directory (.claude/, .cursor/, .windsurf/, .opencode/). " +
					$"Skills were installed to the fallback location: {fallbackDir}. " +
					$"To install for a specific agent, use --claude, --cursor, --windsurf, or --opencode.";
			}
		}

		foreach (var skillsDir in targetDirs)
		{
			Directory.CreateDirectory(skillsDir);
			InstallSkillsTo(skillsDir, embedded, args.force, installed, skipped);
			UpdateManifest(skillsDir, embedded);
		}

		return Task.FromResult(new InstallAISkillsCommandResult
		{
			targetDirectories = targetDirs.ToArray(),
			installedCount = installed.Count,
			skippedCount = skipped.Count,
			installedFiles = installed.ToArray(),
			skippedFiles = skipped.ToArray(),
			fallbackMessage = fallbackMessage
		});
	}

	private static void InstallSkillsTo(
		string skillsDir,
		List<(string name, string content)> embedded,
		bool force,
		List<string> installed,
		List<string> skipped)
	{
		foreach (var (name, content) in embedded)
		{
			var filePath = Path.Combine(skillsDir, name + ".md");
			if (File.Exists(filePath) && !force)
			{
				skipped.Add(filePath);
				continue;
			}

			File.WriteAllText(filePath, content);
			installed.Add(filePath);
		}
	}

	private static void UpdateManifest(string skillsDir, List<(string name, string content)> embedded)
	{
		var manifestPath = Path.Combine(skillsDir, "skills-manifest.json");

		var manifest = new Dictionary<string, object>();
		if (File.Exists(manifestPath))
		{
			try
			{
				var existing = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(
					File.ReadAllText(manifestPath));
				if (existing?.TryGetValue("skills", out var existingSkills) == true)
				{
					foreach (var kvp in existingSkills)
						manifest[kvp.Key] = kvp.Value;
				}
			}
			catch
			{
				// Corrupted manifest — rebuild from scratch.
			}
		}

		foreach (var (name, content) in embedded)
		{
			var summary = content.Split('\n', 2)[0].Trim();
			manifest[name] = new { summary, file = name + ".md" };
		}

		var result = new { skills = manifest };
		File.WriteAllText(manifestPath, JsonConvert.SerializeObject(result, Formatting.Indented));
	}
}
