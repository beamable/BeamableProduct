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
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Own commands: Done
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Backend: local
		Ansi.Input.PushTextWithEnter("Mine");          // Description
		Ansi.Input.PushKey(ConsoleKey.Enter);          // Review: "Write it"

		var exit = RunFull(new[] { "local", "create" });
		TestContext.WriteLine(Ansi.Output);
		Assert.That(exit, Is.EqualTo(0));

		var written = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "bbbb.json")))["bbbb"]!;
		Assert.That(written["include"]!.Select(t => (string)t), Is.EqualTo(new[] { "docker" }));
		Assert.That((string)written["description"], Is.EqualTo("Mine"));
		Assert.That(written["target"], Is.Null, "local is what a stack runs against when it names nothing");
	}

	/// <summary>A stack or a variant can name its backend, as a target or as an address; an unknown one isn't written.</summary>
	[Test]
	public void Create_names_the_backend()
	{
		SetupAndLoad();

		Run("local", "create", "remote-gw", "--quiet", "--extends", "backend", "--target", "https://gw.example.com/");
		Run("local", "create", "backend:phones", "--quiet", "--target", "lan");
		Assert.That(RunFull(new[] { "local", "create", "odd", "--quiet", "--extends", "backend", "--target", "nowhere" }), Is.Not.EqualTo(0));

		var stacks = LocalStackStacks.Load(WorkingDir);
		var config = LocalStackConfigIO.Load(SettingsPath);
		Assert.That(stacks.Resolve("remote-gw", config).target, Is.EqualTo("https://gw.example.com"));
		Assert.That(stacks.Resolve("backend:phones", config).target, Is.EqualTo("lan"));
		Assert.That(stacks.Contains("odd"), Is.False);
		Assert.That(stacks.List().First(l => l.name == "remote-gw").target, Is.EqualTo("https://gw.example.com"));
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

	/// <summary>Setup only reports what .env.local says: stacks give the portal its host, so the file is never written.</summary>
	[Test]
	public void Portal_config_leaves_env_local_alone()
	{
		var portal = Path.Combine(WorkingDir, "portal-for-env");
		Directory.CreateDirectory(portal);
		var env = Path.Combine(portal, PortalEnvService.EnvFileName);

		Run("local", "setup", "--only", "portal-config", "--quiet", "--portal-dir", portal);
		Assert.That(File.Exists(env), Is.False, "not created");

		File.WriteAllText(env, "VITE_API_BASE=https://dev.api.beamable.com\nVITE_WINGMAN_URL=http://localhost:4960\n");
		var before = File.ReadAllText(env);
		Run("local", "setup", "--only", "portal-config", "--quiet", "--portal-dir", portal, "--force");
		Assert.That(File.ReadAllText(env), Is.EqualTo(before), "not rewritten, even with --force");
	}

	/// <summary>
	/// End to end on a detached stack of two steps that only sleep: running another stack name changes the running
	/// stack in place (a step it adds starts, one it drops stops, the rest keeps its process), a restart replaces
	/// one step's process, and stop takes everything down.
	/// </summary>
	[Test]
	public void A_running_stack_is_changed_in_place_and_a_step_restarted()
	{
		if (!OperatingSystem.IsWindows()) Assert.Ignore("The sleeping steps are PowerShell.");

		SetupAndLoad();
		Run("local", "setup", "--quiet", "--add-step", "sleep: a", "--step-shell", "Start-Sleep -Seconds 600");
		Run("local", "setup", "--quiet", "--add-step", "sleep: b", "--step-shell", "Start-Sleep -Seconds 600");
		Run("local", "create", "one", "--quiet", "--include", "sleep: a");
		Run("local", "create", "both", "--quiet", "--include", "sleep: a,sleep: b");

		var runStatePath = LocalStackRunStateIO.ResolveRunStatePath(SettingsPath);
		int Pid(string step) => LocalStackRunStateIO.Load(runStatePath)?.steps.FirstOrDefault(e => e.name == step)?.pid ?? 0;
		try
		{
			Run("local", "run", "one", "--detach", "--no-create-realm", "--quiet");
			var a = Pid("sleep: a");
			Assert.That(a, Is.GreaterThan(0));

			Run("local", "run", "both", "--detach", "--no-create-realm", "--quiet");
			Assert.That(Pid("sleep: a"), Is.EqualTo(a), "kept running");
			Assert.That(Pid("sleep: b"), Is.GreaterThan(0), "added");

			Run("local", "run", "one", "--no-create-realm", "--quiet");
			Assert.That(Pid("sleep: b"), Is.EqualTo(0), "dropped, even though this run asked to be attached: the running stack stays detached");
			Assert.That(Pid("sleep: a"), Is.EqualTo(a));

			Run("local", "ps", "--restart", "Sleep: a", "--quiet");
			Assert.That(Pid("sleep: a"), Is.Not.EqualTo(a).And.GreaterThan(0), "a new process");

			// Nothing named: the list of what can be named, and nothing restarted.
			a = Pid("sleep: a");
			var from = Ansi.Output.Length;
			Run("local", "ps", "--restart", "--quiet");
			var listed = Ansi.Output.Substring(from);
			Assert.That(listed, Does.Contain("custom").And.Contain("(sleep: a)").And.Contain("--all"));
			Assert.That(Pid("sleep: a"), Is.EqualTo(a), "nothing restarted");

			// ps prints the board, not its result as JSON; --json prints only that.
			from = Ansi.Output.Length;
			Run("local", "ps", "--quiet");
			Assert.That(Ansi.Output.Substring(from), Does.Contain("Sleep").And.Not.Contain("\"steps\""));

			Run("local", "run", "both", "--detach", "--no-create-realm", "--quiet");
			var b = Pid("sleep: b");
			Run("local", "ps", "--restart", "--all", "--quiet");
			Assert.That((Pid("sleep: a"), Pid("sleep: b")), Is.Not.EqualTo((a, b)));
			Assert.That(Pid("sleep: a"), Is.Not.EqualTo(a).And.GreaterThan(0), "everything restarted");
			Assert.That(Pid("sleep: b"), Is.Not.EqualTo(b).And.GreaterThan(0));
		}
		finally
		{
			var started = new[] { Pid("sleep: a"), Pid("sleep: b") }.Where(p => p > 0).ToList();
			RunFull(new[] { "local", "stop", "--quiet" });

			// A stopped process can take a moment to let go of the test folder, which the teardown then deletes.
			bool Alive(int pid)
			{
				try { return !System.Diagnostics.Process.GetProcessById(pid).HasExited; }
				catch (ArgumentException) { return false; }
			}

			for (var i = 0; i < 100 && started.Any(Alive); i++) System.Threading.Thread.Sleep(100);
		}

		Assert.That(LocalStackRunStateIO.Load(runStatePath), Is.Null);
	}

	/// <summary>
	/// Builds in different folders run at the same time: the first one waits for a file that only the second one
	/// writes, so one after the other it would give up and fail the run.
	/// </summary>
	[Test]
	public void Builds_in_different_folders_run_at_the_same_time()
	{
		if (!OperatingSystem.IsWindows()) Assert.Ignore("The build steps are PowerShell.");

		SetupAndLoad();
		var first = Path.Combine(WorkingDir, "repo-one");
		var second = Path.Combine(WorkingDir, "repo-two");
		Directory.CreateDirectory(first);
		Directory.CreateDirectory(second);
		var marker = Path.Combine(WorkingDir, "second-built.txt");

		Run("local", "setup", "--quiet", "--add-step", "build: one", "--step-once", "--step-dir", first, "--step-shell",
			$"$n = 0; while (-not (Test-Path '{marker}') -and $n -lt 150) {{ Start-Sleep -Milliseconds 100; $n++ }}; if (-not (Test-Path '{marker}')) {{ exit 3 }}");
		Run("local", "setup", "--quiet", "--add-step", "build: two", "--step-once", "--step-dir", second, "--step-shell",
			$"Set-Content -Path '{marker}' -Value built");
		var config = LocalStackConfigIO.Load(SettingsPath);
		foreach (var step in config.steps.Where(s => s.name is "build: one" or "build: two"))
			step.build = true;
		LocalStackConfigIO.Save(SettingsPath, config);
		Run("local", "create", "builds", "--quiet", "--include", "build: one,build: two");

		try
		{
			Assert.That(RunFull(new[] { "local", "run", "builds:build", "--detach", "--no-create-realm", "--quiet" }), Is.EqualTo(0));
			Assert.That(File.Exists(marker), Is.True);
			Assert.That(BoardState("build: one"), Is.EqualTo(LocalStackStepState.Done), "it saw the file the other build wrote");
		}
		finally
		{
			RunFull(new[] { "local", "stop", "--quiet" });
		}
	}

	private LocalStackStepState? BoardState(string step) =>
		LocalStackBoardIO.Load(LocalStackBoardIO.PathFor(LocalStackRunStateIO.ResolveRunStatePath(SettingsPath)))?.rows
			.FirstOrDefault(r => r.name == step)?.state;

	/// <summary>
	/// A step that fails doesn't stop the stack: it is marked failed, a step that needs it can't start, and the
	/// others start.
	/// </summary>
	[Test]
	public void A_failed_step_is_marked_and_the_rest_start()
	{
		if (!OperatingSystem.IsWindows()) Assert.Ignore("The steps are PowerShell.");

		SetupAndLoad();
		Run("local", "setup", "--quiet", "--add-step", "seed", "--step-once", "--step-shell", "exit 3");
		Run("local", "setup", "--quiet", "--add-step", "needs seed", "--step-shell", "Start-Sleep -Seconds 600");
		Run("local", "setup", "--quiet", "--add-step", "sleep: a", "--step-shell", "Start-Sleep -Seconds 600");
		var config = LocalStackConfigIO.Load(SettingsPath);
		config.steps.First(s => s.name == "needs seed").dependsOn = new List<string> { "seed" };
		LocalStackConfigIO.Save(SettingsPath, config);
		Run("local", "create", "mixed", "--quiet", "--include", "seed,needs seed,sleep: a");

		var runStatePath = LocalStackRunStateIO.ResolveRunStatePath(SettingsPath);
		try
		{
			Assert.That(RunFull(new[] { "local", "run", "mixed", "--detach", "--no-create-realm", "--quiet" }), Is.EqualTo(0));
			Assert.That(BoardState("seed"), Is.EqualTo(LocalStackStepState.Failed));
			Assert.That(BoardState("needs seed"), Is.EqualTo(LocalStackStepState.Failed), "it can't start without the seed");
			Assert.That(LocalStackRunStateIO.Load(runStatePath)?.steps.FirstOrDefault(e => e.name == "sleep: a")?.pid ?? 0, Is.GreaterThan(0), "the rest started");
		}
		finally
		{
			var started = LocalStackRunStateIO.Load(runStatePath)?.steps.Select(e => e.pid).Where(p => p > 0).ToList() ?? new List<int>();
			RunFull(new[] { "local", "stop", "--quiet" });
			bool Alive(int pid)
			{
				try { return !System.Diagnostics.Process.GetProcessById(pid).HasExited; }
				catch (ArgumentException) { return false; }
			}

			for (var i = 0; i < 100 && started.Any(Alive); i++) System.Threading.Thread.Sleep(100);
		}
	}

	/// <summary>Adds a custom step and marks it a build, which <c>--add-step</c> can't.</summary>
	private void AddBuildStep(string name, string dir, string script, int timeout = 120, List<string> inputs = null)
	{
		Run("local", "setup", "--quiet", "--add-step", name, "--step-once", "--step-dir", dir, "--step-timeout", timeout.ToString(),
			"--step-shell", script);
		var config = LocalStackConfigIO.Load(SettingsPath);
		var step = config.steps.First(s => s.name == name);
		step.build = true;
		step.inputs = inputs;
		LocalStackConfigIO.Save(SettingsPath, config);
	}

	private static void Git(string dir, params string[] args)
	{
		var psi = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (var arg in args) psi.ArgumentList.Add(arg);
		using var git = System.Diagnostics.Process.Start(psi)!;
		git.WaitForExit();
		Assert.That(git.ExitCode, Is.EqualTo(0), $"git {string.Join(" ", args)}: {git.StandardError.ReadToEnd()}");
	}

	/// <summary>
	/// A build the stack names outright builds the first time, is up to date while its sources stay the same, and
	/// builds again once they change.
	/// </summary>
	[Test]
	public void A_named_build_is_skipped_while_its_sources_are_unchanged()
	{
		if (!OperatingSystem.IsWindows()) Assert.Ignore("The build step is PowerShell.");

		SetupAndLoad();
		var repo = Path.Combine(WorkingDir, "sources");
		Directory.CreateDirectory(repo);
		Git(repo, "init", "-q");
		File.WriteAllText(Path.Combine(repo, "a.txt"), "one");
		Git(repo, "add", ".");
		Git(repo, "-c", "user.email=t@t", "-c", "user.name=t", "commit", "-q", "-m", "one");

		var count = Path.Combine(WorkingDir, "builds.txt");
		AddBuildStep("build: sources", repo, $"Add-Content -Path '{count}' -Value built", inputs: new List<string> { "." });
		Run("local", "create", "src", "--quiet", "--include", "build: sources");
		int Builds() => File.Exists(count) ? File.ReadAllLines(count).Length : 0;

		try
		{
			Run("local", "run", "src", "--detach", "--no-create-realm", "--quiet");
			Assert.That(Builds(), Is.EqualTo(1), "never built by beam local run");
			RunFull(new[] { "local", "stop", "--quiet" });

			Run("local", "run", "src", "--detach", "--no-create-realm", "--quiet");
			Assert.That(Builds(), Is.EqualTo(1), "unchanged: up to date");
			RunFull(new[] { "local", "stop", "--quiet" });

			File.WriteAllText(Path.Combine(repo, "a.txt"), "two");
			Run("local", "run", "src", "--detach", "--no-create-realm", "--quiet");
			Assert.That(Builds(), Is.EqualTo(2), "changed: builds");
		}
		finally
		{
			RunFull(new[] { "local", "stop", "--quiet" });
		}
	}

	/// <summary>A build that runs out of time is stopped and marked failed, instead of running on unwatched; the run goes on.</summary>
	[Test]
	public void A_build_that_runs_out_of_time_is_stopped()
	{
		if (!OperatingSystem.IsWindows()) Assert.Ignore("The build step is PowerShell.");

		SetupAndLoad();
		var dir = Path.Combine(WorkingDir, "slow");
		Directory.CreateDirectory(dir);
		var pidFile = Path.Combine(WorkingDir, "slow.pid");
		AddBuildStep("build: slow", dir, $"Set-Content -Path '{pidFile}' -Value $PID; Start-Sleep -Seconds 120", timeout: 3);
		Run("local", "create", "slow", "--quiet", "--include", "build: slow");

		try
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			Assert.That(RunFull(new[] { "local", "run", "slow:build", "--detach", "--no-create-realm", "--quiet" }), Is.EqualTo(0), "a failed step doesn't fail the run");
			Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(60)));
			Assert.That(BoardState("build: slow"), Is.EqualTo(LocalStackStepState.Failed));

			Assert.That(File.Exists(pidFile), Is.True);
			var pid = int.Parse(File.ReadAllText(pidFile).Trim());
			bool Alive()
			{
				try { return !System.Diagnostics.Process.GetProcessById(pid).HasExited; }
				catch (ArgumentException) { return false; }
			}

			for (var i = 0; i < 50 && Alive(); i++) System.Threading.Thread.Sleep(100);
			Assert.That(Alive(), Is.False, "the build's process was stopped");
		}
		finally
		{
			RunFull(new[] { "local", "stop", "--quiet" });
		}
	}

	/// <summary>Create adds what the picked steps need, and says so.</summary>
	[Test]
	public void Create_adds_what_the_picked_steps_need()
	{
		SetupAndLoad();
		Run("local", "create", "gw", "--quiet", "--include", "c# gateway");

		var written = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "gw.json")))["gw"]!;
		var include = written["include"]!.Select(t => (string)t).ToList();
		Assert.That(include, Is.SupersetOf(new[] { "c# gateway", LocalStackDependencies.ApiDepsStep, "scala: auth", "scala: content" }),
			"the docker deps, the Scala login set and the Scala services the gateway calls");
		Assert.That(include, Has.None.StartsWith("build:"), "its build isn't named: it comes with it");
		var steps = LocalStackStacks.Load(WorkingDir).Resolve("gw", LocalStackConfigIO.Load(SettingsPath)).steps.Select(s => s.name);
		Assert.That(steps, Does.Contain("build: c# gateway"));
	}

	/// <summary>
	/// A stack can run this workspace's microservices as steps of its own, narrowed to them, with the backend they
	/// log into added; and have commands of its own, placed where they say.
	/// </summary>
	[Test]
	public void Create_runs_services_and_own_commands()
	{
		SetupAndLoad();
		Run("local", "create", "svc", "--quiet", "--services", "Stats");
		var svc = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "svc.json")))["svc"]!;
		Assert.That(svc["steps"]![LocalStackTemplate.MicroservicePrefix + "Stats"]!["arguments"]!.ToString(), Does.Contain("--ids Stats"));
		Assert.That(svc["extensions"]!.Select(t => (string)t), Is.EqualTo(new[] { "Stats" }));
		Assert.That(svc["include"]!.Select(t => (string)t), Does.Contain(LocalStackTemplate.MicroservicePrefix + "Stats").And.Contain("c# gateway"));

		Run("local", "create", "own", "--quiet", "--extends", "backend", "--add-step", "seed", "--step-shell", "echo seeded", "--step-once", "--step-first");
		Run("local", "edit", "own", "--quiet", "--add-step", "watch", "--step-command", "node", "--step-args", "watch.js");

		var stacks = LocalStackStacks.Load(WorkingDir);
		var steps = stacks.Resolve("own", LocalStackConfigIO.Load(SettingsPath)).steps.Select(s => s.name).ToList();
		Assert.That(steps.First(), Is.EqualTo("seed"));
		Assert.That(steps.Last(), Is.EqualTo("watch"));
		var own = JObject.Parse(File.ReadAllText(Path.Combine(LocalStacksDir, "own.json")))["own"]!;
		Assert.That((bool)own["steps"]!["seed"]!["custom"]!, Is.True);
		Assert.That(RunFull(new[] { "local", "edit", "own", "--quiet", "--add-step", "seed", "--step-command", "node" }), Is.Not.EqualTo(0), "names are unique");
	}
}
