using System.Diagnostics;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
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

	/// <summary>
	/// Docker's port proxy / VM plumbing — a process holding a port that maps to one of these is a legitimate
	/// published container port, never a squatter to kill.
	/// </summary>
	private static readonly string[] DockerProcessNames =
		{ "com.docker.backend", "com.docker.build", "dockerd", "docker", "vpnkit", "wslrelay", "wslhost" };

	/// <summary>
	/// Frees a host TCP port squatted by a non-Docker process bound to the <em>specific</em> <c>127.0.0.1</c>
	/// address. Docker Desktop's port proxy binds <c>0.0.0.0</c> / <c>[::]</c> (never the specific loopback
	/// address), so a listener on exactly <c>127.0.0.1:&lt;port&gt;</c> is by definition a foreign squatter 
	/// which permanently takes TCP 27015 on Windows and shadows the local stack's <c>mongo_master</c> publish, 
	/// timing out every Mongo connection. Windows-only; a no-op (returns <c>null</c>) elsewhere. 
	///Best-effort — never throws. Returns the freed process name (for logging) or <c>null</c> if nothing was freed.
	/// </summary>
	public static string FreeLoopbackPortSquatter(int port)
	{
		if (!OperatingSystem.IsWindows())
			return null;
		return FreeLoopbackPortSquatterWindows(port);
	}

	[SupportedOSPlatform("windows")]
	private static string FreeLoopbackPortSquatterWindows(int port)
	{
		try
		{
			var pid = FindLoopbackListenerPid(port);
			if (pid <= 0)
				return null;

			string name;
			try { name = Process.GetProcessById(pid).ProcessName; }
			catch { return null; } // already gone

			// Never touch Docker's own proxy (it binds 0.0.0.0, not 127.0.0.1, so this is belt-and-suspenders).
			if (DockerProcessNames.Contains(name, StringComparer.OrdinalIgnoreCase))
				return null;

			Process.GetProcessById(pid).Kill();
			return name;
		}
		catch
		{
			return null; // a port that can't be freed just falls through to the normal (failing) bring-up
		}
	}

	// --- iphlpapi GetExtendedTcpTable: find the PID owning the 127.0.0.1:<port> LISTEN socket -----------------

	private const int AF_INET = 2;
	private const int TCP_TABLE_OWNER_PID_LISTENER = 3;
	private const uint MIB_TCP_STATE_LISTEN = 2;

	[StructLayout(LayoutKind.Sequential)]
	private struct MIB_TCPROW_OWNER_PID
	{
		public uint state;
		public uint localAddr;
		public uint localPort; // network byte order, low 16 bits
		public uint remoteAddr;
		public uint remotePort;
		public uint owningPid;
	}

	[DllImport("iphlpapi.dll", SetLastError = true)]
	private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort,
		int ipVersion, int tblClass, int reserved);

	[SupportedOSPlatform("windows")]
	private static int FindLoopbackListenerPid(int port)
	{
		// 127.0.0.1 as the driver stores it (network-order bytes read as a host uint).
		var loopbackAddr = BitConverter.ToUInt32(IPAddress.Loopback.GetAddressBytes(), 0);

		int bufLen = 0;
		GetExtendedTcpTable(IntPtr.Zero, ref bufLen, false, AF_INET, TCP_TABLE_OWNER_PID_LISTENER, 0);
		if (bufLen <= 0)
			return 0;

		var table = Marshal.AllocHGlobal(bufLen);
		try
		{
			if (GetExtendedTcpTable(table, ref bufLen, false, AF_INET, TCP_TABLE_OWNER_PID_LISTENER, 0) != 0)
				return 0;

			var numEntries = Marshal.ReadInt32(table);
			var rowPtr = IntPtr.Add(table, sizeof(int));
			var rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
			for (var i = 0; i < numEntries; i++)
			{
				var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
				rowPtr = IntPtr.Add(rowPtr, rowSize);

				if (row.state != MIB_TCP_STATE_LISTEN)
					continue;
				if (row.localAddr != loopbackAddr) // specific 127.0.0.1 bind only — excludes Docker's 0.0.0.0
					continue;

				// localPort is network byte order in the low word; swap to host order.
				var hostPort = ((int)(row.localPort & 0xFF) << 8) | (int)((row.localPort >> 8) & 0xFF);
				if (hostPort == port)
					return (int)row.owningPid;
			}
		}
		finally
		{
			Marshal.FreeHGlobal(table);
		}

		return 0;
	}
}
