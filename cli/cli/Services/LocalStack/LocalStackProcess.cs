using System.Management;
using System.Runtime.Versioning;

namespace cli.Services.LocalStack;

/// <summary>
/// Windows-aware process introspection for the local-stack lifecycle. On Windows a launched service runs
/// under a wrapper chain the CLI does not fully detach (<c>cmd.exe → powershell.exe → java.exe</c> for the
/// Scala services, <c>cmd.exe → npm.cmd → node.exe</c> for the portal, <c>cmd.exe → dotnet.exe</c> for beam
/// microservices, <c>cmd.exe → BeamableGateway.exe</c> for the C# gateway). Those wrappers die when
/// <c>beam local up</c> returns, orphaning the real runtime. These helpers let <c>up</c> record the real
/// service process (the one that outlives the wrappers) and let <c>stop</c> find an orphan by its command
/// line even after the recorded pid is gone. On macOS/Linux the launcher <c>exec</c>s the service so the
/// tracked pid already IS the service — the methods here are no-ops there.
/// </summary>
public static class LocalStackProcess
{
	/// <summary>
	/// The runtime images the local stack launches. Used by <c>stop</c> to scope command-line matching so
	/// only stack runtimes are considered — never arbitrary processes. NOTE: <c>dotnet.exe</c> and
	/// <c>node.exe</c> are shared with unrelated tools (Rider, MSBuild, MCP), so matching MUST additionally
	/// require a stack-specific command-line token; image name alone is not sufficient for those.
	/// </summary>
	public static readonly string[] ServiceImages =
		{ "java.exe", "javaw.exe", "node.exe", "dotnet.exe", "BeamableGateway.exe" };

	/// <summary>Wrapper/host-console images that are never the real service when walking a process tree.</summary>
	private static readonly string[] WrapperImages =
		{ "cmd.exe", "conhost.exe", "powershell.exe", "pwsh.exe" };

	/// <summary>
	/// Resolves the real service process launched under <paramref name="rootPid"/>. On Windows, walks the
	/// process tree and returns the <em>topmost</em> descendant that is not a wrapper/console-host — the
	/// process that both roots the step's real subtree (so a tree-kill of it takes everything down) and
	/// outlives the <c>cmd</c>/<c>powershell</c> wrappers. Falls back to <paramref name="rootPid"/> when no
	/// such descendant is found. On non-Windows (or on any error) returns <paramref name="rootPid"/>.
	/// </summary>
	public static int ResolveServiceRootPid(int rootPid)
	{
		if (rootPid <= 0 || !OperatingSystem.IsWindows())
			return rootPid;
		return ResolveServiceRootPidWindows(rootPid);
	}

	/// <summary>
	/// Finds running processes whose command line contains any of <paramref name="tokens"/>, optionally
	/// restricted to the given <paramref name="imageNames"/> (e.g. <c>java.exe</c>). Windows-only; returns
	/// empty on other platforms (their <c>stop</c> already kills the service directly by pid).
	/// </summary>
	public static IReadOnlyList<int> FindByCommandLine(IEnumerable<string> tokens, string[] imageNames)
	{
		var tokenList = tokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
		if (tokenList.Count == 0 || !OperatingSystem.IsWindows())
			return Array.Empty<int>();
		return FindByCommandLineWindows(tokenList, imageNames);
	}

	[SupportedOSPlatform("windows")]
	private static int ResolveServiceRootPidWindows(int rootPid)
	{
		try
		{
			// Snapshot the whole process table once: parentPid -> [(pid, imageName)].
			var childrenByParent = new Dictionary<int, List<(int pid, string name)>>();
			using (var searcher = new ManagementObjectSearcher(
				       "SELECT ProcessId, ParentProcessId, Name FROM Win32_Process"))
			using (var results = searcher.Get())
			{
				foreach (var o in results)
				{
					using var mo = (ManagementObject)o;
					var pid = ToInt(mo["ProcessId"]);
					var parent = ToInt(mo["ParentProcessId"]);
					var name = mo["Name"] as string ?? string.Empty;
					if (!childrenByParent.TryGetValue(parent, out var list))
						childrenByParent[parent] = list = new List<(int, string)>();
					list.Add((pid, name));
				}
			}

			// Breadth-first so we return the SHALLOWEST real (non-wrapper) descendant: tree-killing it takes
			// the whole real subtree down, and it is what survives once the wrappers above it exit.
			var seen = new HashSet<int> { rootPid };
			var queue = new Queue<int>();
			queue.Enqueue(rootPid);
			while (queue.Count > 0)
			{
				var pid = queue.Dequeue();
				if (!childrenByParent.TryGetValue(pid, out var kids))
					continue;
				foreach (var (kpid, kname) in kids)
				{
					if (!seen.Add(kpid))
						continue; // guard against pid-reuse cycles in the snapshot
					if (!WrapperImages.Contains(kname, StringComparer.OrdinalIgnoreCase))
						return kpid;
					queue.Enqueue(kpid);
				}
			}

			return rootPid;
		}
		catch
		{
			return rootPid;
		}
	}

	[SupportedOSPlatform("windows")]
	private static IReadOnlyList<int> FindByCommandLineWindows(List<string> tokens, string[] imageNames)
	{
		var found = new List<int>();
		try
		{
			var where = string.Empty;
			if (imageNames is { Length: > 0 })
			{
				var clauses = imageNames.Select(n => $"Name = '{n.Replace("'", "''")}'");
				where = " WHERE " + string.Join(" OR ", clauses);
			}

			using var searcher = new ManagementObjectSearcher(
				$"SELECT ProcessId, CommandLine FROM Win32_Process{where}");
			using var results = searcher.Get();
			foreach (var o in results)
			{
				using var mo = (ManagementObject)o;
				var cmd = mo["CommandLine"] as string;
				if (string.IsNullOrEmpty(cmd))
					continue;
				if (tokens.Any(t => cmd.Contains(t, StringComparison.OrdinalIgnoreCase)))
					found.Add(ToInt(mo["ProcessId"]));
			}
		}
		catch
		{
			/* best-effort — a WMI hiccup shouldn't crash `stop` */
		}

		return found;
	}

	private static int ToInt(object value)
	{
		try { return value == null ? 0 : Convert.ToInt32(value); }
		catch { return 0; }
	}
}
