---
name: create-mcp-skill
description: Write or rework a Beamable CLI MCP skill (a .md.scriban template under cli/Docs/SkillTemplates/) that documents a system for AI agents. Use when asked to create, update, or review an MCP skill.
---

# Create an MCP Skill

An MCP skill is "highly detailed usage docs" served to agents by `beam_get_skill`. It is a Scriban template rendered to Markdown at build time.

## Where things live

| Part | File |
|---|---|
| Templates (edit these) | `cli/Docs/SkillTemplates/<name>.md.scriban` |
| Rendered output (gitignored, generated) | `cli/Docs/Skills/<name>.md` |
| Renderer | `cli/Commands/Docs/GenerateSkillDocsCommand.cs` |
| Template data model | `cli/Commands/Docs/SkillTemplateData.cs` |
| Serving | `cli/Commands/Mcp/McpServerBuilder.cs` (`beam_get_skill`), `McpToolExecutor.cs` |
| Skill index for agents | `cli/Docs/AGENTS.md` → "Workflow Skills" |

## Process

1. **Ask for other sources.** Before reading, ask the user whether any other repo or source code (outside this repo) would help with this skill. Read those too.
2. **Read the whole system.** Read every file of the system in full and grep for all its types, routes, constants, and options classes.
3. **Trust the code over the comments.** Take signatures, defaults, limits, error codes, and status strings from code. If a comment disagrees with the code, the code wins.
4. **Write the template** using the structure below.
5. **Register it** with a one-line entry in `cli/Docs/AGENTS.md` under "Workflow Skills".
6. **Render and check**: `dotnet build cli/cli.csproj`, then read `cli/Docs/Skills/<name>.md` and confirm the Scriban blocks rendered.

## Structure

Frontmatter is required (`McpToolExecutor` checks for `---`):

```
---
name: beam-<topic>
description: <One sentence: what the agent can do with this skill>
---
```

Sections, in this order:

1. **High-Level Architecture**: only the data flow through the system, ideally as a short ASCII diagram. Don't describe code the agent can read.
2. **Code Mapping**: a `part → file` table with repo-relative paths, naming the repo when a file comes from a source the user provided. No code descriptions; the agent should go read the file.
3. **Business Goals Constraints**: why the system exists and the use cases it covers, in a few bullets.
4. **Performance Constraints**: throughput, latency, batch sizes, timeouts, cache TTLs, and retry caps, with real defaults from the options classes.
5. **Usage Guide**: routes, signatures, request/response shapes, error codes, and minimal code snippets. Use tables for enumerations.

Link related skills by name at the top, e.g. `> For X, load \`beam-other-skill\`.`

## Include only

- Design decisions that have a technical constraint behind them, stated as facts.
- Real API signatures and payloads. Agents waste most of their time guessing wrong ones.
- Concise language: bullets and tables over prose.

## Do NOT include

- Discussion context ("because someone said…", "we decided in the meeting…").
- References to plans, PRs, tickets, branches, or named reworks ("the funnel rework").
- History ("used to be", "a past bug", "has not caught up yet", "will be fixed").
- SDK or codegen drift notes and temporary workarounds.
- Descriptions of what the code does line by line.
- Duplicate checklists or pitfall lists that repeat the Usage Guide.

## Scriban data

Available globals (snake_case, renamed from `SkillTemplateData`):

- `commands["<path without beam>"]` → `name`, `description`, `execution_path`, `arguments[]`, `options[]` (`name`, `aliases`, `description`, `type`, `is_required`)
- `federation_types[]` → `interface_name`, `namespace`, `summary`, `generic_constraint`, `platform`, `methods[]`
- `unreal_type_mappings[]` → `cpp_type`, `c_sharp_equivalent`, `notes`

Guard every lookup so a missing command doesn't break rendering:

```
{{ if commands["project new service"] }}
| Option | Type | Description |
|---|---|---|
{{ for opt in commands["project new service"].options }}`{{ opt.name }}` | {{ opt.type }} | {{ opt.description }}
{{ end }}{{ end }}
```

Prefer generated tables like this over hand-copied CLI options, which go stale. In examples, call commands through `beam_exec("<command> -q")`.

## Before finishing

**Sources**
- [ ] Asked the user for other repos or source code, and read what they gave.
- [ ] Read every file of the system in full.

**Structure**
- [ ] Frontmatter has `name` (`beam-<topic>`) and a one-sentence `description`.
- [ ] The five sections are present, in order, with these exact headings.
- [ ] Related skills are linked by name at the top.

**Accuracy**
- [ ] Every file path in Code Mapping exists. Check with `ls` or Glob.
- [ ] Every type, method, route, error code and status string you name exists. Grep for each one.
- [ ] Defaults and limits match the code (options classes, constants), not comments or design docs.
- [ ] Code snippets use real signatures.
- [ ] Wherever the code and its comments disagree, the skill follows the code.

**Content**
- [ ] Architecture shows only data flow.
- [ ] Code Mapping holds only part → file, with no description of the code.
- [ ] Business and Performance sections are short context, not a feature list.
- [ ] Nothing restates another skill. Point to it by name instead.
- [ ] No rule appears in more than one section; no checklist or pitfall list repeating the Usage Guide.

**Wording**
- [ ] Grep the file for `#[0-9]`, `PR`, `rework`, `plan`, `used to`, `no longer`, `now `, `moved`,
      `was `, `previously`, `caught up`, `past bug`. Remove each hit or rewrite it as a current-state fact.
- [ ] No names of people, quotes from reviewers, or "we decided".
- [ ] No SDK/codegen drift notes or temporary workarounds.

**Template**
- [ ] Every Scriban lookup is guarded with `{{ if }}`.
- [ ] Entry added to `cli/Docs/AGENTS.md` under "Workflow Skills".
- [ ] `dotnet build cli/cli.csproj` succeeds and `cli/Docs/Skills/<name>.md` renders correctly.

**Wrap-up**
- [ ] If an existing skill changed, other skills that link to it still point at what exists.
- [ ] Don't commit.
