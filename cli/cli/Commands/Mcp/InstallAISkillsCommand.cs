using cli.Commands.Mcp;
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
	private static readonly (string flag, string dirName, string skillsSubPath, string skillFileName)[] KnownAgents =
	{
		("claude", ".claude", Path.Combine(".claude", "skills"), "Skill.md"),
		("cursor", ".cursor", Path.Combine(".cursor", "skills"), "SKILL.md"),
		("windsurf", ".windsurf", Path.Combine(".windsurf", "skills"), "SKILL.md"),
		("opencode", ".opencode", Path.Combine(".opencode", "skills"), "SKILL.md"),
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
				"Install to .cursor/skills/"),
			(args, v) => args.cursor = v);

		AddOption(new Option<bool>(
				"--windsurf",
				"Install to .windsurf/skills/"),
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
		var targets = new List<(string dir, string skillFileName)>();
		string fallbackMessage = null;

		if (hasExplicitFlag)
		{
			foreach (var (flag, _, skillsSubPath, skillFileName) in KnownAgents)
			{
				if (!explicitFlags[flag]) continue;
				targets.Add((Path.Combine(projectRoot, skillsSubPath), skillFileName));
			}
		}
		else
		{
			foreach (var (_, dirName, skillsSubPath, skillFileName) in KnownAgents)
			{
				if (Directory.Exists(Path.Combine(projectRoot, dirName)))
					targets.Add((Path.Combine(projectRoot, skillsSubPath), skillFileName));
			}

			if (targets.Count == 0)
			{
				var fallbackDir = Path.Combine(configDir, ConfigService.LOCAL_FOLDER_NAME, "skills");
				targets.Add((fallbackDir, "SKILL.md"));
				fallbackMessage =
					$"Could not detect an AI agent directory (.claude/, .cursor/, .windsurf/, .opencode/). " +
					$"Skills were installed to the fallback location: {fallbackDir}. " +
					$"To install for a specific agent, use --claude, --cursor, --windsurf, or --opencode.";
			}
		}

		foreach (var (skillsDir, skillFileName) in targets)
		{
			Directory.CreateDirectory(skillsDir);
			InstallSkills(skillsDir, skillFileName, embedded, args.force, installed, skipped);
			UpdateManifest(skillsDir, embedded);
		}

		return Task.FromResult(new InstallAISkillsCommandResult
		{
			targetDirectories = targets.Select(t => t.dir).ToArray(),
			installedCount = installed.Count,
			skippedCount = skipped.Count,
			installedFiles = installed.ToArray(),
			skippedFiles = skipped.ToArray(),
			fallbackMessage = fallbackMessage
		});
	}

	private static void InstallSkills(
		string skillsDir,
		string skillFileName,
		List<(string name, string content)> embedded,
		bool force,
		List<string> installed,
		List<string> skipped)
	{
		foreach (var (name, content) in embedded)
		{
			var dir = Path.Combine(skillsDir, name);
			var filePath = Path.Combine(dir, skillFileName);
			if (File.Exists(filePath) && !force)
			{
				skipped.Add(filePath);
				continue;
			}

			Directory.CreateDirectory(dir);
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
			var summary = McpToolExecutor.ExtractDescription(content);
			manifest[name] = new { summary, file = name + ".md" };
		}

		var result = new { skills = manifest };
		File.WriteAllText(manifestPath, JsonConvert.SerializeObject(result, Formatting.Indented));
	}
}
