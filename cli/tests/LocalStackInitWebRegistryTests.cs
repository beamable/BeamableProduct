using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tests.Examples;

namespace tests;

/// <summary>
/// <c>beam local setup</c> writes the settings file and its step catalogue, and <c>beam local create</c> writes
/// stacks against it. The web-registry steps are always written, since a stack runs them through the
/// <c>web-registry</c> tag (the <c>:web</c> variant); there is no standing choice to record any more.
/// </summary>
public class LocalStackInitWebRegistryTests : CLITest
{
	private static readonly string[] WebSteps =
	{
		LocalStackTemplate.WebRegistryStepName,
		LocalStackTemplate.WebPublishStepName,
		LocalStackTemplate.WebRefreshStepName,
	};

	private string SettingsPath => LocalStackCommand.SettingsPath(Path.Combine(WorkingDir, ".beamable"));

	private LocalStackConfig SetupAndLoad(params string[] extraArgs)
	{
		// --quiet: the test console reports itself as interactive, so without it `setup` would try to prompt.
		// --only settings: nothing else (toolchain downloads, docker, AWS) belongs in a unit test.
		Run(new[] { "local", "setup", "--only", "settings", "--quiet" }.Concat(extraArgs).ToArray());

		Assert.That(File.Exists(SettingsPath), Is.True, $"setup wrote no settings at {SettingsPath}");
		return LocalStackConfigIO.Load(SettingsPath);
	}

	[Test]
	public void Setup_writes_the_web_steps_into_every_catalogue()
	{
		var names = SetupAndLoad().steps.Select(s => s.name).ToList();

		foreach (var step in WebSteps)
		{
			Assert.That(names, Does.Contain(step), $"{step} must be in the catalogue so `:web` can run it");
		}

		Assert.That(File.ReadAllText(SettingsPath), Does.Not.Contain("\"webRegistry\""), "the standing choice is gone");
	}

	[Test]
	public void Web_registry_dir_is_recorded()
	{
		var dir = Path.Combine(WorkingDir, "portal-localdev");
		Directory.CreateDirectory(dir);

		var config = SetupAndLoad("--web-registry-dir", dir);

		Assert.That(config.repos.webRegistryDir, Is.EqualTo(dir));
	}

	/// <summary>A second setup only refreshes the toolchain block: a developer's edited steps must survive it.</summary>
	[Test]
	public void A_second_setup_keeps_the_catalogue()
	{
		var config = SetupAndLoad();
		config.steps.RemoveAll(s => s.name == "portal frontend");
		LocalStackConfigIO.Save(SettingsPath, config);

		Assert.That(SetupAndLoad().steps.Select(s => s.name), Does.Not.Contain("portal frontend"));
	}

	/// <summary>
	/// <c>--force</c> recreates the catalogue, but variables, targets and the port offset are only ever written by
	/// a person, so they carry over.
	/// </summary>
	[Test]
	public void Force_recreates_the_catalogue_and_keeps_what_a_person_wrote()
	{
		var config = SetupAndLoad();
		config.steps.RemoveAll(s => s.name == "portal frontend");
		config.variables = new Dictionary<string, string> { ["SeedOrg"] = "my-org" };
		config.portOffset = 100;
		LocalStackConfigIO.Save(SettingsPath, config);

		var recreated = SetupAndLoad("--force");

		Assert.That(recreated.steps.Select(s => s.name), Does.Contain("portal frontend"));
		Assert.That(recreated.variables["SeedOrg"], Is.EqualTo("my-org"));
		Assert.That(recreated.portOffset, Is.EqualTo(100));
	}

	[Test]
	public void Update_services_only_changes_the_service_steps()
	{
		SetupAndLoad();

		var updated = SetupAndLoad("--update-services", "--services", "MyService");

		Assert.That(updated.steps.Select(s => s.name), Does.Contain(LocalStackTemplate.MicroservicePrefix + "MyService"));
		Assert.That(updated.steps.Select(s => s.name), Does.Contain("portal frontend"));
	}

	// ----------------------------------------------------------------------------------
	// beam local setup --add-step / --remove-step / --list-steps
	// ----------------------------------------------------------------------------------

	private LocalStackStep Step(string name) =>
		LocalStackConfigIO.Load(SettingsPath).steps.FirstOrDefault(s => s.name == name);

	[Test]
	public void Add_step_from_flags_goes_where_it_was_asked()
	{
		SetupAndLoad();

		Run("local", "setup", "--quiet", "--add-step", "seed: campaigns", "--step-command", "node",
			"--step-args", "scripts/seed.mjs --org ${SeedOrg}", "--step-dir", "${PortalPath}", "--step-once",
			"--step-env", "MODE=fast", "--step-tags", "portal", "--step-after", "portal frontend");

		var names = LocalStackConfigIO.Load(SettingsPath).steps.Select(s => s.name).ToList();
		Assert.That(names.IndexOf("seed: campaigns"), Is.EqualTo(names.IndexOf("portal frontend") + 1));

		var step = Step("seed: campaigns");
		Assert.That(step.custom, Is.True);
		Assert.That(step.tags, Is.EqualTo(new[] { "custom", "portal" }));
		Assert.That(step.command, Is.EqualTo("node"));
		Assert.That(step.arguments, Is.EqualTo("scripts/seed.mjs --org ${SeedOrg}"));
		Assert.That(step.workingDirectory, Is.EqualTo("${PortalPath}"));
		Assert.That(step.waitForExit, Is.True);
		Assert.That(step.environment["MODE"], Is.EqualTo("fast"));
	}

	[Test]
	public void Shell_and_beam_steps_are_added_too()
	{
		SetupAndLoad();

		Run("local", "setup", "--quiet", "--add-step", "reset: fixtures", "--step-shell", "./scripts/reset.sh", "--step-shell-kind", "sh", "--step-once");
		Run("local", "setup", "--quiet", "--add-step", "content: publish", "--step-beam", "content publish", "--step-once");

		Assert.That(Step("reset: fixtures").shell, Is.True);
		Assert.That(Step("reset: fixtures").shellKind, Is.EqualTo("sh"));
		Assert.That(Step("content: publish").beam, Is.True);
		Assert.That(Step("content: publish").arguments, Is.EqualTo("content publish"));
	}

	[Test]
	public void A_bad_custom_step_changes_nothing()
	{
		SetupAndLoad();
		var before = File.ReadAllText(SettingsPath);

		Assert.That(RunFull(new[] { "local", "setup", "--quiet", "--add-step", "nothing to run" }), Is.Not.EqualTo(0));
		Assert.That(RunFull(new[] { "local", "setup", "--quiet", "--add-step", "portal frontend", "--step-command", "node" }), Is.Not.EqualTo(0),
			"a name already in the catalogue");
		Assert.That(RunFull(new[] { "local", "setup", "--quiet", "--add-step", "microservice: x", "--step-command", "node" }), Is.Not.EqualTo(0),
			"a name --update-services owns");
		Assert.That(RunFull(new[] { "local", "setup", "--quiet", "--add-step", "x", "--step-command", "node", "--step-after", "no such step" }), Is.Not.EqualTo(0));

		Assert.That(File.ReadAllText(SettingsPath), Is.EqualTo(before));
	}

	[Test]
	public void Only_custom_steps_are_removed_and_they_survive_a_recreated_catalogue()
	{
		SetupAndLoad();
		Run("local", "setup", "--quiet", "--add-step", "mock: payments", "--step-command", "npx", "--step-args", "json-server db.json",
			"--step-port", "7070", "--step-after", "portal frontend");

		SetupAndLoad("--force");
		var names = LocalStackConfigIO.Load(SettingsPath).steps.Select(s => s.name).ToList();
		Assert.That(names.IndexOf("mock: payments"), Is.EqualTo(names.IndexOf("portal frontend") + 1), "kept, in its place");

		Assert.That(RunFull(new[] { "local", "setup", "--quiet", "--remove-step", "portal frontend" }), Is.Not.EqualTo(0));
		Run("local", "setup", "--quiet", "--remove-step", "mock: payments");
		Assert.That(Step("mock: payments"), Is.Null);
		Assert.That(Step("portal frontend"), Is.Not.Null);

		Run("local", "setup", "--quiet", "--list-steps");
	}

	/// <summary>The wizard for a custom step, as a person meets it: only the questions that apply, then a review.</summary>
	[Test]
	public void Add_step_interactively()
	{
		SetupAndLoad();

		Ansi.Input.PushKey(ConsoleKey.Enter);          // the CLI's first-run telemetry question, in a fresh test workspace
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Runs: a program
		Ansi.Input.PushTextWithEnter("node");          // the program
		Ansi.Input.PushTextWithEnter("server.js");     // its arguments
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Folder: keep ${PortalPath}
		Ansi.Input.PushKey(ConsoleKey.DownArrow);      // Lifetime: keeps running
		Ansi.Input.PushKey(ConsoleKey.Enter);
		Ansi.Input.PushKey(ConsoleKey.DownArrow);      // Ready when: a URL answers
		Ansi.Input.PushKey(ConsoleKey.Enter);
		Ansi.Input.PushTextWithEnter("http://localhost:7070");
		Ansi.Input.PushTextWithEnter("7070");          // Port
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Tags: just custom
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Place: at the end
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Review: add it

		var exit = RunFull(new[] { "local", "setup", "--add-step", "mock: server" });
		TestContext.WriteLine(Ansi.Output);
		Assert.That(exit, Is.EqualTo(0));

		var step = LocalStackConfigIO.Load(SettingsPath).steps.Last();
		Assert.That(step.name, Is.EqualTo("mock: server"));
		Assert.That(step.custom, Is.True);
		Assert.That(step.command, Is.EqualTo("node"));
		Assert.That(step.arguments, Is.EqualTo("server.js"));
		Assert.That(step.workingDirectory, Is.EqualTo("${PortalPath}"));
		Assert.That(step.waitForExit, Is.False);
		Assert.That(step.readyWhenHttpOk, Is.EqualTo("http://localhost:7070"));
		Assert.That(step.port, Is.EqualTo(7070));
	}

	// ----------------------------------------------------------------------------------
	// beam local create
	// ----------------------------------------------------------------------------------

	private string LocalStacksDir => LocalStackStacks.StacksDir(WorkingDir, LocalStackLayer.Local);

	[Test]
	public void Create_writes_a_stack_that_resolves()
	{
		SetupAndLoad();

		Run("local", "create", "mine", "--quiet", "--extends", "backend", "--exclude", "c# analytics loader", "--description", "Mine");

		var file = Path.Combine(LocalStacksDir, "mine.json");
		Assert.That(File.Exists(file), Is.True);
		var written = JObject.Parse(File.ReadAllText(file))["mine"];
		Assert.That((string)written["extends"], Is.EqualTo("backend"));
		Assert.That(written["exclude"]!.Select(t => (string)t), Is.EqualTo(new[] { "c# analytics loader" }));
	}

	/// <summary>A personal variant on a built-in stack is a merge entry; the built-in stays untouched.</summary>
	[Test]
	public void Create_writes_a_variant_on_another_layer_as_a_merge_entry()
	{
		SetupAndLoad();

		Run("local", "create", "backend:with-portal", "--quiet", "--after", "portal frontend");

		var written = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "backend.json")))["backend"];
		Assert.That(written["variants"]?["with-portal"]?["after"]?.Select(t => (string)t), Is.EqualTo(new[] { "portal frontend" }));
		Assert.That(written["include"], Is.Null, "a merge entry must not copy the built-in stack");

		var resolved = LocalStackStacks.Load(WorkingDir).Resolve("backend:with-portal", LocalStackConfigIO.Load(SettingsPath));
		Assert.That(resolved.steps.Last().name, Is.EqualTo("portal frontend"));
	}

	[Test]
	public void Create_writes_nothing_when_the_stack_does_not_resolve()
	{
		SetupAndLoad();

		Assert.That(RunFull(new[] { "local", "create", "broken", "--quiet", "--include", "no such step" }), Is.Not.EqualTo(0));
		Assert.That(File.Exists(Path.Combine(LocalStacksDir, "broken.json")), Is.False);
	}

	private static readonly ConsoleKeyInfo ShiftTab = new ConsoleKeyInfo('\t', ConsoleKey.Tab, shift: true, alt: false, control: false);

	/// <summary>
	/// The interactive flow end to end, including going back: Shift+Tab in the tags picker must return to the
	/// question before it, and the answers given after that must be the ones written.
	/// </summary>
	[Test]
	public void Create_interactively_with_a_step_back()
	{
		SetupAndLoad();

		Ansi.Input.PushKey(ConsoleKey.Enter);          // the CLI's first-run telemetry question, in a fresh test workspace
		Ansi.Input.PushKey(ConsoleKey.Enter);          // What: "A new stack" (first question, no back)
		Ansi.Input.PushTextWithEnter("bbbb");          // name
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Who: "Just for me"
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Starts from: "(start from nothing)"
		Ansi.Input.PushKey(ShiftTab);                  // Adds: go back
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Starts from again: "(start from nothing)"
		Ansi.Input.PushKey(ConsoleKey.Spacebar);       // Adds: the Docker group, which ticks all of its steps
		Ansi.Input.PushKey(ConsoleKey.Enter);
		Ansi.Input.PushTextWithEnter("Mine");          // Description
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Review: "Write it"

		var exit = RunFull(new[] { "local", "create" });
		TestContext.WriteLine(Ansi.Output);
		Assert.That(exit, Is.EqualTo(0));

		var written = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "bbbb.json")))["bbbb"]!;
		Assert.That(written["include"]!.Select(t => (string)t), Is.EqualTo(new[] { "docker" }));
		Assert.That((string)written["description"], Is.EqualTo("Mine"));
	}

	/// <summary>Without a terminal there is nobody to ask, so a missing name fails instead of prompting.</summary>
	[Test]
	public void Create_without_a_name_in_quiet_mode_writes_nothing()
	{
		SetupAndLoad();

		Assert.That(RunFull(new[] { "local", "create", "--quiet" }), Is.Not.EqualTo(0));
		Assert.That(Directory.Exists(LocalStacksDir), Is.False);
	}

	// ----------------------------------------------------------------------------------
	// beam local delete
	// ----------------------------------------------------------------------------------

	[Test]
	public void Delete_removes_a_stack_and_its_emptied_file()
	{
		SetupAndLoad();
		Run("local", "create", "mine", "--quiet", "--extends", "backend");

		Run("local", "delete", "mine", "--quiet");

		Assert.That(File.Exists(Path.Combine(LocalStacksDir, "mine.json")), Is.False);
		Assert.That(LocalStackStacks.Load(WorkingDir).Contains("mine"), Is.False);
	}

	/// <summary>Deleting a personal variant on a built-in stack removes the merge entry; the built-in stays.</summary>
	[Test]
	public void Delete_removes_one_variant_and_keeps_the_rest()
	{
		SetupAndLoad();
		Run("local", "create", "backend:a", "--quiet", "--after", "portal frontend");
		Run("local", "create", "backend:b", "--quiet", "--after", "portal frontend");

		Run("local", "delete", "backend:a", "--quiet");

		var variants = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "backend.json")))["backend"]!["variants"]!;
		Assert.That(((JObject)variants).Properties().Select(p => p.Name), Is.EqualTo(new[] { "b" }));

		Run("local", "delete", "backend:b", "--quiet");
		Assert.That(File.Exists(Path.Combine(LocalStacksDir, "backend.json")), Is.False);
		Assert.That(LocalStackStacks.Load(WorkingDir).Contains("backend"), Is.True, "the built-in stack stays");
	}

	[Test]
	public void Delete_refuses_built_ins_and_stacks_others_extend()
	{
		SetupAndLoad();
		Run("local", "create", "base", "--quiet", "--extends", "backend");
		Run("local", "create", "child", "--quiet", "--extends", "base");

		Assert.That(RunFull(new[] { "local", "delete", "full", "--quiet" }), Is.Not.EqualTo(0));
		Assert.That(RunFull(new[] { "local", "delete", "base", "--quiet" }), Is.Not.EqualTo(0));
		Assert.That(File.Exists(Path.Combine(LocalStacksDir, "base.json")), Is.True, "a stack another one extends is kept");

		Run("local", "delete", "child", "--quiet");
		Run("local", "delete", "base", "--quiet");
		Assert.That(File.Exists(Path.Combine(LocalStacksDir, "base.json")), Is.False);
	}

	[Test]
	public void Delete_needs_shared_for_a_shared_stack_and_a_bare_delete_only_lists()
	{
		SetupAndLoad();
		Run("local", "create", "team", "--quiet", "--shared", "--extends", "backend", "--description", "Team");
		var sharedFile = Path.Combine(LocalStackStacks.StacksDir(WorkingDir, LocalStackLayer.Shared), "team.json");

		Assert.That(RunFull(new[] { "local", "delete", "team", "--quiet" }), Is.Not.EqualTo(0));
		Assert.That(File.Exists(sharedFile), Is.True);
		// A bare delete lists what can be deleted, like a bare run, and deletes nothing.
		Run("local", "delete", "--quiet");
		Assert.That(File.Exists(sharedFile), Is.True);

		Run("local", "delete", "team", "--shared", "--quiet");
		Assert.That(File.Exists(sharedFile), Is.False);
	}

	[Test]
	public void Create_shared_needs_a_description()
	{
		SetupAndLoad();

		Assert.That(RunFull(new[] { "local", "create", "team", "--quiet", "--shared", "--include", "docker" }), Is.Not.EqualTo(0));
		Assert.That(Directory.Exists(LocalStackStacks.StacksDir(WorkingDir, LocalStackLayer.Shared)), Is.False);
	}

	// ----------------------------------------------------------------------------------
	// beam local edit
	// ----------------------------------------------------------------------------------

	/// <summary>A flag replaces its field and an empty one clears it; the entry's variants and variables stay.</summary>
	[Test]
	public void Edit_changes_a_stack_and_keeps_the_rest_of_its_entry()
	{
		SetupAndLoad();
		Run("local", "create", "mine", "--quiet", "--extends", "backend", "--exclude", "c# analytics loader", "--description", "Mine");
		Run("local", "create", "mine:v", "--quiet", "--after", "portal frontend");
		var file = Path.Combine(LocalStacksDir, "mine.json");
		var json = JObject.Parse(File.ReadAllText(file));
		json["mine"]!["variables"] = JObject.Parse("{ \"Seed\": { \"description\": \"Seed size\" } }");
		File.WriteAllText(file, json.ToString());

		Run("local", "edit", "mine", "--quiet", "--include", "docker", "--exclude", "");

		var written = JObject.Parse(File.ReadAllText(file))["mine"]!;
		Assert.That(written["include"]!.Select(t => (string)t), Is.EqualTo(new[] { "docker" }));
		Assert.That(written["exclude"], Is.Null);
		Assert.That((string)written["extends"], Is.EqualTo("backend"));
		Assert.That((string)written["description"], Is.EqualTo("Mine"));
		Assert.That(written["variants"]?["v"], Is.Not.Null);
		Assert.That(written["variables"]?["Seed"], Is.Not.Null);

		var resolved = LocalStackStacks.Load(WorkingDir).Resolve("mine", LocalStackConfigIO.Load(SettingsPath));
		Assert.That(resolved.steps.Select(s => s.name), Does.Contain("c# analytics loader"), "no longer left out");
	}

	/// <summary>A personal variant on a built-in stack is edited in its merge entry; what edit doesn't ask about stays.</summary>
	[Test]
	public void Edit_changes_a_variant_and_keeps_force_build()
	{
		SetupAndLoad();
		Run("local", "create", "backend:a", "--quiet", "--after", "portal frontend");
		var file = Path.Combine(LocalStacksDir, "backend.json");
		var json = JObject.Parse(File.ReadAllText(file));
		json["backend"]!["variants"]!["a"]!["forceBuild"] = true;
		File.WriteAllText(file, json.ToString());

		Run("local", "edit", "backend:a", "--quiet", "--after", "", "--remove", "c# analytics loader", "--description", "No loader");

		var variant = JObject.Parse(File.ReadAllText(file))["backend"]!["variants"]!["a"]!;
		Assert.That(variant["after"], Is.Null);
		Assert.That(variant["remove"]!.Select(t => (string)t), Is.EqualTo(new[] { "c# analytics loader" }));
		Assert.That((string)variant["description"], Is.EqualTo("No loader"));
		Assert.That((bool)variant["forceBuild"], Is.True);

		var resolved = LocalStackStacks.Load(WorkingDir).Resolve("backend:a", LocalStackConfigIO.Load(SettingsPath));
		Assert.That(resolved.steps.Select(s => s.name), Does.Not.Contain("c# analytics loader"));
	}

	[Test]
	public void Edit_refuses_what_it_cannot_change_and_writes_nothing()
	{
		SetupAndLoad();
		Run("local", "create", "mine", "--quiet", "--extends", "backend");
		Run("local", "create", "backend:a", "--quiet", "--after", "portal frontend");
		Run("local", "create", "team", "--quiet", "--shared", "--extends", "backend", "--description", "Team");
		var mine = Path.Combine(LocalStacksDir, "mine.json");
		var before = File.ReadAllText(mine);

		Assert.That(RunFull(new[] { "local", "edit", "full", "--quiet", "--description", "x" }), Is.Not.EqualTo(0), "built in");
		Assert.That(RunFull(new[] { "local", "edit", "backend", "--quiet", "--description", "x" }), Is.Not.EqualTo(0), "the local entry only adds variants");
		Assert.That(RunFull(new[] { "local", "edit", "team", "--quiet", "--description", "x" }), Is.Not.EqualTo(0), "shared needs --shared");
		Assert.That(RunFull(new[] { "local", "edit", "mine", "--quiet" }), Is.Not.EqualTo(0), "nothing to change");
		Assert.That(RunFull(new[] { "local", "edit", "mine", "--quiet", "--include", "no such step" }), Is.Not.EqualTo(0), "doesn't resolve");
		Assert.That(RunFull(new[] { "local", "edit", "mine", "--quiet", "--extends", "" }), Is.Not.EqualTo(0), "nothing left to run");
		Assert.That(RunFull(new[] { "local", "edit", "backend:a", "--quiet", "--after", "" }), Is.Not.EqualTo(0), "a variant that changes nothing");
		Assert.That(File.ReadAllText(mine), Is.EqualTo(before));

		Run("local", "edit", "team", "--shared", "--quiet", "--description", "Team stack");
		var team = JObject.Parse(File.ReadAllText(Path.Combine(LocalStackStacks.StacksDir(WorkingDir, LocalStackLayer.Shared), "team.json")))["team"]!;
		Assert.That((string)team["description"], Is.EqualTo("Team stack"));
	}

	/// <summary>In a terminal, edit opens on the review with the flags already applied; saving writes them.</summary>
	[Test]
	public void Edit_interactively_opens_on_the_review()
	{
		SetupAndLoad();
		Run("local", "create", "mine", "--quiet", "--extends", "backend", "--description", "Mine");

		Ansi.Input.PushKey(ConsoleKey.Enter);          // the CLI's first-run telemetry question, when it is asked
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Review: "Save the changes"

		var exit = RunFull(new[] { "local", "edit", "mine", "--description", "Edited" });
		TestContext.WriteLine(Ansi.Output);
		Assert.That(exit, Is.EqualTo(0));
		Assert.That(Ansi.Output, Does.Contain("Save the changes"));

		var written = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "mine.json")))["mine"]!;
		Assert.That((string)written["description"], Is.EqualTo("Edited"));
		Assert.That((string)written["extends"], Is.EqualTo("backend"));
	}

	// ----------------------------------------------------------------------------------
	// beam local run, ps and stop: the board
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// End to end, on a stack of one step that only prints something: a detached run records its board, `ps` reads
	/// it back as the same rows, and `stop` clears it.
	/// </summary>
	[Test]
	public void Run_records_a_board_that_ps_reads_and_stop_clears()
	{
		SetupAndLoad();
		Run("local", "setup", "--quiet", "--add-step", "hello: print", "--step-shell",
			OperatingSystem.IsWindows() ? "Write-Output hello" : "echo hello", "--step-once", "--step-label", "Say hello");
		Run("local", "create", "hello", "--quiet", "--include", "hello: print");

		// --pretty: the test's output is redirected, which would otherwise make the board silent (data only).
		var exit = RunFull(new[] { "local", "run", "hello", "--detach", "--no-create-realm", "--quiet", "--pretty" });
		TestContext.WriteLine(Ansi.Output);
		Assert.That(exit, Is.EqualTo(0));
		// The test console counts as a terminal, so this is the live board, then the summary that replaces it.
		Assert.That(Ansi.Output, Does.Contain("Running stack hello").And.Contain("Say hello"));
		Assert.That(Ansi.Output, Does.Contain("Stack hello is up").And.Contain("1 of 1 ready"));

		var runState = LocalStackRunStateIO.ResolveRunStatePath(SettingsPath);
		var board = LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(runState));
		Assert.That(board, Is.Not.Null, "a detached run keeps its board for `ps`");
		Assert.That(board.stack, Is.EqualTo("hello"));
		var row = board.rows.Single();
		Assert.That(row.label, Is.EqualTo("Say hello"));
		Assert.That(row.state, Is.EqualTo(LocalStackStepState.Done));

		Assert.That(RunFull(new[] { "local", "ps", "--quiet" }), Is.EqualTo(0));
		Assert.That(Ansi.Output, Does.Contain("Running stack hello").And.Contain("Say hello"));

		Run("local", "stop", "--quiet");
		Assert.That(File.Exists(LocalStackBoardIO.PathFor(runState)), Is.False);
	}
}
