using cli.Services.LocalStack;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace tests;

/// <summary>
/// Covers the container watchdog. Docker steps are run-to-completion, so once <c>compose up</c> returns
/// nothing in <c>up</c> ever looks at those containers again — a container can die mid-run and the only
/// symptom is a rising tide of connection errors underneath an already-printed "Stack is up". That is
/// exactly what happened on 2026-08-07 (mongo_master exit 139, then the whole Docker VM at exit 255), and
/// the run kept streaming for 27 more minutes.
///
/// Two behaviours matter most and are tested hardest: a one-shot init container at <c>exited 0</c> must
/// never be called dead (or the watchdog fires on every healthy run and gets ignored), and a container that
/// keeps dying must stop being restarted.
/// </summary>
public class LocalStackDockerWatchdogTests
{
	private static LocalStackStep DockerStep(string arguments, string workingDirectory = "/tmp/compose",
		int readyRetries = 0) =>
		new LocalStackStep
		{
			name = "docker: test",
			command = "docker",
			arguments = arguments,
			workingDirectory = workingDirectory,
			waitForExit = true,
			readyRetries = readyRetries,
		};

	private static string Row(string service, string name, string state, int exitCode) =>
		$"{{\"Service\":\"{service}\",\"Name\":\"{name}\",\"State\":\"{state}\",\"ExitCode\":{exitCode}}}";

	#region classification

	[Test]
	public void A_running_container_is_alive()
	{
		var parsed = LocalStackDockerWatchdog.ParsePs(Row("redis", "local-redis-1", "running", 0)).Single();
		Assert.That(parsed.IsDead, Is.False);
	}

	[Test]
	public void A_one_shot_init_container_that_exited_zero_is_not_dead()
	{
		// mongo_master_setup / mongo_customer_setup sit at "exited 0" for the whole life of a healthy stack.
		// Calling these dead would fire the watchdog on every run.
		var parsed = LocalStackDockerWatchdog
			.ParsePs(Row("mongo_master_setup", "mongo_master_setup", "exited", 0)).Single();

		Assert.That(parsed.IsDead, Is.False,
			"a zero-exit one-shot init container must never be reported as a failure");
	}

	[Test]
	public void A_segfaulted_container_is_dead_and_explains_itself()
	{
		var parsed = LocalStackDockerWatchdog.ParsePs(Row("mongo_master", "mongo_master", "exited", 139)).Single();

		Assert.That(parsed.IsDead, Is.True);
		Assert.That(parsed.Explain(), Does.Contain("SIGSEGV"));
	}

	[Test]
	public void A_container_killed_with_the_docker_vm_is_dead_and_explains_itself()
	{
		var parsed = LocalStackDockerWatchdog.ParsePs(Row("caddy", "beamableapi-caddy-1", "exited", 255)).Single();

		Assert.That(parsed.IsDead, Is.True);
		Assert.That(parsed.Explain(), Does.Contain("Docker VM"));
	}

	[Test]
	public void A_restarting_container_with_a_nonzero_exit_is_dead()
	{
		var parsed = LocalStackDockerWatchdog.ParsePs(Row("broker", "broker-1", "restarting", 1)).Single();
		Assert.That(parsed.IsDead, Is.True);
	}

	#endregion

	#region liveness baseline

	private static List<LocalStackDockerWatchdog.ContainerState> Poll(params string[] rows) =>
		LocalStackDockerWatchdog.ParsePs(string.Join("\n", rows));

	[Test]
	public void A_baseline_container_that_exits_cleanly_is_dead()
	{
		// `docker stop mongo_master` really does yield "exited 0" — mongod handles SIGTERM. A vanished
		// mongo_master is fatal to the stack however politely it left, so exit code alone cannot decide this.
		var tracker = new LocalStackDockerWatchdog.LivenessTracker();

		Assert.That(tracker.Dead(Poll(Row("mongo_master", "mongo_master", "running", 0))), Is.Empty);

		var dead = tracker.Dead(Poll(Row("mongo_master", "mongo_master", "exited", 0)));

		Assert.That(dead.Select(c => c.name), Is.EquivalentTo(new[] { "mongo_master" }));
		Assert.That(dead.Single().Explain(), Does.Contain("was running earlier"));
	}

	[Test]
	public void An_init_container_already_finished_at_the_first_poll_stays_exempt()
	{
		var tracker = new LocalStackDockerWatchdog.LivenessTracker();
		var poll = Poll(
			Row("mongo_master", "mongo_master", "running", 0),
			Row("mongo_master_setup", "mongo_master_setup", "exited", 0));

		for (var i = 0; i < 5; i++)
			Assert.That(tracker.Dead(poll), Is.Empty, $"poll {i} must not flag a zero-exit init container");
	}

	[Test]
	public void An_init_container_that_re_runs_after_a_restart_is_not_adopted_into_the_baseline()
	{
		// Restarting a step re-runs its init containers, so the watchdog can catch one mid-run. Its normal
		// completion must not be reported as a death — this was a live false positive before the baseline seal.
		var tracker = new LocalStackDockerWatchdog.LivenessTracker();

		tracker.Dead(Poll(
			Row("mongo_master", "mongo_master", "running", 0),
			Row("mongo_master_setup", "mongo_master_setup", "exited", 0)));

		// the restart re-ran it, so a later poll sees it running...
		Assert.That(tracker.Dead(Poll(
			Row("mongo_master", "mongo_master", "running", 0),
			Row("mongo_master_setup", "mongo_master_setup", "running", 0))), Is.Empty);

		// ...and then finishing normally
		Assert.That(tracker.Dead(Poll(
			Row("mongo_master", "mongo_master", "running", 0),
			Row("mongo_master_setup", "mongo_master_setup", "exited", 0))), Is.Empty,
			"an init container that completes after a restart must not read as a failure");

		Assert.That(tracker.InBaseline("mongo_master_setup"), Is.False);
		Assert.That(tracker.InBaseline("mongo_master"), Is.True);
	}

	[Test]
	public void A_nonzero_exit_is_dead_even_outside_the_baseline()
	{
		// The watchdog can start after a crash (the container died during bring-up), so the exit code must
		// still stand on its own.
		var tracker = new LocalStackDockerWatchdog.LivenessTracker();

		var dead = tracker.Dead(Poll(Row("mongo_master", "mongo_master", "exited", 139)));

		Assert.That(dead.Select(c => c.name), Is.EquivalentTo(new[] { "mongo_master" }));
		Assert.That(tracker.InBaseline("mongo_master"), Is.False);
	}

	[Test]
	public void A_restarted_baseline_container_reads_alive_and_a_second_death_is_caught()
	{
		var tracker = new LocalStackDockerWatchdog.LivenessTracker();
		var up = Poll(Row("mongo_master", "mongo_master", "running", 0));
		var down = Poll(Row("mongo_master", "mongo_master", "exited", 139));

		Assert.That(tracker.Dead(up), Is.Empty);
		Assert.That(tracker.Dead(down), Has.Count.EqualTo(1));
		Assert.That(tracker.Dead(up), Is.Empty, "a restart must read as alive again");
		Assert.That(tracker.Dead(down), Has.Count.EqualTo(1), "and a second crash must still be caught");
	}

	[Test]
	public void An_empty_first_poll_seals_an_empty_baseline_without_throwing()
	{
		var tracker = new LocalStackDockerWatchdog.LivenessTracker();

		Assert.That(tracker.Dead(null), Is.Empty);
		Assert.That(tracker.Dead(Poll(Row("redis", "local-redis-1", "exited", 0))), Is.Empty,
			"nothing was in the baseline, and a clean exit is not evidence of failure on its own");
	}

	#endregion

	#region compose ps parsing

	[Test]
	public void Newline_delimited_json_is_parsed()
	{
		var stdout = string.Join("\n",
			Row("redis", "local-redis-1", "running", 0),
			Row("mongo_master", "mongo_master", "exited", 139));

		var parsed = LocalStackDockerWatchdog.ParsePs(stdout);

		Assert.That(parsed.Select(c => c.name),
			Is.EquivalentTo(new[] { "local-redis-1", "mongo_master" }));
		Assert.That(parsed.Count(c => c.IsDead), Is.EqualTo(1));
	}

	[Test]
	public void A_json_array_is_parsed()
	{
		var stdout = $"[{Row("redis", "local-redis-1", "running", 0)}," +
		             $"{Row("caddy", "beamableapi-caddy-1", "exited", 255)}]";

		var parsed = LocalStackDockerWatchdog.ParsePs(stdout);

		Assert.That(parsed, Has.Count.EqualTo(2));
		Assert.That(parsed.Single(c => c.IsDead).name, Is.EqualTo("beamableapi-caddy-1"));
	}

	[Test]
	public void Empty_output_yields_nothing()
	{
		Assert.That(LocalStackDockerWatchdog.ParsePs(null), Is.Empty);
		Assert.That(LocalStackDockerWatchdog.ParsePs("   "), Is.Empty);
	}

	[Test]
	public void A_malformed_line_is_skipped_and_the_rest_still_parses()
	{
		var stdout = string.Join("\n",
			"this is not json",
			Row("redis", "local-redis-1", "running", 0),
			"{\"Service\":\"broken\"");

		var parsed = LocalStackDockerWatchdog.ParsePs(stdout);

		Assert.That(parsed, Has.Count.EqualTo(1), "a bad row must not lose the good rows in the same poll");
		Assert.That(parsed.Single().name, Is.EqualTo("local-redis-1"));
	}

	#endregion

	#region target discovery

	[Test]
	public void A_compose_up_naming_a_service_watches_only_that_service()
	{
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep> { DockerStep("compose up -d --no-deps redis") }
		};

		var target = LocalStackDockerWatchdog.DiscoverTargets(config.steps, config).Single();

		Assert.That(target.services, Is.EquivalentTo(new[] { "redis" }));
		Assert.That(target.Watches("redis"), Is.True);
		Assert.That(target.Watches("mongo_master"), Is.False);
	}

	[Test]
	public void A_whole_project_compose_up_watches_every_service()
	{
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep> { DockerStep("compose up -d --wait") }
		};

		var target = LocalStackDockerWatchdog.DiscoverTargets(config.steps, config).Single();

		Assert.That(target.services, Is.Empty);
		Assert.That(target.Watches("anything-at-all"), Is.True);
	}

	[Test]
	public void A_flag_value_is_not_mistaken_for_a_service_name()
	{
		var tokens = LocalStackDockerWatchdog.Tokenize("compose -f other.yml up -d --profile web redis");
		var up = LocalStackDockerWatchdog.IndexOfSubcommand(tokens, "compose", "up");

		var services = LocalStackDockerWatchdog.ServiceNamesAfterUp(tokens, up);

		Assert.That(services, Is.EquivalentTo(new[] { "redis" }),
			"'web' is the value of --profile, and 'other.yml' precedes 'up' — neither is a service");
	}

	[Test]
	public void Non_docker_and_non_up_steps_yield_no_targets()
	{
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep>
			{
				DockerStep("compose down"),
				DockerStep("compose stop"),
				new LocalStackStep { name = "scala: gateway", command = "java", arguments = "-cp x App" },
				new LocalStackStep { name = "off", command = "docker", arguments = "compose up -d", enabled = false },
				new LocalStackStep { name = "build", command = "docker", arguments = "compose up -d", build = true },
			}
		};

		Assert.That(LocalStackDockerWatchdog.DiscoverTargets(config.steps, config), Is.Empty);
	}

	[Test]
	public void A_quoted_path_survives_tokenization_as_one_token()
	{
		var tokens = LocalStackDockerWatchdog.Tokenize("compose -f \"C:\\Program Files\\a.yml\" up -d");

		Assert.That(tokens, Does.Contain("C:\\Program Files\\a.yml"));
		Assert.That(LocalStackDockerWatchdog.IndexOfSubcommand(tokens, "compose", "up"), Is.EqualTo(3));
	}

	#endregion

	#region restart budget

	[Test]
	public void A_step_without_ready_retries_gets_the_default_budget()
	{
		Assert.That(LocalStackDockerWatchdog.RestartAttemptsFor(DockerStep("compose up -d")),
			Is.EqualTo(LocalStackDockerWatchdog.DefaultRestartAttempts));
	}

	[Test]
	public void A_step_with_ready_retries_uses_them_as_its_budget()
	{
		Assert.That(LocalStackDockerWatchdog.RestartAttemptsFor(DockerStep("compose up -d", readyRetries: 5)),
			Is.EqualTo(5));
	}

	#endregion

	[Test]
	public void The_watchdog_is_enabled_by_default_and_can_be_turned_off()
	{
		Assert.That(new LocalStackConfig().dockerWatchdogSeconds, Is.EqualTo(15));
		Assert.That(new LocalStackConfig { dockerWatchdogSeconds = 0 }.dockerWatchdogSeconds, Is.EqualTo(0),
			"0 must be expressible so the poll can be disabled");
	}
}
