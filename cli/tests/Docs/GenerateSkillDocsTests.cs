using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using tests.Examples;

namespace tests.Docs;

/// <summary>
/// Renders the real skill templates in cli/cli/Docs/SkillTemplates with `generate-skill-docs`, so a broken
/// template, a dangling skill reference or a regression in the facts agents rely on fails a test instead of
/// shipping in the embedded skills.
/// </summary>
public class GenerateSkillDocsTests : CLITest
{
	/// <summary>Walks up from the test binaries to the checked-in skill templates.</summary>
	public static string FindSkillTemplatesDir()
	{
		var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
		while (dir != null)
		{
			var candidate = Path.Combine(dir.FullName, "cli", "cli", "Docs", "SkillTemplates");
			if (Directory.Exists(candidate)) return candidate;
			dir = dir.Parent;
		}

		Assert.Fail("Could not find cli/cli/Docs/SkillTemplates above " + TestContext.CurrentContext.TestDirectory);
		return null!;
	}

	private Dictionary<string, string> RenderAll()
	{
		var docsDir = Path.Combine(WorkingDir, "Docs");
		var templatesDir = Path.Combine(docsDir, "SkillTemplates");
		Directory.CreateDirectory(templatesDir);
		foreach (var file in Directory.GetFiles(FindSkillTemplatesDir(), "*.md.scriban"))
			File.Copy(file, Path.Combine(templatesDir, Path.GetFileName(file)));

		Run("generate-skill-docs", "--template-dir", docsDir, "-q");

		return Directory.GetFiles(Path.Combine(docsDir, "Skills"), "*.md")
			.ToDictionary(f => Path.GetFileNameWithoutExtension(f), File.ReadAllText);
	}

	[Test]
	public void EveryTemplateRenders_WithMatchingFrontMatter()
	{
		var templates = Directory.GetFiles(FindSkillTemplatesDir(), "*.md.scriban")
			.Select(f => Path.GetFileName(f).Replace(".md.scriban", ""))
			.ToList();
		var skills = RenderAll();

		CollectionAssert.AreEquivalent(templates, skills.Keys, "every template must render to a skill");
		foreach (var (name, content) in skills)
		{
			Assert.That(content, Does.StartWith("---"), $"{name} is missing front matter");
			Assert.That(content, Does.Contain($"name: {name}\n").Or.Contain($"name: {name}\r\n"), $"{name} front matter name must match the file name");
			Assert.That(content, Does.Match(@"(?m)^description: \S"), $"{name} needs a description (it is the beam_get_skill summary)");
			// ${{ ... }} is a GitHub Actions expression inside a workflow example, not Scriban
			Assert.That(content, Does.Not.Match(@"(?<!\$)\{\{"), $"{name} has unrendered Scriban");
		}
	}

	[Test]
	public void ReferencedSkillsExist()
	{
		var skills = RenderAll();
		// beam_get_skill("x") calls and the "Related Skills" lists must point at skills that ship
		var reference = new Regex(@"beam_get_skill\(""(?<name>beam-[a-z0-9-]+)""\)|^- `(?<name>beam-[a-z0-9-]+)`", RegexOptions.Multiline);
		foreach (var (name, content) in skills)
		{
			foreach (Match match in reference.Matches(content))
			{
				var referenced = match.Groups["name"].Value;
				Assert.That(skills.ContainsKey(referenced), $"{name} references missing skill '{referenced}'");
			}
		}
	}

	[Test]
	public void WebGuide_HasCorrectPackageAndRealmPrerequisites()
	{
		var guide = RenderAll()["beam-web-guide"];

		Assert.That(guide, Does.Contain("npm install @beamable/sdk"));
		Assert.That(guide, Does.Contain("https://unpkg.com/@beamable/sdk"));
		Assert.That(guide, Does.Not.Match(@"(install|add) beamable-sdk|from ""beamable-sdk|require\('beamable-sdk"), "the stale unscoped package name must not be recommended");

		Assert.That(guide, Does.Contain("notification|publisher::beamable"));
		Assert.That(guide, Does.Contain("content publish"));
		Assert.That(guide, Does.Contain("id=global"));

		Assert.That(guide, Does.Contain("project generate web-client --output-dir"));
		Assert.That(guide, Does.Contain("`--output-dir` |"), "the generator options table must render from the live command");
		Assert.That(guide, Does.Contain("beam.use(MatchServiceClient)"));
		Assert.That(guide, Does.Not.Contain("endpoint: \"/"), "endpoints have no leading slash; the SDK adds it");

		Assert.That(guide, Does.Contain("session-start"));
		Assert.That(guide, Does.Contain("connectTimeoutMs"));
		Assert.That(guide, Does.Contain("beam-test-and-verify"));
	}

	[Test]
	public void WebSkills_CoverTheEndToEndPath()
	{
		var skills = RenderAll();

		var quickstart = skills["beam-web-game-quickstart"];
		foreach (var step in new[] { "init --cid", "mcp setup", "config realm check --for web --fix", "project new storage", "--link-to",
			         "project build", "project generate web-client", "Beam.init", "beam.use(MatchServiceClient)", "deploy release --merge --wait", "beam-web-hosting" })
			Assert.That(quickstart, Does.Contain(step), $"quickstart is missing '{step}'");

		var verify = skills["beam-test-and-verify"];
		foreach (var step in new[] { "project call", "listen player --probe", "project remote-logs", "project open-mongo", "project storage snapshot", "session-start", "Never stub" })
			Assert.That(verify, Does.Contain(step), $"test-and-verify is missing '{step}'");

		var hosting = skills["beam-web-hosting"];
		foreach (var step in new[] { "base: \"./\"", "actions/upload-pages-artifact", "actions/deploy-pages", "Source: GitHub Actions", "realm secret", "${{ steps.deployment.outputs.page_url }}" })
			Assert.That(hosting, Does.Contain(step), $"web-hosting is missing '{step}'");
	}
}
