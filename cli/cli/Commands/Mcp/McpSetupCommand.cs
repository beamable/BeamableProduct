using Beamable.Common.Util;
using Beamable.Server;
using cli.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;

namespace cli.Mcp;

public class McpSetupCommandArgs : CommandArgs
{
	public string projectPath;
}

[Serializable]
public class McpSetupCommandResult
{
	public string configPath;
	/// <summary>The executable written to the MCP server entry (an absolute dotnet path when one could be found).</summary>
	public string command;
	public string[] args;
	/// <summary>Whether `dotnet beam --version` succeeded from the target directory.</summary>
	public bool beamResolves;
	public List<string> warnings = new();
}

public class McpSetupCommand
	: AtomicCommand<McpSetupCommandArgs, McpSetupCommandResult>
	, IStandaloneCommand
	, ISkipManifest
{
	public static readonly string[] ServeArgs = { "beam", "mcp", "serve" };

	public McpSetupCommand() : base("setup", "Write a .mcp.json config file so AI clients can invoke beam commands as MCP tools")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--project-path", "Directory to write the .mcp.json file into; defaults to the Beamable workspace root"),
			(args, v) => args.projectPath = v);
	}

	public override async Task<McpSetupCommandResult> GetResult(McpSetupCommandArgs args)
	{
		var targetDir = ResolveTargetDirectory(args);
		var result = new McpSetupCommandResult();

		ConfigService.EnsureDotNetToolsManifest(targetDir);

		// MCP clients often launch servers with a minimal PATH that doesn't include a dotnet installed by
		// dotnet-install.sh (~/.dotnet), so write an absolute path to the dotnet executable when we can find one.
		var dotnet = ResolveDotnetExecutable(args.AppContext?.DotnetPath);
		result.command = dotnet;
		result.args = ServeArgs;

		var configPath = Path.Combine(targetDir, ".mcp.json");

		var root = File.Exists(configPath)
			? JObject.Parse(File.ReadAllText(configPath))
			: new JObject();

		var servers = root["mcpServers"] as JObject ?? new JObject();
		servers["beamable"] = JObject.FromObject(new
		{
			command = dotnet,
			args = ServeArgs
		});
		root["mcpServers"] = servers;

		File.WriteAllText(configPath, root.ToString(Formatting.Indented));
		result.configPath = configPath;
		Log.Information($"Wrote MCP server 'beamable' to {configPath}: {dotnet} {string.Join(" ", ServeArgs)}");

		if (!Path.IsPathRooted(dotnet))
		{
			var warning = "Could not find an absolute path to the dotnet executable (checked DOTNET_HOST_PATH, DOTNET_ROOT and PATH); " +
			              "wrote plain `dotnet`, which only works if the MCP client's PATH contains dotnet.";
			result.warnings.Add(warning);
			Log.Warning(warning);
		}

		// Restore the local tool and make sure the command we wrote actually starts the CLI.
		var (restore, restoreOutput) = await CliExtensions.RunWithOutput(dotnet, "tool restore", targetDir);
		if (restore.ExitCode != 0)
		{
			var warning = $"`dotnet tool restore` failed in {targetDir}: {restoreOutput.ToString().Trim()}";
			result.warnings.Add(warning);
			Log.Warning(warning);
		}

		var (version, versionOutput) = await CliExtensions.RunWithOutput(dotnet, "beam --version", targetDir);
		result.beamResolves = version.ExitCode == 0;
		if (!result.beamResolves)
		{
			var warning = $"`{dotnet} beam --version` failed in {targetDir}, so the MCP server will not start: {versionOutput.ToString().Trim()}";
			result.warnings.Add(warning);
			Log.Warning(warning);
		}
		else
		{
			var toolProblem = await ConfigService.VerifyLocalBeamTool(dotnet, targetDir, BeamAssemblyVersionUtil.GetVersion<App>());
			if (toolProblem != null)
			{
				result.warnings.Add(toolProblem);
				Log.Warning(toolProblem);
			}
		}

		return result;
	}

	private static string ResolveTargetDirectory(McpSetupCommandArgs args)
	{
		if (!string.IsNullOrWhiteSpace(args.projectPath))
			return Path.GetFullPath(args.projectPath);

		var workspace = args.ConfigService?.BeamableWorkspace;
		return string.IsNullOrWhiteSpace(workspace)
			? Directory.GetCurrentDirectory()
			: workspace;
	}

	/// <summary>
	/// Finds an absolute path to the dotnet executable. In order: an explicit rooted --dotnet-path / BEAM_DOTNET_PATH,
	/// DOTNET_HOST_PATH, DOTNET_ROOT, the current process when it is dotnet itself (`dotnet beam ...`), then PATH.
	/// Falls back to plain "dotnet" when none of them resolves.
	/// </summary>
	public static string ResolveDotnetExecutable(
		string configuredDotnetPath = null,
		Func<string, string> getEnv = null,
		string processPath = null)
	{
		getEnv ??= Environment.GetEnvironmentVariable;
		processPath ??= Environment.ProcessPath;
		var exeName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

		if (!string.IsNullOrWhiteSpace(configuredDotnetPath) && Path.IsPathRooted(configuredDotnetPath) && File.Exists(configuredDotnetPath))
			return configuredDotnetPath;

		var hostPath = getEnv("DOTNET_HOST_PATH");
		if (!string.IsNullOrWhiteSpace(hostPath) && Path.IsPathRooted(hostPath) && File.Exists(hostPath))
			return hostPath;

		var dotnetRoot = getEnv("DOTNET_ROOT");
		if (!string.IsNullOrWhiteSpace(dotnetRoot))
		{
			var candidate = Path.Combine(dotnetRoot, exeName);
			if (File.Exists(candidate))
				return Path.GetFullPath(candidate);
		}

		if (!string.IsNullOrWhiteSpace(processPath)
		    && string.Equals(Path.GetFileName(processPath), exeName, StringComparison.OrdinalIgnoreCase)
		    && File.Exists(processPath))
			return processPath;

		var path = getEnv("PATH");
		if (!string.IsNullOrWhiteSpace(path))
		{
			foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
			{
				try
				{
					var candidate = Path.Combine(dir.Trim().Trim('"'), exeName);
					if (Path.IsPathRooted(candidate) && File.Exists(candidate))
						return Path.GetFullPath(candidate);
				}
				catch (ArgumentException)
				{
					// ignore malformed PATH entries
				}
			}
		}

		return "dotnet";
	}
}
