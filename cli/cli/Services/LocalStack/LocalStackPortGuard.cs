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

		var probe = Probe(IPAddress.Loopback, port);

		// On Windows a denied bind above the privileged range is a reservation/exclusion (winnat, Hyper-V) or a
		// holder that took the port exclusively, either of which makes it just as unusable as an occupied one. On
		// unix the same error usually means only "you are not root" for a low port, which is NOT occupancy —
		// hence the platform and range guard.
		return probe == BindProbe.InUse
		       || (probe == BindProbe.Denied && OperatingSystem.IsWindows() && port >= PrivilegedPortCeiling);
	}

	/// <summary>
	/// Why a bind probe failed. <see cref="BindProbe.InUse"/> and <see cref="BindProbe.Denied"/> both mean "not
	/// usable", but only the latter can be a Windows port <em>reservation</em>, which needs a completely different
	/// remedy (there is no process to stop) — so the two are kept apart rather than collapsed into a bool.
	/// </summary>
	private enum BindProbe
	{
		Free,
		InUse,
		Denied
	}

	private static BindProbe Probe(IPAddress address, int port)
	{
		try
		{
			using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			// Do NOT set ReuseAddress: the point is to fail exactly where a real service would fail.
			socket.Bind(new IPEndPoint(address, port));
			return BindProbe.Free;
		}
		catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
		{
			return BindProbe.InUse;
		}
		catch (SocketException e) when (e.SocketErrorCode == SocketError.AccessDenied)
		{
			return BindProbe.Denied;
		}
		catch
		{
			return BindProbe.Free; // can't probe — never block a bring-up on an inconclusive answer
		}
	}

	/// <summary>Ports below this need elevation on unix, so a denied bind there says nothing about occupancy.</summary>
	private const int PrivilegedPortCeiling = 1024;

	/// <summary>
	/// True when Windows itself has reserved <paramref name="port"/> — WinNAT/Hyper-V carves blocks out of the
	/// ephemeral range at every boot, and a port inside one cannot be bound by anything, even though no process
	/// holds it. That is a different failure from occupancy and needs a different fix, so it is detected separately:
	/// the classic symptom is <c>docker compose up</c> dying with "ports are not available … bind: An attempt was
	/// made to access a socket in a way forbidden by its access permissions" on a port nothing is listening on.
	///
	/// Two signals must agree, because neither alone is sufficient:
	/// <list type="bullet">
	/// <item>the bind is DENIED (not merely in use), and</item>
	/// <item>the port sits in a NON-administered range from <c>netsh … show excludedportrange</c>.</item>
	/// </list>
	/// The range check is what makes this safe. A denied bind on its own is ambiguous — a holder that took the port
	/// exclusively also yields <c>AccessDenied</c> (this is exactly why the Scala gateway's :9002 reads as denied
	/// while it is serving), and the owning-pid lookup cannot disambiguate it because that table is IPv4-only and
	/// the gateway binds <c>::</c>. Conversely an exclusion we added ourselves (administered, marked <c>*</c>) is
	/// still bindable, so it must never be reported: those rows are skipped when the table is read.
	/// Windows-only; false everywhere else.
	/// </summary>
	public static bool IsPortReserved(int port)
	{
		if (port < PrivilegedPortCeiling || port > 65535 || !OperatingSystem.IsWindows())
		{
			return false;
		}

		if (Probe(IPAddress.Loopback, port) != BindProbe.Denied)
		{
			return false;
		}

		return ReservedRanges().Any(r => port >= r.start && port <= r.end);
	}

	/// <summary>
	/// The message for a port Windows has reserved, or null when it hasn't. Distinct from
	/// <see cref="DescribeConflict"/>'s occupancy text because none of that advice applies: there is no pid to
	/// kill and <c>beam local stop</c> changes nothing. The remedy is to claim the port as an <em>administered</em>
	/// exclusion — which stays bindable, unlike WinNAT's — and to move the ephemeral range below the ports the
	/// stack publishes so the collision cannot recur at the next boot.
	/// </summary>
	public static string DescribeReservation(string stepName, int port)
	{
		if (!IsPortReserved(port))
		{
			return null;
		}

		return $"Step '{stepName}' cannot start: TCP port {port} is reserved by Windows (WinNAT/Hyper-V), so nothing "
		       + "can bind it. No process is holding it, so there is nothing to stop — these blocks are carved out of "
		       + "the ephemeral port range at every boot. Claim the port back, in an Administrator shell:"
		       + Environment.NewLine + "    net stop winnat"
		       + Environment.NewLine + $"    netsh int ipv4 add excludedportrange protocol=tcp startport={port} numberofports=1 store=persistent"
		       + Environment.NewLine + "    net start winnat"
		       + Environment.NewLine
		       + "then restart Docker Desktop and re-run. An exclusion you add yourself remains bindable; it only "
		       + "stops WinNAT from taking the port. To stop this recurring on other high ports, move the ephemeral "
		       + "range below them: `netsh int ipv4 set dynamicport tcp start=49152 num=11000`.";
	}

	/// <summary>
	/// Compose file names docker itself looks for, in its own precedence order — checked in a docker step's working
	/// directory to find the file whose published ports have to be tested.
	/// </summary>
	private static readonly string[] ComposeFileNames =
		{ "compose.yaml", "compose.yml", "docker-compose.yaml", "docker-compose.yml" };

	/// <summary>
	/// The message <c>up</c> must abort with when Windows has reserved a host port one of the docker steps'
	/// compose files publishes, or null when none is. Docker fails the whole <c>compose up</c> if it cannot bind a
	/// single published port, the step exits 1 mid-bring-up and the stack is rolled back — with an error that names
	/// neither cause nor owner, because there is no owner. Saying so before anything launches is far cheaper.
	///
	/// Ports are read from each step's own compose file rather than hardcoded, so this covers the API deps, the
	/// Scala <c>redis</c> step and the web registry alike. Reports ONLY reservations, never occupancy: a re-run
	/// against an already-up stack finds its own containers holding these ports, and treating that as a conflict
	/// would break every idempotent <c>up</c>.
	/// </summary>
	public static string DescribeReservedDockerPorts(IEnumerable<LocalStackStep> steps)
	{
		if (steps == null || !OperatingSystem.IsWindows())
		{
			return null;
		}

		var problems = new List<string>();
		foreach (var step in steps.Where(s => string.Equals(s?.command, "docker", StringComparison.OrdinalIgnoreCase)))
		{
			if (string.IsNullOrWhiteSpace(step.workingDirectory))
			{
				continue;
			}

			var compose = ComposeFileNames
				.Select(name => DockerComposeModel.TryLoadFile(Path.Combine(step.workingDirectory, name)))
				.FirstOrDefault(model => model != null);
			if (compose == null)
			{
				continue; // no readable compose file — never block a bring-up on an inconclusive answer
			}

			foreach (var port in compose.PublishedHostPorts().Distinct())
			{
				var reservation = DescribeReservation(step.name, port);
				if (reservation != null)
				{
					problems.Add(reservation);
				}
			}
		}

		return problems.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, problems);
	}

	/// <summary>
	/// Non-administered exclusion ranges, read once per process (they only change on boot or an explicit netsh
	/// call, neither of which happens mid-bring-up). Empty when the table can't be read — an inconclusive answer
	/// must never block a bring-up.
	/// </summary>
	private static (int start, int end)[] _reservedRanges;

	private static (int start, int end)[] ReservedRanges() => _reservedRanges ??= ReadReservedRanges();

	/// <summary>
	/// Parses <c>netsh int ipv4 show excludedportrange protocol=tcp</c>. Rows are two integers; the ones carrying a
	/// <c>*</c> are administered (ours — still bindable) and are skipped, as are the header, the rule line and the
	/// trailing legend. There is no managed API for this table, hence shelling out. Never throws.
	/// </summary>
	private static (int start, int end)[] ReadReservedRanges()
	{
		var empty = Array.Empty<(int, int)>();
		try
		{
			var psi = new ProcessStartInfo("netsh")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			psi.ArgumentList.Add("int");
			psi.ArgumentList.Add("ipv4");
			psi.ArgumentList.Add("show");
			psi.ArgumentList.Add("excludedportrange");
			psi.ArgumentList.Add("protocol=tcp");

			using var proc = Process.Start(psi);
			if (proc == null)
			{
				return empty;
			}

			// Drain both pipes before waiting, for the same deadlock reason as the lsof call below.
			var stdout = proc.StandardOutput.ReadToEndAsync();
			var stderr = proc.StandardError.ReadToEndAsync();
			if (!proc.WaitForExit(3000))
			{
				try { proc.Kill(entireProcessTree: true); } catch { /* best-effort */ }
				return empty;
			}

			Task.WaitAll(new Task[] { stdout, stderr }, 1000);
			if (!stdout.IsCompletedSuccessfully)
			{
				return empty;
			}

			var ranges = new List<(int, int)>();
			foreach (var line in stdout.Result.Split('\n'))
			{
				if (line.Contains('*'))
				{
					continue; // administered exclusion (or the legend explaining the marker)
				}

				var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 2
				    && int.TryParse(parts[0], out var start)
				    && int.TryParse(parts[1], out var end)
				    && start > 0 && end >= start)
				{
					ranges.Add((start, end));
				}
			}

			return ranges.ToArray();
		}
		catch
		{
			return empty;
		}
	}

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
		// A Windows reservation also reads as "taken", but none of the advice below applies to it, so it is
		// answered first with its own remedy.
		var reservation = DescribeReservation(stepName, port);
		if (reservation != null)
		{
			return reservation;
		}

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
