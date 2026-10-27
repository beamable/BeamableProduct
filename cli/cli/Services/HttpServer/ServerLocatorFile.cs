using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cli.Services.HttpServer;

/// <summary>
/// What a running `beam server serve` writes about itself, so local tools (Unity, `beam mcp status`, an agent's
/// headers helper) can find it and authenticate to it without having spawned it.
/// </summary>
[Serializable]
public class ServerLocator
{
	public int pid;
	public int port;
	public string uri;
	public string owner;
	public string version;
	public string token;
	public long startedAt;

	/// <summary>The MCP endpoint's URL, or null when the server was started without --enable-mcp.</summary>
	public string mcpUrl;
}

/// <summary>
/// One <see cref="ServerLocator"/> per running server, at <c>.beamable/temp/servers/{pid}.json</c>. The files hold
/// the session token, so they are readable by the current user only, and each server deletes its own on exit.
/// </summary>
public static class ServerLocatorFile
{
	public const string FOLDER_NAME = "servers";

	public static string GetFolder(string configTempDirectory) => Path.Combine(configTempDirectory, FOLDER_NAME);

	public static string GetPath(string configTempDirectory, int pid) =>
		Path.Combine(GetFolder(configTempDirectory), $"{pid}.json");

	/// <summary>Writes <paramref name="locator"/> and arranges for it to be deleted when this process exits.</summary>
	public static string Write(string configTempDirectory, ServerLocator locator)
	{
		var folder = GetFolder(configTempDirectory);
		Directory.CreateDirectory(folder);
		var path = GetPath(configTempDirectory, locator.pid);
		var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

		var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
		if (!OperatingSystem.IsWindows())
		{
			options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}

		using (var writer = new StreamWriter(tempPath, options))
		{
			writer.Write(JsonConvert.SerializeObject(locator, Formatting.Indented));
		}

		// A rename is atomic, so a reader only ever sees a whole file.
		File.Move(tempPath, path, true);

		// Environment.Exit (self-destruct, the required-process watcher) runs ProcessExit handlers, but a
		// termination signal can end the process before ours is reached, so handle those signals directly.
		AppDomain.CurrentDomain.ProcessExit += (_, _) => Delete(path);
		foreach (var signal in new[] { PosixSignal.SIGTERM, PosixSignal.SIGINT, PosixSignal.SIGHUP, PosixSignal.SIGQUIT })
		{
			// The default handling (shutting down) still runs, since the context is not cancelled.
			SignalRegistrations.Add(PosixSignalRegistration.Create(signal, _ => Delete(path)));
		}
		return path;
	}

	// A registration is undone when it is garbage collected, so keep them for the life of the process.
	private static readonly List<PosixSignalRegistration> SignalRegistrations = new List<PosixSignalRegistration>();

	public static void Delete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (IOException)
		{
			// Best effort; readers skip files whose process is gone.
		}
		catch (UnauthorizedAccessException)
		{
			// Best effort; readers skip files whose process is gone.
		}
	}

	/// <summary>
	/// The locators of servers whose process is still running, newest first. Files left behind by a server that
	/// crashed are skipped (and removed when <paramref name="prune"/> is set).
	/// </summary>
	public static List<ServerLocator> ReadLive(string configTempDirectory, bool prune = false)
	{
		var live = new List<ServerLocator>();
		var folder = GetFolder(configTempDirectory);
		if (!Directory.Exists(folder))
		{
			return live;
		}

		foreach (var path in Directory.GetFiles(folder, "*.json"))
		{
			ServerLocator locator;
			try
			{
				locator = JsonConvert.DeserializeObject<ServerLocator>(File.ReadAllText(path));
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
			{
				continue;
			}

			if (locator != null && IsRunning(locator.pid))
			{
				live.Add(locator);
			}
			else if (prune)
			{
				Delete(path);
			}
		}

		return live.OrderByDescending(l => l.startedAt).ToList();
	}

	static bool IsRunning(int pid)
	{
		try
		{
			using var process = Process.GetProcessById(pid);
			return !process.HasExited;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}
}
