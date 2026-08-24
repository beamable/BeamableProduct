using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;
using System.IO;
using System.Linq;
using tests.Examples;

namespace tests;

/// <summary>
/// The web-registry choice is made once, at <c>beam local init</c>, and recorded in the manifest so it lasts
/// until the next <c>init</c> — before this, the only way to skip the registry was to remember
/// a flag on every single <c>beam local up</c>.
///
/// Two things have to hold for that to work, and both are easy to break by accident: the choice must be
/// PERSISTED (a dropped `false` reads back as "unrecorded", which means on), and the three steps must be
/// written EITHER WAY, so flipping the choice never requires regenerating the manifest by hand.
/// </summary>
public class LocalStackInitWebRegistryTests : CLITest
{
	private static readonly string[] WebSteps =
	{
		LocalStackTemplate.WebRegistryStepName,
		LocalStackTemplate.WebPublishStepName,
		LocalStackTemplate.WebRefreshStepName,
	};

	private LocalStackConfig InitAndLoad(params string[] extraArgs)
	{
		// --quiet: the test console reports itself as interactive, so without it `init` would try to prompt.
		Run(new[] { "local", "init", "--quiet", "--force" }.Concat(extraArgs).ToArray());

		var path = Path.Combine(WorkingDir, ".beamable", LocalStackConfigIO.DefaultFileName);
		Assert.That(File.Exists(path), Is.True, $"init wrote no manifest at {path}");
		return LocalStackConfigIO.Load(path);
	}

	private static void AssertWebStepsWritten(LocalStackConfig config)
	{
		var names = config.steps.Select(s => s.name).ToList();
		foreach (var step in WebSteps)
		{
			Assert.That(names, Does.Contain(step),
				$"{step} must be written whatever the choice, so it can be turned on without a re-init");
		}
	}

	/// <summary>
	/// The default is NO: the registry is only useful while iterating on the web SDK, and its steps are a slow
	/// pnpm rebuild plus a reinstall per extension.
	/// </summary>
	[Test]
	public void A_plain_init_records_the_registry_as_off_but_still_writes_its_steps()
	{
		var config = InitAndLoad();

		Assert.That(config.webRegistry, Is.False);
		AssertWebStepsWritten(config);

		// And `up` with no flags honours it — this is the whole point of the field.
		Assert.That(LocalStackUpCommand.ResolveNoWebRegistry(config, null), Is.True);
	}

	[Test]
	public void With_web_registry_records_the_registry_as_on()
	{
		var config = InitAndLoad("--with-web-registry");

		Assert.That(config.webRegistry, Is.True);
		AssertWebStepsWritten(config);
		Assert.That(LocalStackUpCommand.ResolveNoWebRegistry(config, null), Is.False);
	}

	[Test]
	public void With_web_registry_false_records_the_registry_as_off()
	{
		var config = InitAndLoad("--with-web-registry=false");

		Assert.That(config.webRegistry, Is.False);
		AssertWebStepsWritten(config);
	}

	/// <summary>Naming the directory only makes sense if you want the registry, so it answers the question.</summary>
	[Test]
	public void Web_registry_dir_implies_the_registry_is_on()
	{
		var dir = Path.Combine(WorkingDir, "portal-localdev");
		Directory.CreateDirectory(dir);

		var config = InitAndLoad("--web-registry-dir", dir);

		Assert.That(config.webRegistry, Is.True);
		Assert.That(config.repos.webRegistryDir, Is.EqualTo(dir));
	}

	/// <summary>
	/// The single flag takes an optional VALUE, which is what lets it express BOTH answers — that is the whole
	/// reason there is no second `--no-...` flag. The binder receives default(bool) for an absent flag, so
	/// honouring an explicit `false` means asking the parse result whether the token was really there
	/// (LocalStackCommand.WasSupplied); getting that wrong would silently ignore every `=false` and leave the
	/// command with no way to say "no" at all.
	/// </summary>
	[TestCase("--with-web-registry", true)]
	[TestCase("--with-web-registry=true", true)]
	[TestCase("--with-web-registry=false", false)]
	public void The_flag_accepts_an_explicit_bool_value(string flag, bool expected)
	{
		Assert.That(InitAndLoad(flag).webRegistry, Is.EqualTo(expected));
	}

	/// <summary>The space-separated form has to work too, since that is how people type flags.</summary>
	[Test]
	public void The_value_can_be_space_separated()
	{
		Assert.That(InitAndLoad("--with-web-registry", "true").webRegistry, Is.True);
		Assert.That(InitAndLoad("--with-web-registry", "false").webRegistry, Is.False);
	}

	/// <summary>
	/// Re-running `init` is how the standing choice is changed, so the second run has to actually overwrite it.
	/// </summary>
	[Test]
	public void Re_running_init_flips_the_standing_choice()
	{
		Assert.That(InitAndLoad("--with-web-registry").webRegistry, Is.True);
		Assert.That(InitAndLoad("--with-web-registry=false").webRegistry, Is.False);
		Assert.That(InitAndLoad("--with-web-registry").webRegistry, Is.True);
	}

	/// <summary>
	/// `--update-services` rewrites only the microservice/extension/group steps. The web-registry choice is
	/// not part of that, and silently resetting it would be exactly the "flag I have to keep re-passing"
	/// problem this feature removes.
	/// </summary>
	[Test]
	public void Update_services_leaves_the_standing_choice_alone()
	{
		Assert.That(InitAndLoad("--with-web-registry").webRegistry, Is.True);

		Run("local", "init", "--quiet", "--update-services");

		var path = Path.Combine(WorkingDir, ".beamable", LocalStackConfigIO.DefaultFileName);
		Assert.That(LocalStackConfigIO.Load(path).webRegistry, Is.True);
	}
}
