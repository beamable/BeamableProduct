using System.Text;

namespace cli.Services.LocalStack;

/// <summary>
/// Builds the <c>beam-local-stack</c> agent skill that <c>beam local init</c> writes alongside the manifest:
/// a hand-written prose guide (embedded from <c>Docs/LocalStack/beam-local-stack.md</c>) with a generated
/// section describing the manifest that was just written — the repositories it spans, the endpoints, the
/// services selected, and anything still left to fill in.
///
/// <para>
/// The generated section is derived from the saved <see cref="LocalStackConfig"/>, never from the
/// <c>init</c> prompts. That is what makes it correct for <c>--update-services</c>, which loads an existing
/// manifest and never sees a repo path or endpoint, and it guarantees the doc describes what is actually in
/// the JSON rather than what was asked for.
/// </para>
/// </summary>
public static class LocalStackSkillTemplate
{
	/// <summary>Skill directory name; also the <c>name:</c> in the template's YAML frontmatter.</summary>
	public const string SkillName = "beam-local-stack";

	/// <summary>
	/// Skill entry-point filename. All caps, which is what agents discover on case-sensitive filesystems.
	/// (<c>beam install-ai-skill</c> writes <c>Skill.md</c> for its claude target — a separate, older path;
	/// don't copy it.)
	/// </summary>
	public const string SkillFileName = "SKILL.md";

	/// <summary>The agent directory the skill is installed under, relative to the workspace root.</summary>
	public const string AgentDirName = ".claude";

	/// <summary>The skills subdirectory inside <see cref="AgentDirName"/>.</summary>
	public const string SkillsDirName = "skills";

	/// <summary>Placeholder in the embedded template that <see cref="RenderThisStack"/> replaces.</summary>
	public const string ThisStackToken = "{{THIS_STACK}}";

	/// <summary>
	/// The prose half of the skill, embedded by the <c>Docs\**\*.md</c> glob in <c>cli.csproj</c>.
	/// Deliberately NOT under <c>Docs/Skills/</c>: that folder is regenerated from Scriban templates by the
	/// <c>RegenerateSkillDocs</c> build target, and everything in it is installed into customer workspaces by
	/// <c>beam install-ai-skill</c> — neither of which should apply to an internal local-stack guide.
	/// </summary>
	private const string EmbeddedResourceName = "cli.Docs.LocalStack.beam-local-stack.md";

	/// <summary>Step-name prefix of the Scala service steps (the counterpart of the prefixes on
	/// <see cref="LocalStackTemplate.MicroservicePrefix"/> and friends, which that type owns).</summary>
	private const string ScalaPrefix = "scala: ";

	/// <summary>
	/// Where the skill is written: <c>&lt;workspace&gt;/.claude/skills/beam-local-stack/SKILL.md</c>. The
	/// workspace root is the folder holding <c>.beamable</c>; <c>beam local init</c> is an
	/// <c>IStandaloneCommand</c>, so when it runs outside a workspace the manifest's own directory is used
	/// instead — the skill always lands next to the stack it documents.
	/// </summary>
	public static string ResolveSkillPath(ConfigService configService, string manifestPath)
	{
		var root = configService?.DirectoryExists == true && !string.IsNullOrEmpty(configService.BeamableWorkspace)
			? configService.BeamableWorkspace
			: Path.GetDirectoryName(Path.GetFullPath(manifestPath));

		if (string.IsNullOrEmpty(root))
		{
			root = Directory.GetCurrentDirectory();
		}

		return Path.Combine(root, AgentDirName, SkillsDirName, SkillName, SkillFileName);
	}

	/// <summary>
	/// The full skill file: the embedded prose with <see cref="ThisStackToken"/> replaced by the generated
	/// section. Returns null when the embedded template cannot be found, so callers can skip writing rather
	/// than produce a doc with nothing in it.
	/// </summary>
	public static string Render(LocalStackConfig config, string manifestPath)
	{
		var template = ReadEmbeddedTemplate();
		if (template == null)
		{
			return null;
		}

		return template.Replace(ThisStackToken, RenderThisStack(config, manifestPath));
	}

	/// <summary>
	/// Writes the skill, overwriting any existing file unconditionally — it is generated, so a stale copy is
	/// worse than no copy. Returns the path written, or null when the embedded template is missing.
	/// </summary>
	public static string Write(ConfigService configService, string manifestPath, LocalStackConfig config)
	{
		var content = Render(config, manifestPath);
		if (content == null)
		{
			return null;
		}

		var path = ResolveSkillPath(configService, manifestPath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	/// <summary>
	/// The generated "This workspace's stack" section. Pure — no IO, no prompts, everything read off
	/// <paramref name="config"/> — so it is directly unit-testable.
	/// </summary>
	public static string RenderThisStack(LocalStackConfig config, string manifestPath)
	{
		config ??= new LocalStackConfig();
		var steps = config.steps ?? new List<LocalStackStep>();
		var sb = new StringBuilder();

		sb.AppendLine("## This workspace's stack");
		sb.AppendLine();
		sb.AppendLine("Generated from the manifest below. Everything in this section is specific to this machine.");
		sb.AppendLine();
		sb.AppendLine($"- **Manifest**: `{Display(manifestPath)}`");
		sb.AppendLine($"- **Steps**: {steps.Count} ({steps.Count(s => s.enabled)} enabled, {steps.Count(s => s.build)} build-only)");
		sb.AppendLine();

		AppendRepositories(sb, config.repos);
		AppendEndpoints(sb, config);
		AppendSelection(sb, steps);
		AppendPlaceholders(sb, steps, config.repos);
		AppendSteps(sb, steps);

		return sb.ToString().TrimEnd();
	}

	private static void AppendRepositories(StringBuilder sb, LocalStackRepos repos)
	{
		sb.AppendLine("### Repositories this manifest points at");
		sb.AppendLine();

		if (repos == null)
		{
			// Manifests written before `repos` existed. The step list further down still carries the paths,
			// so say where to look rather than guessing them back out of step names.
			sb.AppendLine("This manifest predates the recorded repository paths. Read them off the");
			sb.AppendLine("`workingDirectory` of the steps listed below, or re-run `beam local init` to record them.");
			sb.AppendLine();
			return;
		}

		sb.AppendLine("| Repository | Path |");
		sb.AppendLine("|---|---|");
		sb.AppendLine($"| BeamableAPI (C# gateway + docker deps) | `{Display(repos.apiDir)}` |");
		sb.AppendLine($"| BeamableBackend (Scala services) | `{Display(repos.scalaDir)}` |");
		sb.AppendLine($"| Portal frontend | `{Display(repos.portalDir)}` |");
		sb.AppendLine($"| portal-localdev (web registry) | {DisplayOptional(repos.webRegistryDir)} |");
		sb.AppendLine($"| BeamableProduct (web packages) | {DisplayOptional(repos.productDir)} |");
		sb.AppendLine();
	}

	private static void AppendEndpoints(StringBuilder sb, LocalStackConfig config)
	{
		sb.AppendLine("### Endpoints");
		sb.AppendLine();
		sb.AppendLine($"- **Backend host** (`${{host}}`): `{Display(config.host)}`");
		sb.AppendLine($"- **Portal** (`${{portalUrl}}`): `{Display(config.portalUrl)}`");
		sb.AppendLine(string.IsNullOrWhiteSpace(config.javaHome)
			? "- **Java 8 home** (`${java}`): not baked in — resolved at run time from `--java-path` / `BEAM_JAVA_HOME` / auto-detection"
			: $"- **Java 8 home** (`${{java}}`): `{config.javaHome}`");
		sb.AppendLine();
	}

	private static void AppendSelection(StringBuilder sb, List<LocalStackStep> steps)
	{
		// `shell` narrows the "scala: " prefix to the host-JVM services. `scala: redis` shares the prefix but is
		// a docker container the Scala services depend on, not one of them.
		var scala = IdsWithPrefix(steps, ScalaPrefix, s => s.shell);
		var services = IdsWithPrefix(steps, LocalStackTemplate.MicroservicePrefix);
		var extensions = IdsWithPrefix(steps, LocalStackTemplate.ExtensionPrefix);
		var groups = IdsWithPrefix(steps, LocalStackTemplate.GroupPrefix);
		var webSteps = steps.Where(s => LocalStackTemplate.IsWebStep(s.name)).Select(s => s.name).ToList();

		sb.AppendLine("### What this stack runs");
		sb.AppendLine();
		AppendIdList(sb, $"Scala services ({scala.Count})", scala);
		AppendIdList(sb, $"Microservices ({services.Count})", services);
		AppendIdList(sb, $"Portal extensions ({extensions.Count})", extensions);
		AppendIdList(sb, $"Service groups ({groups.Count})", groups);
		sb.AppendLine(webSteps.Count > 0
			? $"- **Local web registry**: included ({string.Join(", ", webSteps.Select(n => $"`{n}`"))}). `beam local up --no-web-registry` skips them."
			: "- **Local web registry**: NOT included — the portal resolves `@beamable/*` from the published packages, not your local build. Re-run `beam local init` without `--no-web-registry` to add it.");
		sb.AppendLine();
	}

	/// <summary>
	/// Lists everything still holding an <c>&lt;EDIT: ...&gt;</c> placeholder. A manifest with any of these is
	/// not runnable: the orchestrator refuses to resolve a placeholder path, so the step silently never works.
	/// </summary>
	private static void AppendPlaceholders(StringBuilder sb, List<LocalStackStep> steps, LocalStackRepos repos)
	{
		var unresolved = new List<string>();

		void Check(string label, string value)
		{
			if (HasPlaceholder(value))
			{
				unresolved.Add($"{label} — `{value}`");
			}
		}

		if (repos != null)
		{
			Check("BeamableAPI", repos.apiDir);
			Check("BeamableBackend", repos.scalaDir);
			Check("Portal frontend", repos.portalDir);
			Check("portal-localdev", repos.webRegistryDir);
			Check("BeamableProduct", repos.productDir);
		}

		foreach (var step in steps)
		{
			if (HasPlaceholder(step.workingDirectory) || HasPlaceholder(step.requiredOutput))
			{
				unresolved.Add($"step `{step.name}`");
			}
		}

		if (unresolved.Count == 0)
		{
			return;
		}

		sb.AppendLine("### ⚠️ Not runnable yet");
		sb.AppendLine();
		sb.AppendLine("These still hold an unedited `<EDIT: ...>` placeholder. Fill them in (or re-run");
		sb.AppendLine("`beam local init` with the right `--*-dir` option) before `beam local up`:");
		sb.AppendLine();
		foreach (var entry in unresolved.Distinct())
		{
			sb.AppendLine($"- {entry}");
		}

		sb.AppendLine();
	}

	private static void AppendSteps(StringBuilder sb, List<LocalStackStep> steps)
	{
		sb.AppendLine("### Steps, in order");
		sb.AppendLine();
		sb.AppendLine("These are the exact names `beam local up --only` / `--skip`, `beam local logs` and");
		sb.AppendLine("`beam local stop` take.");
		sb.AppendLine();

		if (steps.Count == 0)
		{
			sb.AppendLine("_(none)_");
			sb.AppendLine();
			return;
		}

		sb.AppendLine("| # | Step | Notes |");
		sb.AppendLine("|---|---|---|");
		for (var i = 0; i < steps.Count; i++)
		{
			var step = steps[i];
			var notes = new List<string>();
			if (!step.enabled)
			{
				notes.Add("disabled");
			}

			if (step.build)
			{
				notes.Add("build");
			}

			if (!string.IsNullOrEmpty(step.group))
			{
				notes.Add($"group `{step.group}` (parallel)");
			}

			sb.AppendLine($"| {i + 1} | `{step.name}` | {(notes.Count > 0 ? string.Join(", ", notes) : "")} |");
		}

		sb.AppendLine();
	}

	private static List<string> IdsWithPrefix(List<LocalStackStep> steps, string prefix,
		Func<LocalStackStep, bool> also = null) =>
		steps
			.Where(s => s.name?.StartsWith(prefix, StringComparison.Ordinal) == true)
			.Where(s => also == null || also(s))
			.Select(s => s.name.Substring(prefix.Length))
			.ToList();

	private static void AppendIdList(StringBuilder sb, string label, List<string> ids) =>
		sb.AppendLine(ids.Count == 0
			? $"- **{label}**: none"
			: $"- **{label}**: {string.Join(", ", ids.Select(id => $"`{id}`"))}");

	private static bool HasPlaceholder(string value) =>
		value?.Contains(LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal) == true;

	private static string Display(string value) => string.IsNullOrWhiteSpace(value) ? "(unset)" : value;

	/// <summary>A null optional path means that part of the stack is not included, which is not the same as
	/// "unset" — say so rather than rendering an empty cell.</summary>
	private static string DisplayOptional(string value) =>
		string.IsNullOrWhiteSpace(value) ? "_not part of this stack_" : $"`{value}`";

	private static string ReadEmbeddedTemplate()
	{
		var assembly = typeof(LocalStackSkillTemplate).Assembly;
		using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
		if (stream == null)
		{
			return null;
		}

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
