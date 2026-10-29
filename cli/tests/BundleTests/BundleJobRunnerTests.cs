using cli.Commands.LocalStack;
using cli.DeploymentCommands;
using cli.Services.Bundles;
using cli.Services.LocalWeb;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace tests.BundleTests;

/// <summary>
/// Unit tests for <see cref="BundlesApi.BuildArgs"/> — the whitelist that turns a page request into the
/// arguments of an existing CLI command. Everything the page sends is validated before it reaches the parser.
/// </summary>
public class BundleJobRunnerTests
{
	static string[] Build(string op, string bundle = "", string tag = "", string checksum = "", string comment = "",
		string acl = "", string scope = "", bool flag = false) =>
		BundlesApi.BuildArgs(op, bundle, tag, checksum, comment, acl, scope, flag);

	[Test]
	public void Publish_PassesCommentAsASingleArgument()
	{
		var args = Build("publish", "@beam-project/zone-core", tag: "stable", comment: "fix \"quotes\" --and flags",
			acl: "org", flag: true);

		Assert.That(args, Is.EqualTo(new[]
		{
			"bundles", "publish", "@beam-project/zone-core", "--from-latest-plan", "--tag", "stable", "--acl", "org",
			"--comment=fix \"quotes\" --and flags"
		}));
	}

	[Test]
	public void CommentOption_KeepsALeadingDashAsText()
	{
		var commentOption = new Option<string>(new[] { "--comment", "-c" });
		var quietOption = new Option<bool>("--quiet");
		var root = new RootCommand { commentOption, quietOption };
		var comment = Build("deploy-plan", scope: "realm", comment: "--quiet please").Last();

		var result = root.Parse(new[] { comment });

		Assert.That(result.Errors, Is.Empty);
		Assert.That(result.GetValueForOption(commentOption), Is.EqualTo("--quiet please"));
		Assert.That(result.GetValueForOption(quietOption), Is.False);
	}

	[Test]
	public void Install_PrefersChecksumOverTag()
	{
		var args = Build("install", "vip", tag: "stable", checksum: "sha256:abcdef0123456789");

		Assert.That(args, Is.EqualTo(new[] { "bundles", "install", "vip", "--checksum", "sha256:abcdef0123456789" }));
	}

	[Test]
	public void Tag_AcceptsAChecksumReference()
	{
		var args = Build("tag", "vip@sha256:abcdef0123456789", tag: "stable");

		Assert.That(args, Is.EqualTo(new[] { "bundles", "tag", "vip@sha256:abcdef0123456789", "stable" }));
	}

	[Test]
	public void DeployRelease_UsesTheLatestPlan()
	{
		Assert.That(Build("deploy-release", scope: "zone", comment: "ship it"),
			Is.EqualTo(new[] { "deploy", "release", "--from-latest-plan", "--scope", "zone", "--comment=ship it" }));
	}

	[TestCase("plan", "--help")]
	[TestCase("plan", "vip; rm -rf /")]
	[TestCase("plan", "")]
	[TestCase("yank", "vip@sha256 x")]
	public void InvalidBundle_IsRejected(string op, string bundle)
	{
		Assert.That(() => Build(op, bundle), Throws.TypeOf<LocalWebRequestException>());
	}

	[TestCase("acl", "vip", "realm", "")]
	[TestCase("deploy-plan", "", "", "galaxy")]
	[TestCase("unknown", "vip", "", "")]
	public void UnknownValues_AreRejected(string op, string bundle, string acl, string scope)
	{
		Assert.That(() => Build(op, bundle, acl: acl, scope: scope), Throws.TypeOf<LocalWebRequestException>());
	}

	[Test]
	public void InvalidTag_IsRejected()
	{
		Assert.That(() => Build("tag", "vip@sha256:abcdef0123456789", tag: "--quiet x"), Throws.TypeOf<LocalWebRequestException>());
	}

	[Test]
	public void Reporter_KeepsEachProgressStepInStartOrder()
	{
		var job = new CliJob();
		var reporter = new CliJobReporter(job);

		reporter.Report("progress", new PlanReleaseProgress { name = "fetching latest", ratio = 0.5f, isKnownLength = true });
		reporter.Report("progress", new PlanReleaseProgress { name = "build vip", ratio = 0.1f, isKnownLength = true, serviceName = "vip" });
		reporter.Report("progress", new PlanReleaseProgress { name = "fetching latest", ratio = 1, isKnownLength = true });

		var view = job.ToView(0);
		Assert.That(view.progress.Select(p => (p.name, p.ratio)),
			Is.EqualTo(new[] { ("fetching latest", 1f), ("build vip", 0.1f) }));
		Assert.That(view.channels, Is.Empty);
	}
}

/// <summary>
/// Unit tests for <see cref="LocalWebServer"/>'s request guard and the bundles routes. The API runs CLI commands with
/// this CLI's credentials, so only the page's own origin, with this run's session token, gets through.
/// </summary>
public class LocalWebServerTests
{
	const string Token = "session-secret";

	static LocalWebServer NewServer() =>
		new("bundles", new BundlesApi(new BundlesManagementContext { Jobs = new CliJobRunner(System.Array.Empty<string>()) }), Token);

	static BundlesApi NewApi() => new(new BundlesManagementContext { Jobs = new CliJobRunner(System.Array.Empty<string>()) });

	[Test]
	public void Authorize_AcceptsThePageWithTheToken()
	{
		var server = NewServer();

		Assert.That(server.Authorize(server.Origin, "Bearer " + Token).status, Is.EqualTo(200));
		// A same-origin GET carries no Origin header.
		Assert.That(server.Authorize(null, "Bearer " + Token).status, Is.EqualTo(200));
	}

	[TestCase(null)]
	[TestCase("Bearer wrong")]
	[TestCase(Token)]
	public void Authorize_RefusesAMissingOrWrongToken(string authorization)
	{
		var server = NewServer();

		Assert.That(server.Authorize(server.Origin, authorization).status, Is.EqualTo(401));
	}

	[TestCase("https://evil.example")]
	[TestCase("http://localhost:4950")]
	[TestCase("http://127.0.0.1:1")]
	public void Authorize_RefusesOtherOrigins(string origin)
	{
		Assert.That(NewServer().Authorize(origin, "Bearer " + Token).status, Is.EqualTo(403));
	}

	[TestCase("GET", "unknown", "")]
	[TestCase("DELETE", "jobs", "")]
	[TestCase("GET", "jobs/nope", "")]
	[TestCase("POST", "jobs/nope/cancel", "")]
	public void Dispatch_UnknownRoutesAndJobs_AreNotFound(string method, string path, string body)
	{
		var ex = Assert.Throws<LocalWebRequestException>(() => NewApi().Dispatch(method, path.Split('/'), null, body));

		Assert.That(ex!.Status, Is.EqualTo(404));
	}

	[TestCase("{\"op\":\"unknown\"}")]
	[TestCase("{\"op\":\"plan\",\"bundle\":\"--help\"}")]
	[TestCase("not json")]
	public void Dispatch_StartJob_RejectsBadRequests(string body)
	{
		var ex = Assert.Throws<LocalWebRequestException>(() => NewApi().Dispatch("POST", new[] { "jobs" }, null, body));

		Assert.That(ex!.Status, Is.EqualTo(400));
	}

	[Test]
	public void Dispatch_ListJobs_StartsEmpty()
	{
		Assert.That(NewApi().Dispatch("GET", new[] { "jobs" }, null, ""), Is.Empty);
	}

	/// <summary>Only embedded names are served: a path can't climb out of the app's folder.</summary>
	[TestCase("/../../appsettings.json")]
	[TestCase("/..%2F..%2Fcli.dll")]
	[TestCase("/runtime/../stacks/index.html.bak")]
	[TestCase("/nope.js")]
	public void Assets_OnlyServeEmbeddedFiles(string path)
	{
		Assert.That(LocalWebAssets.TryRead("stacks", path, out _, out _), Is.False);
	}

	/// <summary>
	/// The BeamableProduct checkout is found by what's in it, so any naming works: a download's BeamableProduct-branch
	/// next to agentic-portal-branch is tried before a plain BeamableProduct, and that before unrelated folders.
	/// </summary>
	[Test]
	public void Checkout_NextToTheWorkspace_PrefersTheSameSuffix()
	{
		var dir = Path.Combine(Path.GetTempPath(), "beam-checkouts-" + Path.GetRandomFileName());
		foreach (var name in new[] { "agentic-portal-feat-x", "BeamableAPI-feat-x", "BeamableProduct", "BeamableProduct-feat-x", "aaa-other" })
			Directory.CreateDirectory(Path.Combine(dir, name));
		try
		{
			var order = LocalStackPortalManagement.ProductCheckoutsNextTo(Path.Combine(dir, "agentic-portal-feat-x")).Select(Path.GetFileName).ToList();
			Assert.That(order, Does.Not.Contain("agentic-portal-feat-x"));
			Assert.That(order.IndexOf("BeamableProduct-feat-x"), Is.LessThan(order.IndexOf("BeamableProduct")));
			Assert.That(order.IndexOf("BeamableProduct"), Is.LessThan(order.IndexOf("aaa-other")));
		}
		finally
		{
			Directory.Delete(dir, true);
		}
	}

	/// <summary>Served from the checkout's build (a CLI built without its apps): only files inside that folder.</summary>
	[Test]
	public void Assets_FromTheCheckout_StayInsideItsBuild()
	{
		var dir = Path.Combine(Path.GetTempPath(), "beam-localweb-" + Path.GetRandomFileName());
		Directory.CreateDirectory(Path.Combine(dir, "LocalWeb", "stacks"));
		File.WriteAllText(Path.Combine(dir, "LocalWeb", "stacks", "index.html"), "<html>");
		File.WriteAllText(Path.Combine(dir, "secret.txt"), "no");
		try
		{
			var web = Path.Combine(dir, "LocalWeb");
			Assert.That(LocalWebAssets.CheckoutFile(web, "stacks/index.html"), Is.Not.Null);
			Assert.That(LocalWebAssets.CheckoutFile(web, "../secret.txt"), Is.Null);
			Assert.That(LocalWebAssets.CheckoutFile(web, "stacks/../../secret.txt"), Is.Null);
			Assert.That(LocalWebAssets.CheckoutFile(web, "stacks/missing.js"), Is.Null);
		}
		finally
		{
			Directory.Delete(dir, true);
		}
	}
}

/// <summary>
/// Unit tests for <see cref="LocalStackManagementApi.BuildArgs"/>: what the stacks page may ask the CLI to run. Values go
/// as single <c>--option=value</c> tokens and must start alphanumeric, so none can parse as an option.
/// </summary>
public class LocalStackManagementApiTests
{
	static string[] Build(string json) => LocalStackManagementApi.BuildArgs(JObject.Parse(json)).args;

	[Test]
	public void Create_PassesEveryFieldAsOneToken()
	{
		var args = Build("{\"op\":\"create\",\"name\":\"wallet-dev\",\"shared\":true,\"extends\":\"backend\"," +
		                 "\"include\":[\"scala: auth\",\"c# gateway\"],\"exclude\":[],\"services\":[\"DebugWalletService\"]," +
		                 "\"extensions\":[],\"groups\":[],\"target\":\"dev\",\"description\":\"--quiet please\"}");

		Assert.That(args, Is.EqualTo(new[]
		{
			"local", "create", "wallet-dev", "--shared", "--extends", "backend", "--include", "scala: auth,c# gateway",
			"--services", "DebugWalletService", "--target", "dev", "--description=--quiet please"
		}));
	}

	[Test]
	public void Edit_PassesEmptyFieldsSoTheyAreCleared()
	{
		var args = Build("{\"op\":\"edit\",\"name\":\"mine\",\"extends\":\"\",\"include\":[],\"target\":\"\",\"description\":\"\"}");

		Assert.That(args, Is.EqualTo(new[]
		{
			"local", "edit", "mine", "--extends", "", "--include", "", "--target", "", "--description", ""
		}));
	}

	[Test]
	public void Commands_GoAsOneJsonToken_KeepingOnlyWhatTheCliReads()
	{
		var args = Build("{\"op\":\"create\",\"name\":\"seeded\",\"commands\":[{\"name\":\"seed: demo\",\"command\":\"node\"," +
		                 "\"args\":\"scripts/seed.mjs --fast\",\"env\":[\"MODE=demo\"],\"once\":true,\"extra\":\"--purge\"}]}");

		Assert.That(args, Is.EqualTo(new[]
		{
			"local", "create", "seeded",
			"--commands=[{\"name\":\"seed: demo\",\"command\":\"node\",\"args\":\"scripts/seed.mjs --fast\",\"env\":[\"MODE=demo\"],\"once\":true}]"
		}));
	}

	[Test]
	public void Commands_EmptyClearsThemOnlyOnEdit()
	{
		Assert.That(Build("{\"op\":\"create\",\"name\":\"a\",\"commands\":[]}"), Is.EqualTo(new[] { "local", "create", "a" }));
		Assert.That(Build("{\"op\":\"edit\",\"name\":\"a\",\"commands\":[]}"), Is.EqualTo(new[] { "local", "edit", "a", "--commands=[]" }));
	}

	[TestCase("{\"op\":\"create\",\"name\":\"ok\",\"commands\":{\"name\":\"x\"}}")]
	[TestCase("{\"op\":\"create\",\"name\":\"ok\",\"commands\":[\"x\"]}")]
	[TestCase("{\"op\":\"create\",\"name\":\"ok\",\"commands\":[{\"name\":{\"nested\":1}}]}")]
	public void BadCommands_AreRejected(string json)
	{
		var ex = Assert.Throws<LocalWebRequestException>(() => Build(json));

		Assert.That(ex!.Status, Is.EqualTo(400));
	}

	[TestCase("\u001b[36mvite v7.3.7 \u001b[32mbuilding client\u001b[39m", "vite v7.3.7 building client")]
	[TestCase("\u001b[2massets/\u001b[22m\u001b[35mstyle.css\u001b[39m \u001b[1m\u001b[2m 13.40 kB\u001b[22m", "assets/style.css  13.40 kB")]
	[TestCase("\u001b]0;title\u0007plain\r", "plain")]
	[TestCase("no codes", "no codes")]
	[TestCase("-- Stack full is up 12 ready ----------------------------------", "-- Stack full is up 12 ready")]
	[TestCase("a -- b", "a -- b")]
	public void Logs_DropTerminalCodes(string line, string plain)
	{
		Assert.That(LocalStackManagementApi.Plain(line), Is.EqualTo(plain));
	}

	[Test]
	public void Grafana_LinksEachStepToItsKindsDashboard_OnlyWhenLogsAreForwarded()
	{
		var snapshot = new LocalStackPsCommandResult
		{
			stack = "csharp-portal:web",
			steps = new System.Collections.Generic.List<LocalStackPsEntry>
			{
				new() { name = "scala: auth" }, new() { name = "c# gateway" }, new() { name = "seed: demo" },
			},
		};
		Assert.That(LocalStackManagementApi.Grafana(snapshot), Is.Null, "no logs: forward step, nothing in Grafana");

		snapshot.steps.Add(new LocalStackPsEntry { name = "logs: forward" });
		var steps = (System.Collections.Generic.Dictionary<string, string>)JObject.FromObject(LocalStackManagementApi.Grafana(snapshot))["steps"]!
			.ToObject(typeof(System.Collections.Generic.Dictionary<string, string>))!;
		Assert.That(steps["scala: auth"], Is.EqualTo("http://localhost:3001/d/beam-local-scala?from=now-1h&to=now&var-stack=csharp-portal%3Aweb&var-step=scala%3A%20auth"));
		Assert.That(steps["c# gateway"], Does.StartWith("http://localhost:3001/d/beam-local-csharp?"));
		Assert.That(steps["seed: demo"], Does.StartWith("http://localhost:3001/d/beam-local-other?"));
	}

	[Test]
	public void DryRun_AndDelete()
	{
		Assert.That(Build("{\"op\":\"dry-run\",\"stack\":\"csharp-portal:web\"}"),
			Is.EqualTo(new[] { "local", "run", "csharp-portal:web", "--dry-run" }));
		Assert.That(Build("{\"op\":\"delete\",\"name\":\"mine\"}"), Is.EqualTo(new[] { "local", "delete", "mine" }));
	}

	[TestCase("{\"op\":\"create\",\"name\":\"--help\"}")]
	[TestCase("{\"op\":\"create\",\"name\":\"a b\"}")]
	[TestCase("{\"op\":\"create\",\"name\":\"ok\",\"include\":[\"-x\"]}")]
	[TestCase("{\"op\":\"create\",\"name\":\"ok\",\"target\":\"dev --purge\"}")]
	[TestCase("{\"op\":\"dry-run\",\"stack\":\"full::web\"}")]
	[TestCase("{\"op\":\"run\"}")]
	public void BadValues_AreRejected(string json)
	{
		var ex = Assert.Throws<LocalWebRequestException>(() => Build(json));

		Assert.That(ex!.Status, Is.EqualTo(400));
	}
}
