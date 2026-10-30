using cli.Services.HttpServer;

namespace cli.Mcp;

/// <summary>Finds the running `beam server serve --enable-mcp` for a workspace, from the locator files servers write.</summary>
public static class McpLocalServer
{
	/// <summary>
	/// The newest live server in the workspace that serves MCP. With <paramref name="url"/>, only the server at that
	/// MCP URL (what an agent was configured with) qualifies.
	/// </summary>
	public static ServerLocator Find(ConfigService config, string url = null) =>
		config?.DirectoryExists == true ? Find(config.ConfigTempDirectoryPath, url) : null;

	/// <inheritdoc cref="Find(ConfigService, string)"/>
	public static ServerLocator Find(string configTempDirectory, string url = null)
	{
		return ServerLocatorFile.ReadLive(configTempDirectory, prune: true)
			.Where(l => !string.IsNullOrEmpty(l.mcpUrl))
			.FirstOrDefault(l => string.IsNullOrEmpty(url) || SameEndpoint(l.mcpUrl, url));
	}

	static bool SameEndpoint(string a, string b) =>
		Uri.TryCreate(a, UriKind.Absolute, out var ua) && Uri.TryCreate(b, UriKind.Absolute, out var ub) &&
		ua.Port == ub.Port && string.Equals(ua.AbsolutePath.TrimEnd('/'), ub.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal);

	public const string NOT_RUNNING =
		"No `beam server serve --enable-mcp` is running for this workspace. Open the project in Unity with " +
		"'Enable MCP for AI Agents' on, or run `beam server serve --enable-mcp` in a terminal.";
}
