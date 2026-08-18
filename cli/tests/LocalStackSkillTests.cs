using System.Collections.Generic;
using System.IO;
using System.Linq;
using cli.Services.LocalStack;
using Newtonsoft.Json;
using NUnit.Framework;

namespace tests;

/// <summary>
/// `beam local init` writes a <c>beam-local-stack</c> agent skill next to the workspace so the manifest's
/// ~40 opaque step names, and the five repository checkouts they point at, are documented where anyone
/// working in the workspace will find them. The generated half is rendered from the SAVED manifest (not the
/// init prompts) so it stays correct for <c>--update-services</c>, and it is overwritten on every init so it
/// can never describe a stack you no longer have.
/// </summary>
public class LocalStackSkillTests
{
	// Built with Path.Combine rather than literals: WebProductDir()/ResolveSkillPath() split these with
	// Path.GetDirectoryName, which only understands the CURRENT platform's separator — a hard-coded
	// Windows path would silently collapse to "" (and then to an <EDIT: ...> placeholder) on macOS/Linux.
	private static readonly string Root = Path.Combine(Path.GetPathRoot(Path.GetTempPath()) ?? "/", "repos");
	private static readonly string ApiDir = Path.Combine(Root, "BeamableAPI");
	private static readonly string ScalaDir = Path.Combine(Root, "BeamableBackend");
	private static readonly string PortalDir = Path.Combine(Root, "agentic-portal");
	private static readonly string ProductDir = Path.Combine(Root, "BeamableProduct");
	private static readonly string WebRegistryDir = Path.Combine(ProductDir, "portal-localdev");
	private static readonly string ManifestPath =
		Path.Combine(Root, "game", ".beamable", LocalStackConfigIO.DefaultFileName);

	private static LocalStackConfig Config(bool includeWebRegistry = true, bool withRepoPaths = true) =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = withRepoPaths ? ApiDir : null,
			scalaDir = withRepoPaths ? ScalaDir : null,
			portalDir = withRepoPaths ? PortalDir : null,
			includeWebRegistry = includeWebRegistry,
			webRegistryDir = withRepoPaths ? WebRegistryDir : null,
			services = new List<string> { "MyService" },
			extensions = new List<string> { "MyExtension" },
			groups = new List<string> { "MyGroup" },
		});

	/// <summary>
	/// The whole point of the generated section: the reader learns which checkouts their stack spans without
	/// opening the CLI source.
	/// </summary>
	[Test]
	public void Generated_section_names_every_repository()
	{
		var section = LocalStackSkillTemplate.RenderThisStack(Config(), ManifestPath);

		Assert.That(section, Does.Contain(ApiDir));
		Assert.That(section, Does.Contain(ScalaDir));
		Assert.That(section, Does.Contain(PortalDir));
		Assert.That(section, Does.Contain(WebRegistryDir));
		// Derived from the portal-localdev path rather than prompted for.
		Assert.That(section, Does.Contain(ProductDir));
		Assert.That(section, Does.Contain(ManifestPath));
	}

	[Test]
	public void Generated_section_names_the_endpoints_and_the_selected_services()
	{
		var config = Config();
		var section = LocalStackSkillTemplate.RenderThisStack(config, ManifestPath);

		Assert.That(section, Does.Contain(config.host));
		Assert.That(section, Does.Contain(config.portalUrl));
		Assert.That(section, Does.Contain("MyService"));
		Assert.That(section, Does.Contain("MyExtension"));
		Assert.That(section, Does.Contain("MyGroup"));
		// The curated Scala set is listed by id, so `--only`/`--skip` can be used against it.
		Assert.That(section, Does.Contain(LocalStackTemplate.ScalaDbidService));
	}

	/// <summary>
	/// `scala: redis` shares the prefix of the Scala service steps but is a docker container they depend on,
	/// so counting it as a service would overstate the JVM count by one and invite someone to go looking for
	/// a `tools/redis` folder.
	/// </summary>
	[Test]
	public void Redis_is_not_counted_as_a_scala_service()
	{
		var section = LocalStackSkillTemplate.RenderThisStack(Config(), ManifestPath);
		var line = section.Split('\n').First(l => l.Contains("**Scala services ("));

		Assert.That(line, Does.Not.Contain("`redis`"));
		Assert.That(line, Does.Contain($"({LocalStackTemplate.DefaultScalaServices.Length})"));
		// It is still in the step table — it has to be, `--only`/`--skip` take that name.
		Assert.That(section, Does.Contain("scala: redis"));
	}

	/// <summary>
	/// Every step name appears verbatim — they are the exact strings `up --only`, `logs` and `stop` take, so a
	/// paraphrased list would be useless.
	/// </summary>
	[Test]
	public void Generated_section_lists_every_step_name_verbatim()
	{
		var config = Config();
		var section = LocalStackSkillTemplate.RenderThisStack(config, ManifestPath);

		foreach (var step in config.steps)
		{
			Assert.That(section, Does.Contain(step.name), $"step '{step.name}' is missing from the skill");
		}
	}

	/// <summary>
	/// Omitting the web registry is the one choice that silently changes which code the portal runs, so the
	/// doc has to say which way this manifest went.
	/// </summary>
	[Test]
	public void Web_registry_presence_is_reported_either_way()
	{
		Assert.That(LocalStackSkillTemplate.RenderThisStack(Config(includeWebRegistry: true), ManifestPath),
			Does.Contain(LocalStackTemplate.WebRegistryStepName));

		var without = LocalStackSkillTemplate.RenderThisStack(Config(includeWebRegistry: false), ManifestPath);
		Assert.That(without, Does.Not.Contain(LocalStackTemplate.WebRegistryStepName));
		Assert.That(without, Does.Contain("NOT included"));
	}

	/// <summary>
	/// A manifest with unfilled repo paths is not runnable at all, so that has to be the loudest thing in the
	/// generated section rather than something the reader infers from an odd-looking path.
	/// </summary>
	[Test]
	public void Unresolved_repo_paths_are_called_out_as_not_runnable()
	{
		var section = LocalStackSkillTemplate.RenderThisStack(Config(withRepoPaths: false), ManifestPath);

		Assert.That(section, Does.Contain("Not runnable yet"));
		Assert.That(section, Does.Contain(LocalStackConfigIO.EditPlaceholder));
		Assert.That(section, Does.Contain("BeamableAPI"));
	}

	[Test]
	public void Fully_resolved_manifest_has_no_not_runnable_warning()
	{
		Assert.That(LocalStackSkillTemplate.RenderThisStack(Config(), ManifestPath),
			Does.Not.Contain("Not runnable yet"));
	}

	/// <summary>
	/// `repos` was added after the first manifests were written, and it is documentation-only — an older
	/// manifest must still render (pointing the reader at the step list) rather than throw mid-init.
	/// </summary>
	[Test]
	public void Manifest_without_recorded_repos_still_renders()
	{
		var json = JsonConvert.SerializeObject(Config());
		var config = JsonConvert.DeserializeObject<LocalStackConfig>(json);
		config.repos = null;

		var section = LocalStackSkillTemplate.RenderThisStack(config, ManifestPath);

		Assert.That(section, Does.Contain("predates the recorded repository paths"));
		// The step list is the fallback source of the paths, so it must still be there.
		Assert.That(section, Does.Contain(LocalStackTemplate.WebRegistryStepName));
	}

	[Test]
	public void Empty_config_renders_without_throwing()
	{
		Assert.That(LocalStackSkillTemplate.RenderThisStack(new LocalStackConfig(), ManifestPath),
			Does.Contain("This workspace's stack"));
	}

	/// <summary>The prose half is an embedded resource; a broken csproj glob or a renamed file would silently
	/// turn the skill into an empty stub, so assert the frontmatter an agent needs to discover it.</summary>
	[Test]
	public void Render_splices_the_generated_section_into_the_embedded_template()
	{
		var content = LocalStackSkillTemplate.Render(Config(), ManifestPath);

		Assert.That(content, Is.Not.Null, "the embedded skill template was not found");
		Assert.That(content, Does.StartWith("---"));
		Assert.That(content, Does.Contain($"name: {LocalStackSkillTemplate.SkillName}"));
		Assert.That(content, Does.Contain("description:"));
		Assert.That(content, Does.Not.Contain(LocalStackSkillTemplate.ThisStackToken));
		Assert.That(content, Does.Contain("This workspace's stack"));
		Assert.That(content, Does.Contain(ApiDir));
	}

	[Test]
	public void Skill_path_is_under_the_manifest_parent_when_there_is_no_workspace()
	{
		// No ConfigService: `beam local init` is IStandaloneCommand, so it can run outside a workspace and the
		// skill still has to land next to the stack it documents.
		var path = LocalStackSkillTemplate.ResolveSkillPath(null, ManifestPath);
		var expected = Path.Combine(
			Path.GetDirectoryName(Path.GetFullPath(ManifestPath)),
			LocalStackSkillTemplate.AgentDirName,
			LocalStackSkillTemplate.SkillsDirName,
			LocalStackSkillTemplate.SkillName,
			LocalStackSkillTemplate.SkillFileName);

		Assert.That(path, Is.EqualTo(expected));
		Assert.That(Path.GetFileName(path), Is.EqualTo("SKILL.md"));
	}

	/// <summary>Regenerated on every init, so the write must not defer to an existing file the way the
	/// manifest does — no `--force`, no prompt.</summary>
	[Test]
	public void Write_overwrites_an_existing_skill_file()
	{
		var root = Path.Combine(Path.GetTempPath(), "beam-skill-test-" + Path.GetRandomFileName());
		try
		{
			var manifestPath = Path.Combine(root, ".beamable", LocalStackConfigIO.DefaultFileName);
			var skillPath = LocalStackSkillTemplate.ResolveSkillPath(null, manifestPath);
			Directory.CreateDirectory(Path.GetDirectoryName(skillPath));
			// A marker that cannot occur in the template's own prose, so this can never pass or fail on wording.
			const string marker = "PREVIOUS-CONTENTS-8f2a1c";
			File.WriteAllText(skillPath, marker);

			var written = LocalStackSkillTemplate.Write(null, manifestPath, Config());

			Assert.That(written, Is.EqualTo(skillPath));
			var content = File.ReadAllText(skillPath);
			Assert.That(content, Does.Not.Contain(marker));
			Assert.That(content, Does.Contain(ApiDir));
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}

	/// <summary>The recorded repo paths are documentation-only metadata; they must match what the steps
	/// actually got, or the doc would describe a different stack than the one that runs.</summary>
	[Test]
	public void Recorded_repos_match_the_step_working_directories()
	{
		var config = Config();

		Assert.That(config.repos, Is.Not.Null);
		Assert.That(config.repos.apiDir, Is.EqualTo(ApiDir));
		Assert.That(config.repos.scalaDir, Is.EqualTo(ScalaDir));
		Assert.That(config.repos.portalDir, Is.EqualTo(PortalDir));
		Assert.That(config.repos.webRegistryDir, Is.EqualTo(WebRegistryDir));
		Assert.That(config.repos.productDir, Is.EqualTo(ProductDir));

		var registryStep = config.steps.First(s => s.name == LocalStackTemplate.WebRegistryStepName);
		Assert.That(registryStep.workingDirectory, Is.EqualTo(config.repos.webRegistryDir));
	}

	[Test]
	public void Repos_omits_the_web_paths_when_the_registry_is_not_included()
	{
		var config = Config(includeWebRegistry: false);

		Assert.That(config.repos.webRegistryDir, Is.Null);
		Assert.That(config.repos.productDir, Is.Null);
		Assert.That(LocalStackSkillTemplate.RenderThisStack(config, ManifestPath),
			Does.Contain("not part of this stack"));
	}
}
