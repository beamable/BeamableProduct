using cli.Services.LocalStack;
using NUnit.Framework;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace tests;

/// <summary>
/// Covers the pre-launch port check. A squatted port is one of the local stack's most expensive failures
/// because it never announces itself: the service loses the bind, and what the user sees is a readiness
/// timeout, a Caddy 502, or dependents failing to fetch dbids. <c>up</c> therefore refuses to launch a step
/// whose port is already held, and names the owning pid.
/// </summary>
public class LocalStackPortGuardTests
{
	/// <summary>Binds an ephemeral loopback port and hands back its number, so the test owns a real listener.</summary>
	private static TcpListener ListenOnFreePort(out int port)
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		port = ((IPEndPoint)listener.LocalEndpoint).Port;
		return listener;
	}

	private static int FreePort()
	{
		var listener = ListenOnFreePort(out var port);
		listener.Stop();
		return port;
	}

	[Test]
	public void A_bound_port_is_reported_taken_and_a_free_one_is_not()
	{
		var listener = ListenOnFreePort(out var port);
		try
		{
			Assert.That(LocalStackPortGuard.IsPortTaken(port), Is.True, "a port we are listening on must read as taken");
		}
		finally
		{
			listener.Stop();
		}

		Assert.That(LocalStackPortGuard.IsPortTaken(FreePort()), Is.False);
	}

	/// <summary>
	/// A listener bound to one specific NON-loopback address does not stop a service binding localhost, so it must
	/// not read as a conflict. The old probe also tried 0.0.0.0, which fails against any specific-address bind and
	/// therefore hard-aborted bring-ups over LAN/vEthernet/VPN-scoped listeners that were never in the way.
	/// </summary>
	[Test]
	public void A_listener_on_another_local_address_is_not_a_conflict()
	{
		var lanAddress = Dns.GetHostAddresses(Dns.GetHostName())
			.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
		if (lanAddress == null)
		{
			Assert.Ignore("no non-loopback IPv4 address on this machine");
		}

		var port = FreePort();
		var listener = new TcpListener(lanAddress, port);
		try
		{
			listener.Start();
		}
		catch (SocketException e)
		{
			Assert.Ignore($"could not bind {lanAddress}:{port} here ({e.SocketErrorCode})");
			return;
		}

		try
		{
			Assert.That(LocalStackPortGuard.IsPortTaken(port), Is.False,
				"a bind on a specific non-loopback address must not be reported as taken");
			Assert.That(LocalStackPortGuard.DescribeConflict("portal frontend", port), Is.Null);
		}
		finally
		{
			listener.Stop();
		}
	}

	[Test]
	public void An_unset_port_is_never_checked()
	{
		// port defaults to 0 on every step the template can't name a port for (docker, beam, most scala) —
		// those must fall through untouched rather than being probed.
		Assert.That(LocalStackPortGuard.IsPortTaken(0), Is.False);
		Assert.That(LocalStackPortGuard.DescribeConflict("docker: api deps + caddy", 0), Is.Null);
	}

	[Test]
	public void The_conflict_message_names_the_port_and_how_to_clear_it()
	{
		var listener = ListenOnFreePort(out var port);
		try
		{
			var message = LocalStackPortGuard.DescribeConflict("c# campaign runtime", port);

			Assert.That(message, Is.Not.Null, "a held port must produce a conflict message");
			Assert.That(message, Does.Contain("c# campaign runtime"));
			Assert.That(message, Does.Contain(port.ToString()));
			Assert.That(message, Does.Contain("beam local stop"), "the message must say how to clear it");
		}
		finally
		{
			listener.Stop();
		}
	}

	/// <summary>
	/// A port held by a leftover from a previous stack must get the remedy that actually works.
	///
	/// The generic advice is `beam local stop`, and for this case it is worse than useless: `stop` reads
	/// .beamable/local-stack.run.json, which only a DETACHED run writes. An attached run that was Ctrl+C'd
	/// or died mid-bring-up leaves every process it had started with no run-state behind them, so `stop`
	/// answers "No running local stack recorded" while ~20 ports stay held and the next `up` fails the same
	/// way. Sending someone to a command that silently does nothing is the expensive part.
	/// </summary>
	[Test]
	public void A_leftover_stack_process_is_recognised_so_the_remedy_is_not_beam_local_stop()
	{
		// The owner strings DescribeOwner produces: "pid <n> (<processName>)".
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 33313 (java)"), Is.True,
			"the Scala services and the portal run under shared runtimes");
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 32881 (node)"), Is.True);
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 32833 (BeamableGateway)"), Is.True);
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 32852 (BeamableCampaignRuntime)"), Is.True);
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 32844 (BeamableMessageRailRuntime)"), Is.True);

		// Unrelated applications keep the generic remedy.
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 1 (ControlCenter)"), Is.False);
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 1 (Docker)"), Is.False);
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 1"), Is.False,
			"an unidentified owner must not be guessed at");
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover(null), Is.False);

		// Matched on the parenthesised process name, not anywhere in the string — a step name or path that
		// merely contains "node" must not be mistaken for one.
		Assert.That(LocalStackPortGuard.LooksLikeStackLeftover("pid 5 (my-node-tool)"), Is.False);
	}

	[Test]
	public void A_free_port_produces_no_conflict()
	{
		Assert.That(LocalStackPortGuard.DescribeConflict("c# gateway", FreePort()), Is.Null);
	}

	/// <summary>
	/// The whole point of the check is telling the user WHICH process to deal with. Owner lookup is
	/// best-effort per platform (TCP table on Windows, lsof elsewhere), so this asserts the pid when one is
	/// resolved and only that the message stays actionable when it isn't.
	/// </summary>
	[Test]
	public void The_owning_process_is_named_when_the_platform_can_resolve_it()
	{
		var listener = ListenOnFreePort(out var port);
		try
		{
			var owner = LocalStackPortGuard.DescribeOwner(port);
			if (owner == null)
			{
				Assert.That(LocalStackPortGuard.DescribeConflict("c# gateway", port),
					Does.Contain("unidentified process"));
				return;
			}

			// This test process is the listener, so that is the pid it must report.
			Assert.That(owner, Does.Contain(Process.GetCurrentProcess().Id.ToString()));
		}
		finally
		{
			listener.Stop();
		}
	}

	/// <summary>
	/// The steps whose port is knowable must declare it, or the check silently never runs for them. The Scala
	/// gateway is the interesting one: it is health-checked through Caddy on :8080 but binds :9002 itself, which
	/// is exactly why the port is a field rather than something parsed out of the readiness URL.
	/// </summary>
	[Test]
	public void The_template_declares_the_port_each_known_step_binds()
	{
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = @"C:\repos\BeamableBackend",
			portalDir = @"C:\repos\portal",
			scalaTools = new System.Collections.Generic.List<LocalStackTemplate.ScalaToolInfo>
			{
				new() { name = "gateway", mainClass = "com.beamable.gateway.App" },
			},
		});

		LocalStackStep Step(string name) => config.steps.First(s => s.name == name);

		Assert.That(Step("c# gateway").port, Is.EqualTo(5000));
		Assert.That(Step("c# message rail runtime").port, Is.EqualTo(5030));
		// 5045: 5040 is held exclusively by Windows' Connected Devices Platform service, and 5050/5031 are
		// already claimed by BeamableAPI's own launchSettings (BeamableScheduler.Loader/.Dispatcher use 5050).
		Assert.That(Step("c# campaign runtime").port, Is.EqualTo(5045));
		Assert.That(Step("portal frontend").port, Is.EqualTo(4950));
		Assert.That(Step("scala: gateway").port, Is.EqualTo(9002),
			"the scala gateway binds 9002 even though its readiness gate is the Caddy host");

		// Every declared port must be distinct, or the check would fire on our own services.
		var ports = config.steps.Where(s => s.port > 0).Select(s => s.port).ToArray();
		Assert.That(ports, Is.Unique);

		// Steps we cannot attribute a port to stay at 0 (docker publishes its own; beam steps bind nothing).
		Assert.That(Step("docker: api deps + caddy").port, Is.EqualTo(0));
		Assert.That(Step("build: c# gateway").port, Is.EqualTo(0));
	}

	/// <summary>
	/// The anti-false-positive test for reservation detection. A port a real process holds must NOT read as
	/// reserved: on Windows an exclusive holder yields the same <c>AccessDenied</c> as a reservation, and if that
	/// were enough to report one, every re-run of `up` against an already-up stack would abort with advice to
	/// rewrite the machine's port configuration.
	/// </summary>
	[Test]
	public void A_port_held_by_a_process_is_not_reported_as_a_windows_reservation()
	{
		var listener = ListenOnFreePort(out var port);
		try
		{
			Assert.That(LocalStackPortGuard.IsPortReserved(port), Is.False,
				"a port with a real listener is occupancy, not a Windows reservation");
			Assert.That(LocalStackPortGuard.DescribeReservation("docker: api deps + caddy", port), Is.Null);

			// It must still be reported as a conflict, through the occupancy path.
			Assert.That(LocalStackPortGuard.DescribeConflict("docker: api deps + caddy", port),
				Does.Contain("beam local stop"));
		}
		finally
		{
			listener.Stop();
		}
	}

	/// <summary>
	/// A port inside one of WinNAT/Hyper-V's boot-time exclusion blocks cannot be bound by anything, which is what
	/// broke `docker: api deps + caddy` on ActiveMQ's 61616. Administered rows (marked <c>*</c>) are excluded from
	/// the fixture because those stay bindable — that distinction is the whole basis of the check.
	/// </summary>
	[Test]
	public void A_port_in_a_non_administered_exclusion_range_reads_as_reserved()
	{
		if (!System.OperatingSystem.IsWindows())
		{
			Assert.Ignore("port exclusion ranges are a Windows concept");
		}

		var reservedPort = FirstNonAdministeredExcludedPort();
		if (reservedPort == 0)
		{
			Assert.Ignore("no non-administered TCP exclusion range above 1024 on this machine right now");
		}

		Assert.That(LocalStackPortGuard.IsPortReserved(reservedPort), Is.True,
			$"port {reservedPort} is inside a non-administered exclusion range, so nothing can bind it");

		var message = LocalStackPortGuard.DescribeReservation("docker: api deps + caddy", reservedPort);
		Assert.That(message, Is.Not.Null);
		Assert.That(message, Does.Contain(reservedPort.ToString()));
		Assert.That(message, Does.Contain("excludedportrange"), "the message must carry the actual remedy");
		Assert.That(message, Does.Not.Contain("beam local stop"), "there is no process to stop for a reservation");
	}

	/// <summary>
	/// Reads <c>netsh</c>'s exclusion table and returns a port from the first non-administered range above the
	/// privileged ceiling, or 0 when there is none (a machine whose ephemeral range has been moved out of the way
	/// legitimately has none).
	/// </summary>
	private static int FirstNonAdministeredExcludedPort()
	{
		try
		{
			var psi = new ProcessStartInfo("netsh")
			{
				RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
			};
			foreach (var arg in new[] { "int", "ipv4", "show", "excludedportrange", "protocol=tcp" })
			{
				psi.ArgumentList.Add(arg);
			}

			using var proc = Process.Start(psi);
			if (proc == null || !proc.WaitForExit(5000))
			{
				return 0;
			}

			foreach (var line in proc.StandardOutput.ReadToEnd().Split('\n'))
			{
				if (line.Contains('*'))
				{
					continue;
				}

				var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
				if (parts.Length == 2 && int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var end)
				    && start >= 1024 && end >= start)
				{
					return start;
				}
			}
		}
		catch
		{
			// fall through — the test ignores itself when it cannot build a fixture
		}

		return 0;
	}

	/// <summary>
	/// End-to-end wiring of the pre-flight: a docker step whose compose file publishes a genuinely reserved port
	/// must produce the abort message, and the same step must stay silent when its ports are fine. This is the case
	/// that actually broke — `docker: api deps + caddy` publishing ActiveMQ's 61616 into a WinNAT block.
	/// </summary>
	[Test]
	public void A_docker_step_publishing_a_reserved_port_aborts_the_bring_up()
	{
		if (!System.OperatingSystem.IsWindows())
		{
			Assert.Ignore("port exclusion ranges are a Windows concept");
		}

		var reservedPort = FirstNonAdministeredExcludedPort();
		if (reservedPort == 0)
		{
			Assert.Ignore("no non-administered TCP exclusion range above 1024 on this machine right now");
		}

		var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beam-portguard-" + System.Guid.NewGuid().ToString("N"));
		System.IO.Directory.CreateDirectory(dir);
		try
		{
			System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "docker-compose.yml"),
				"services:\n  broker:\n    image: activemq\n    ports:\n      - \"" + reservedPort + ":61616\"\n");

			var step = new LocalStackStep { name = "docker: api deps + caddy", command = "docker", workingDirectory = dir };
			var message = LocalStackPortGuard.DescribeReservedDockerPorts(new[] { step });

			Assert.That(message, Is.Not.Null, "a reserved published port must abort the bring-up");
			Assert.That(message, Does.Contain(reservedPort.ToString()));
			Assert.That(message, Does.Contain("docker: api deps + caddy"), "the failing step must be named");
			Assert.That(message, Does.Contain("excludedportrange"));

			// The same step with a free published port must not trip it — this is what keeps an idempotent re-run
			// of `up` (containers already holding their ports) working.
			System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "docker-compose.yml"),
				"services:\n  broker:\n    image: activemq\n    ports:\n      - \"" + FreePort() + ":61616\"\n");
			Assert.That(LocalStackPortGuard.DescribeReservedDockerPorts(new[] { step }), Is.Null);
		}
		finally
		{
			try { System.IO.Directory.Delete(dir, true); } catch { /* best-effort */ }
		}
	}

	/// <summary>
	/// Non-docker steps must never be probed: the check exists for compose-published ports, and reading a compose
	/// file that happens to sit in some other step's working directory would attribute ports to the wrong step.
	/// </summary>
	[Test]
	public void Only_docker_steps_are_checked_and_no_steps_is_silent()
	{
		Assert.That(LocalStackPortGuard.DescribeReservedDockerPorts(new LocalStackStep[0]), Is.Null);
		Assert.That(LocalStackPortGuard.DescribeReservedDockerPorts(null), Is.Null);
		Assert.That(LocalStackPortGuard.DescribeReservedDockerPorts(new[]
		{
			new LocalStackStep { name = "scala: gateway", command = "pwsh", workingDirectory = System.IO.Path.GetTempPath() }
		}), Is.Null);
	}

	/// <summary>
	/// The preflight tests the host ports the compose files publish, so the host side has to be read correctly out
	/// of every form compose accepts — a wrong answer here either misses the reservation or probes a port the stack
	/// never binds. A bare container port maps to a host port docker picks, so there is nothing fixed to check.
	/// </summary>
	[Test]
	public void Compose_port_entries_yield_the_host_port_only_when_one_is_fixed()
	{
		Assert.That(DockerComposeModel.TryParseHostPort("61616:61616"), Is.EqualTo(61616));
		Assert.That(DockerComposeModel.TryParseHostPort("127.0.0.1:61616:61616"), Is.EqualTo(61616));
		Assert.That(DockerComposeModel.TryParseHostPort("5675:5672"), Is.EqualTo(5675), "host and container differ");
		Assert.That(DockerComposeModel.TryParseHostPort("8080:8080/tcp"), Is.EqualTo(8080));
		Assert.That(DockerComposeModel.TryParseHostPort(" 27015:27017 "), Is.EqualTo(27015));

		Assert.That(DockerComposeModel.TryParseHostPort("61616"), Is.EqualTo(0), "bare container port gets a random host port");
		Assert.That(DockerComposeModel.TryParseHostPort("9000-9010:9000-9010"), Is.EqualTo(0), "ranges are skipped");
		Assert.That(DockerComposeModel.TryParseHostPort(""), Is.EqualTo(0));
		Assert.That(DockerComposeModel.TryParseHostPort(null), Is.EqualTo(0));
	}

	/// <summary>
	/// The whole file has to be read, not just one service: docker fails <c>compose up</c> on the first port it
	/// cannot bind, wherever it appears. This mirrors the real shape of <c>BeamableAPI/docker-compose.yml</c> —
	/// several services, a host≠container mapping, and the 61616 entry that started this.
	/// </summary>
	[Test]
	public void Published_host_ports_are_collected_across_every_service_in_the_file()
	{
		var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beam-compose-" + System.Guid.NewGuid().ToString("N"));
		System.IO.Directory.CreateDirectory(dir);
		try
		{
			var path = System.IO.Path.Combine(dir, "docker-compose.yml");
			System.IO.File.WriteAllText(path,
				"services:\n" +
				"  consul:\n    image: consul:1.15\n    ports:\n      - \"8500:8500\"\n" +
				"  mongo_master:\n    container_name: mongo_master\n    ports:\n      - \"27015:27017\"\n" +
				"  broker:\n    build: docker/activemq\n    ports:\n      - \"8161:8161\"\n      - \"5675:5672\"\n      - \"61616:61616\"\n" +
				"  mongo_master_setup:\n    depends_on:\n      - mongo_master\n");

			var compose = DockerComposeModel.TryLoadFile(path);
			Assert.That(compose, Is.Not.Null);
			Assert.That(compose.PublishedHostPorts().OrderBy(p => p).ToArray(),
				Is.EqualTo(new[] { 5675, 8161, 8500, 27015, 61616 }));
		}
		finally
		{
			try { System.IO.Directory.Delete(dir, true); } catch { /* best-effort */ }
		}
	}

	/// <summary>
	/// A URL written without a port must yield 0, not the scheme default. Uri.Port reports 80/443 in that case, and
	/// a guard probing :80 would abort the bring-up on an unprivileged-bind refusal that means nothing.
	/// </summary>
	[Test]
	public void A_portless_url_declares_no_port_rather_than_80()
	{
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = @"C:\repos\BeamableBackend",
			portalDir = @"C:\repos\portal",
			portalUrl = "http://localhost",
			scalaTools = new System.Collections.Generic.List<LocalStackTemplate.ScalaToolInfo>(),
		});

		var portal = config.steps.First(s => s.name == "portal frontend");
		Assert.That(portal.port, Is.EqualTo(0),
			"http://localhost has no explicit port, so no conflict check should run for it");
	}
}
