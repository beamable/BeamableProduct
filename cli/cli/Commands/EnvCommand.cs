using cli.Services;

namespace cli.Commands;

public class EnvCommandArgs : CommandArgs
{
}

public class EnvCommandResult
{
	public Dictionary<string, string> environmentVariables;
	public Dictionary<string, string> aiDetection;
}

public class EnvCommand : AtomicCommand<EnvCommandArgs, EnvCommandResult>, IStandaloneCommand, ISkipManifest
{
	private static readonly string[] SensitivePatterns =
		{ "SECRET", "TOKEN", "PASSWORD", "KEY", "CREDENTIAL", "AUTH" };

	public override bool IsForInternalUse => true;

	public EnvCommand() : base("env", "Display environment variables and AI agent detection status")
	{
	}

	public override void Configure()
	{
	}

	public override Task<EnvCommandResult> GetResult(EnvCommandArgs args)
	{
		var allVars = Environment.GetEnvironmentVariables();
		var envDict = new Dictionary<string, string>();
		foreach (System.Collections.DictionaryEntry entry in allVars)
		{
			var key = entry.Key.ToString();
			var value = entry.Value?.ToString() ?? "";
			var isSensitive = SensitivePatterns.Any(p =>
				key.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
			envDict[key] = isSensitive ? "***REDACTED***" : value;
		}

		var aiVarNames = new[]
		{
			"AI_AGENT", "CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT",
			"CURSOR_SESSION_ID", "COPILOT_AGENT",
			"WINDSURF_SESSION", "OPENCODE_SESSION", "AIDER"
		};

		var aiDetection = new Dictionary<string, string>();
		foreach (var name in aiVarNames)
		{
			var value = Environment.GetEnvironmentVariable(name);
			aiDetection[name] = value ?? "(not set)";
		}

		return Task.FromResult(new EnvCommandResult
		{
			environmentVariables = envDict.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value),
			aiDetection = aiDetection
		});
	}
}
