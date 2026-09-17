namespace cli;

/// <summary>
/// Shared helper for creating or updating the <c>AGENTS.md</c> AI-agent guide in a Beamable workspace.
/// Used by <c>beam init</c> (opt-in) and <c>beam ai setup</c>.
/// </summary>
public static class AgentsFileWriter
{
	public const string AgentsFileName = "AGENTS.md";

	private const string EmbeddedResourceName = "cli.Docs.AGENTS.md";
	private const string BeginMarker = "<!-- BEGIN BEAMABLE AI GUIDE -->";
	private const string EndMarker = "<!-- END BEAMABLE AI GUIDE -->";

	public enum AgentsFileOutcome
	{
		/// <summary>The embedded guide content could not be located; nothing was written.</summary>
		NoContent,
		/// <summary>The file did not exist and was created with the full guide.</summary>
		Created,
		/// <summary>The file existed without the Beamable section, which was appended.</summary>
		Appended,
		/// <summary>The file already contained the Beamable section; nothing changed.</summary>
		AlreadyPresent
	}

	/// <summary>
	/// Ensures <paramref name="workspaceDir"/> contains an <c>AGENTS.md</c> with the Beamable AI guide.
	/// Creates the file if absent, appends a marked Beamable section if the file exists without it,
	/// and is a no-op if the section is already present.
	/// </summary>
	/// <returns>The path written to and the outcome.</returns>
	public static (string path, AgentsFileOutcome outcome) EnsureAgentsFile(string workspaceDir)
	{
		var agentsPath = Path.Combine(workspaceDir, AgentsFileName);

		var guide = ReadEmbeddedGuide();
		if (guide == null)
			return (agentsPath, AgentsFileOutcome.NoContent);

		if (!File.Exists(agentsPath))
		{
			File.WriteAllText(agentsPath, WrapInMarkers(guide));
			return (agentsPath, AgentsFileOutcome.Created);
		}

		var existing = File.ReadAllText(agentsPath);
		if (existing.Contains(BeginMarker))
			return (agentsPath, AgentsFileOutcome.AlreadyPresent);

		var separator = existing.EndsWith("\n") ? "\n" : "\n\n";
		File.AppendAllText(agentsPath, separator + WrapInMarkers(guide));
		return (agentsPath, AgentsFileOutcome.Appended);
	}

	private static string WrapInMarkers(string guide) =>
		$"{BeginMarker}\n{guide.TrimEnd()}\n{EndMarker}\n";

	private static string ReadEmbeddedGuide()
	{
		var asm = typeof(AgentsFileWriter).Assembly;
		using var stream = asm.GetManifestResourceStream(EmbeddedResourceName);
		if (stream == null)
			return null;

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
