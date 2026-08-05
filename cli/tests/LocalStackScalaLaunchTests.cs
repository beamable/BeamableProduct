using System;
using System.Collections.Generic;
using System.Linq;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers how `beam local init` emits the Scala service steps: the dbid provider's position in the bring-up,
/// the JVM heap cap, and the launch script's tolerance of a failed classpath resolve. All three came out of a
/// run that "succeeded" while logging a wall of errors — see the individual test docs.
/// </summary>
public class LocalStackScalaLaunchTests
{
	private static readonly string[] Tools = { "account", "auth", "dbflake", "gateway", "stats" };

	private static LocalStackConfig Create(string jvmArgs = null) =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = @"C:\repos\BeamableBackend",
			portalDir = @"C:\repos\portal",
			scalaJvmArgs = jvmArgs ?? LocalStackTemplate.DefaultScalaJvmArgs,
			scalaTools = Tools
				.Select(n => new LocalStackTemplate.ScalaToolInfo { name = n, mainClass = $"com.beamable.{n}.App" })
				.ToList(),
		});

	private static LocalStackStep Step(LocalStackConfig c, string name) => c.steps.First(s => s.name == name);

	private static int IndexOf(LocalStackConfig c, string name) => c.steps.FindIndex(s => s.name == name);

	/// <summary>
	/// dbflake serves the dbids every other Scala service fetches at boot. While it took its alphabetical slot
	/// in the parallel group it started 8th, and the six services ahead of it spent ~15s logging
	/// `Failed to fetch DBIDs / ServiceClient timeout` before recovering. Being ungrouped is what makes `up`
	/// await its readiness gate before launching the rest.
	/// </summary>
	[Test]
	public void Dbflake_is_launched_and_awaited_before_the_parallel_group()
	{
		var config = Create();
		var dbflake = Step(config, "scala: dbflake");

		Assert.That(dbflake.group, Is.Null.Or.Empty, "dbflake must not share the parallel group it feeds");
		Assert.That(dbflake.readyWhenLogContains, Is.EqualTo("Service Started"),
			"it needs a readiness gate, or being first buys nothing");

		foreach (var other in Tools.Where(t => t != "dbflake"))
		{
			Assert.That(IndexOf(config, "scala: dbflake"), Is.LessThan(IndexOf(config, $"scala: {other}")),
				$"dbflake must be ordered before scala: {other}");
		}
	}

	[Test]
	public void Every_other_scala_service_still_launches_in_parallel()
	{
		var config = Create();

		foreach (var other in Tools.Where(t => t != "dbflake"))
		{
			Assert.That(Step(config, $"scala: {other}").group, Is.EqualTo("scala"),
				$"scala: {other} is a dbid consumer and must stay in the parallel group");
		}
	}

	/// <summary>
	/// JDK 8 defaults -Xmx to a QUARTER of physical RAM, so each of ~18 unflagged JVMs reserves double-digit
	/// gigabytes and they eventually cannot reserve address space at all ("Could not reserve enough space for
	/// object heap", then a native-OOM crash of the whole Akka JVM). Every launch script must carry a cap.
	/// </summary>
	[Test]
	public void Every_scala_launch_caps_the_jvm_heap()
	{
		var config = Create();

		foreach (var tool in Tools)
		{
			var arguments = Step(config, $"scala: {tool}").arguments;
			Assert.That(arguments, Does.Contain("-Xmx512m"), $"scala: {tool} must cap max heap");
			Assert.That(arguments, Does.Contain("-Xms256m"), $"scala: {tool} must set an initial heap");
		}
	}

	[Test]
	public void The_heap_cap_is_configurable()
	{
		var arguments = Step(Create("-Xmx1g"), "scala: auth").arguments;

		Assert.That(arguments, Does.Contain("-Xmx1g"));
		Assert.That(arguments, Does.Not.Contain("-Xmx512m"));
	}

	/// <summary>
	/// With no flags configured the invocation must carry no argument at all — an empty string would reach the
	/// JVM as an unrecognized `""` option and kill the service on startup.
	/// </summary>
	[Test]
	public void No_configured_flags_emits_no_empty_argument()
	{
		var arguments = Step(Create(string.Empty), "scala: auth").arguments;

		if (OperatingSystem.IsWindows())
		{
			Assert.That(arguments, Does.Contain("$jvmArgs = @()"));
		}
		else
		{
			Assert.That(arguments, Does.Contain("JVM_ARGS=''"));
		}
	}

	/// <summary>
	/// When `mvn dependency:build-classpath` fails, the cache file is left empty. The PowerShell launcher read it
	/// as `(Get-Content $cpf -Raw).Trim()`, which throws "You cannot call a method on a null-valued expression" on
	/// null — the service then never launched and the reason was invisible. Both variants must now say what broke.
	/// </summary>
	[Test]
	public void An_empty_classpath_cache_fails_with_an_explanation()
	{
		var arguments = Step(Create(), "scala: auth").arguments;

		Assert.That(arguments, Does.Contain("is empty"), "the launcher must explain an empty classpath cache");
		Assert.That(arguments, Does.Contain("exit 1"), "and it must stop rather than launch a broken classpath");

		if (OperatingSystem.IsWindows())
		{
			Assert.That(arguments, Does.Not.Contain("-Raw).Trim()"),
				"reading the cache must not call .Trim() on a possibly-null Get-Content result");
		}
	}

	/// <summary>A tools/* service missing from the manifest's selection can't be hoisted — the bring-up must
	/// still be well-formed when dbflake simply isn't part of this stack.</summary>
	[Test]
	public void A_stack_without_dbflake_still_emits_its_group()
	{
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = @"C:\repos\BeamableBackend",
			portalDir = @"C:\repos\portal",
			scalaTools = new List<LocalStackTemplate.ScalaToolInfo>
			{
				new() { name = "auth", mainClass = "com.beamable.auth.App" },
			},
		});

		Assert.That(config.steps.Any(s => s.name == "scala: dbflake"), Is.False);
		Assert.That(Step(config, "scala: auth").group, Is.EqualTo("scala"));
	}
}
