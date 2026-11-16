using cli.Services.LocalStack;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace tests;

/// <summary>A step whose port is taken moves to the next free one, and whatever reaches it follows.</summary>
public class LocalStackPortsTests
{
	private static List<LocalStackStep> Steps() => new List<LocalStackStep>
	{
		new LocalStackStep { name = LocalStackTemplate.ApiDepsStepName },
		new LocalStackStep { name = "c# gateway", port = 5000, readyWhenHttp200 = "http://localhost:5000/health" },
		new LocalStackStep
		{
			name = "c# message rail runtime", port = 5030, readyWhenHttp200 = "http://localhost:5030/health",
			environment = new Dictionary<string, string> { ["ASPNETCORE_URLS"] = "http://localhost:5030" }
		},
		new LocalStackStep { name = LocalStackPorts.ScalaGatewayStep, port = 9002, readyWhenHttp200 = "http://localhost:9002/metadata" },
		new LocalStackStep { name = LocalStackPorts.AnalyticsGatewayStep, port = 9003 },
		new LocalStackStep { name = "scala: auth" },
		new LocalStackStep { name = LocalStackTemplate.PortalFrontendStepName, port = 4950, arguments = "run dev", readyWhenHttpOk = "http://localhost:4950" },
	};

	[Test]
	public void Choose_skips_taken_ports_and_those_other_steps_declare()
	{
		var steps = Steps();
		var taken = new HashSet<int> { 9002, 9004 };
		var moves = LocalStackPorts.Choose(steps, steps.Where(s => s.port is 9002 or 5030), taken.Contains);
		Assert.That(moves[LocalStackPorts.ScalaGatewayStep], Is.EqualTo(9005), "9003 is the analytics gateway's, 9004 is taken");
		Assert.That(moves["c# message rail runtime"], Is.EqualTo(5031));

		var none = LocalStackPorts.Choose(steps, steps.Where(s => s.port == 4950), _ => true);
		Assert.That(none, Is.Empty, "nothing free nearby: left for the conflict check to report");
	}

	[Test]
	public void Apply_repoints_the_step_and_what_reaches_it()
	{
		var steps = Steps();
		var config = new LocalStackConfig { portalUrl = "http://localhost:4950" };
		LocalStackPorts.Apply(steps, config, new Dictionary<string, int>
		{
			["c# gateway"] = 5001, ["c# message rail runtime"] = 5031, [LocalStackPorts.ScalaGatewayStep] = 9005,
			[LocalStackTemplate.PortalFrontendStepName] = 4951
		});
		LocalStackStep Step(string name) => steps.Single(s => s.name == name);

		var gateway = Step("c# gateway");
		Assert.That((gateway.port, gateway.readyWhenHttp200, gateway.environment["ASPNETCORE_URLS"], gateway.environment["Gateway__BaseUrl"]),
			Is.EqualTo((5001, "http://localhost:5001/health", "http://localhost:5001", "http://localhost:5001")));
		Assert.That(gateway.environment["Legacy__Uri"], Is.EqualTo("http://localhost:9005"));

		var rail = Step("c# message rail runtime");
		Assert.That((rail.port, rail.environment["ASPNETCORE_URLS"], rail.readyWhenHttp200), Is.EqualTo((5031, "http://localhost:5031", "http://localhost:5031/health")));

		var scalaGateway = Step(LocalStackPorts.ScalaGatewayStep);
		Assert.That((scalaGateway.port, scalaGateway.readyWhenHttp200), Is.EqualTo((9005, "http://localhost:9005/metadata")));
		Assert.That(scalaGateway.environment["JAVA_TOOL_OPTIONS"], Is.EqualTo("-DbeamableApi.env.local.uri=http://localhost:5001 -Dservice.port=9005"));
		Assert.That(Step("scala: auth").environment["JAVA_TOOL_OPTIONS"], Is.EqualTo("-DbeamableApi.env.local.uri=http://localhost:5001"));
		Assert.That(Step(LocalStackPorts.AnalyticsGatewayStep).port, Is.EqualTo(9003), "not moved");

		var docker = Step(LocalStackTemplate.ApiDepsStepName).environment;
		Assert.That((docker[LocalStackPorts.CaddyGatewayPort], docker[LocalStackPorts.CaddyScalaGatewayPort]), Is.EqualTo(("5001", "9005")));
		Assert.That(docker.ContainsKey(LocalStackPorts.CaddyAnalyticsGatewayPort), Is.False);

		var portal = Step(LocalStackTemplate.PortalFrontendStepName);
		Assert.That((portal.arguments, portal.readyWhenHttpOk, config.portalUrl), Is.EqualTo(("run dev -- --port 4951", "http://localhost:4951", "http://localhost:4951")));
	}

	[Test]
	public void Apply_without_moves_changes_nothing()
	{
		var steps = Steps();
		var config = new LocalStackConfig { portalUrl = "http://localhost:4950" };
		LocalStackPorts.Apply(steps, config, new Dictionary<string, int> { ["c# gateway"] = 5000 });
		Assert.That(steps.All(s => s.environment == null || !s.environment.ContainsKey("JAVA_TOOL_OPTIONS")));
		Assert.That(config.portalUrl, Is.EqualTo("http://localhost:4950"));
	}
}
