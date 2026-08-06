using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace cli.Services.LocalStack;

/// <summary>
/// Answers "is this port already taken, and by whom?" for <c>beam local up</c>'s pre-launch check.
///
/// A squatted port is one of the most expensive failures in the local stack because it never says so: the
/// service loses the bind and dies (or never serves), and what the user sees is a readiness timeout, a Caddy
/// 502, or downstream services failing to fetch dbids. Detecting it before launch — and naming the owning pid
/// — turns a 10-minute investigation into one line of output.
///
/// Detection is a bind probe (portable, no external tools). Naming the owner is best-effort and
/// platform-specific: the TCP table on Windows, <c>lsof</c> on macOS/Linux. A port that is taken is reported
/// even when the owner can't be identified.
/// </summary>
public static class LocalStackPortGuard
{
	/// <summary>
	/// True when something already holds <paramref name="port"/> on loopback — the address every step in the
	/// manifest binds. Deliberately does NOT probe <c>0.0.0.0</c>: a wildcard bind also fails when the port is
	/// held on some OTHER specific address (a LAN/vEthernet/VPN-scoped listener), which would not stop a service
	/// binding <c>localhost</c>, and reporting that as a conflict blocks a bring-up that would have worked.
	/// </summary>
	public static bool IsPortTaken(int port)
	{
		if (port <= 0 || port > 65535)
		{
			return false;
		}

		return IsBound(IPAddress.Loopback, port);
	}

	private static bool IsBound(IPAddress address, int port)
	{
		try
		{
			using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			// Do NOT set ReuseAddress: the point is to fail exactly where a real service would fail.
			socket.Bind(new IPEndPoint(address, port));
			return false;
		}
		catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
		{
			return true;
		}
		catch (SocketException e) when (e.SocketErrorCode == SocketError.AccessDenied
		                               && OperatingSystem.IsWindows() && port >= PrivilegedPortCeiling)
		{
			// On Windows a denied bind above the privileged range is a reservation/exclusion (winnat, Hyper-V),
			// which makes the port just as unusable as an occupied one. On unix the same error usually means only
			// "you are not root" for a low port, which is NOT occupancy — hence the platform and range guard.
			return true;
		}
		catch
		{
			return false; // can't probe — never block a bring-up on an inconclusive answer
		}
	}

	/// <summary>Ports below this need elevation on unix, so a denied bind there says nothing about occupancy.</summary>
	private const int PrivilegedPortCeiling = 1024;

	/// <summary>
	/// Best-effort description of what holds <paramref name="port"/>: <c>"pid 1234 (BeamableGateway)"</c>, or
	/// null when it can't be determined. Never throws.
	/// </summary>
	public static string DescribeOwner(int port)
	{
		var pid = TryGetOwnerPid(port);
		if (pid <= 0)
		{
			return null;
		}

		try
		{
			return $"pid {pid} ({Process.GetProcessById(pid).ProcessName})";
		}
		catch
		{
			return $"pid {pid}";
		}
	}

	private static int TryGetOwnerPid(int port)
	{
		try
		{
			return OperatingSystem.IsWindows()
				? LocalStackProcess.FindListenerPid(port)
				: FindListenerPidUnix(port);
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>
	/// macOS/Linux: <c>lsof -nP -iTCP:&lt;port&gt; -sTCP:LISTEN -t</c> prints the owning pid(s), one per line.
	/// Returns 0 when lsof is absent or prints nothing.
	/// </summary>
	private static int FindListenerPidUnix(int port)
	{
		var psi = new ProcessStartInfo("lsof")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		psi.ArgumentList.Add("-nP");
		psi.ArgumentList.Add($"-iTCP:{port}");
		psi.ArgumentList.Add("-sTCP:LISTEN");
		psi.ArgumentList.Add("-t");

		using var proc = Process.Start(psi);
		if (proc == null)
		{
			return 0;
		}

		// Drain BOTH pipes concurrently and bound the wait. Reading stdout to EOF first would deadlock: lsof
		// routinely writes warnings to stderr ("can't stat() ... file system") for network/fuse mounts, and once
		// that unread pipe fills, lsof blocks on write while we block on read — the timeout below never runs.
		var stdout = proc.StandardOutput.ReadToEndAsync();
		var stderr = proc.StandardError.ReadToEndAsync();
		if (!proc.WaitForExit(3000))
		{
			try { proc.Kill(entireProcessTree: true); } catch { /* best-effort */ }
			return 0;
		}

		Task.WaitAll(new Task[] { stdout, stderr }, 1000);
		if (!stdout.IsCompletedSuccessfully)
		{
			return 0;
		}

		foreach (var line in stdout.Result.Split('\n'))
		{
			if (int.TryParse(line.Trim(), out var pid) && pid > 0)
			{
				return pid;
			}
		}

		return 0;
	}

	/// <summary>
	/// macOS processes behind the classic <c>:5000</c> trap: AirPlay Receiver both takes the port the C# gateway
	/// wants and answers its <c>/health</c> with a 403, so it used to present as a readiness gate that could
	/// never pass. Naming the fix is far more useful than naming the pid.
	/// </summary>
	private static readonly string[] AirPlayProcessNames = { "ControlCenter", "AirPlayXPCHelper", "sharingd" };

	/// <summary>
	/// The message <c>up</c> fails with when a step's port is taken by something else. Returns null when the
	/// port is free (or unknown), so the caller reads as a plain guard.
	/// </summary>
	public static string DescribeConflict(string stepName, int port)
	{
		if (!IsPortTaken(port))
		{
			return null;
		}

		// No logging here on purpose: this is a pure check whose result the caller reports (and a logger is not
		// guaranteed to be configured wherever it runs).
		var owner = DescribeOwner(port);

		var remedy = owner != null && AirPlayProcessNames.Any(n => owner.Contains(n, StringComparison.OrdinalIgnoreCase))
			? "That is macOS AirPlay Receiver — turn it off in System Settings → General → AirDrop & Handoff → "
			  + "\"AirPlay Receiver\" (or change the port in the manifest), then re-run"
			: "Stop it first — `beam local stop` if it is a leftover from a previous stack, otherwise kill that "
			  + "process (or change the port in the manifest)";

		owner ??= OperatingSystem.IsWindows()
			? $"an unidentified process (run `netstat -ano | findstr :{port}` to find it)"
			: $"an unidentified process (run `lsof -nP -iTCP:{port}` to find it)";

		return $"Step '{stepName}' cannot start: port {port} is already in use by {owner}. {remedy}. "
		       + "The bring-up stopped here, so this step never launched.";
	}
}
