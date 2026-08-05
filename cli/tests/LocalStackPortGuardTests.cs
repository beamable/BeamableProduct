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
