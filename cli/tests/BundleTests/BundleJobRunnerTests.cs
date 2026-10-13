using cli.DeploymentCommands;
using cli.Services.Bundles;
using NUnit.Framework;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace tests.BundleTests;

/// <summary>
/// Unit tests for <see cref="BundleJobRunner.BuildArgs"/> — the whitelist that turns a portal request into the
/// arguments of an existing CLI command. Everything the portal sends is validated before it reaches the parser.
/// </summary>
public class BundleJobRunnerTests
{
	static string[] Build(string op, string bundle = "", string tag = "", string checksum = "", string comment = "",
		string acl = "", string scope = "", bool flag = false) =>
		BundleJobRunner.BuildArgs(op, bundle, tag, checksum, comment, acl, scope, flag);

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
		Assert.That(() => Build(op, bundle), Throws.TypeOf<BundleRequestException>());
	}

	[TestCase("acl", "vip", "realm", "")]
	[TestCase("deploy-plan", "", "", "galaxy")]
	[TestCase("unknown", "vip", "", "")]
	public void UnknownValues_AreRejected(string op, string bundle, string acl, string scope)
	{
		Assert.That(() => Build(op, bundle, acl: acl, scope: scope), Throws.TypeOf<BundleRequestException>());
	}

	[Test]
	public void InvalidTag_IsRejected()
	{
		Assert.That(() => Build("tag", "vip@sha256:abcdef0123456789", tag: "--quiet x"), Throws.TypeOf<BundleRequestException>());
	}

	[Test]
	public void Reporter_KeepsEachProgressStepInStartOrder()
	{
		var job = new BundleJob();
		var reporter = new BundleJobReporter(job);

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
/// Unit tests for <see cref="BundlesManagementServer"/>'s request guard and routing. The API runs CLI commands with
/// this CLI's credentials, so only the portal's origin, on 127.0.0.1, with this run's session token gets through.
/// </summary>
public class BundlesManagementServerTests
{
	const string Portal = "http://localhost:4950";
	const string Token = "session-secret";

	static BundlesManagementServer NewServer() =>
		new(new BundlesManagementContext { Jobs = new BundleJobRunner(System.Array.Empty<string>()) }, Portal + "/some/page", Token);

	static string Host(BundlesManagementServer server) => $"127.0.0.1:{server.Port}";

	[Test]
	public void Authorize_AcceptsThePortalWithTheToken()
	{
		var server = NewServer();

		Assert.That(server.Authorize("GET", Host(server), Portal, "Bearer " + Token).status, Is.EqualTo(200));
	}

	[TestCase(null)]
	[TestCase("Bearer wrong")]
	[TestCase(Token)]
	public void Authorize_RefusesAMissingOrWrongToken(string authorization)
	{
		var server = NewServer();

		Assert.That(server.Authorize("GET", Host(server), Portal, authorization).status, Is.EqualTo(401));
	}

	[TestCase("https://evil.example")]
	[TestCase("http://localhost:4951")]
	[TestCase(null)]
	public void Authorize_RefusesOtherOrigins(string origin)
	{
		var server = NewServer();

		Assert.That(server.Authorize("GET", Host(server), origin, "Bearer " + Token).status, Is.EqualTo(403));
		Assert.That(server.Authorize("OPTIONS", Host(server), origin, null).status, Is.EqualTo(403));
	}

	[Test]
	public void Authorize_RefusesAHostThatIsNotTheLoopbackAddress()
	{
		// A DNS-rebinding page reaches 127.0.0.1 under its own host name.
		var server = NewServer();

		Assert.That(server.Authorize("GET", $"localhost:{server.Port}", Portal, "Bearer " + Token).status, Is.EqualTo(403));
		Assert.That(server.Authorize("GET", $"evil.example:{server.Port}", Portal, "Bearer " + Token).status, Is.EqualTo(403));
	}

	[Test]
	public void Authorize_LetsThePreflightThroughWithoutCredentials()
	{
		var server = NewServer();

		Assert.That(server.Authorize("OPTIONS", Host(server), Portal, null).status, Is.EqualTo(200));
	}

	[TestCase("GET", "/unknown", "")]
	[TestCase("DELETE", "/jobs", "")]
	[TestCase("GET", "/jobs/nope", "")]
	[TestCase("POST", "/jobs/nope/cancel", "")]
	public void Dispatch_UnknownRoutesAndJobs_AreNotFound(string method, string path, string body)
	{
		var ex = Assert.Throws<BundleRequestException>(() => NewServer().Dispatch(method, path, null, body));

		Assert.That(ex!.Status, Is.EqualTo(404));
	}

	[TestCase("{\"op\":\"unknown\"}")]
	[TestCase("{\"op\":\"plan\",\"bundle\":\"--help\"}")]
	[TestCase("not json")]
	public void Dispatch_StartJob_RejectsBadRequests(string body)
	{
		var ex = Assert.Throws<BundleRequestException>(() => NewServer().Dispatch("POST", "/jobs", null, body));

		Assert.That(ex!.Status, Is.EqualTo(400));
	}

	[Test]
	public void Dispatch_ListJobs_StartsEmpty()
	{
		Assert.That(NewServer().Dispatch("GET", "/jobs", null, ""), Is.Empty);
	}
}
