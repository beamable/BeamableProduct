# Template Updater

A tool for syncing Beamable project templates with real projects on your machine.
When you improve a service or portal extension app locally, run this tool to propagate
those improvements back into the template so future projects start from a better baseline.

Two equivalent implementations are provided — use whichever fits your workflow:

| File | Runtime | How to run |
|---|---|---|
| `update-template.sh` | bash | `./update-template.sh <TemplateName>` |
| `update-template.cs` | .NET 10 | `dotnet run update-template.cs <TemplateName>` |

---

## Supported templates

| Template name | Identifies a project by |
|---|---|
| `BeamService` | `.csproj` containing `<BeamProjectType>service</BeamProjectType>` |
| `BeamStorage` | `.csproj` containing `<BeamProjectType>storage</BeamProjectType>` |
| `PortalExtensionApp` | `package.json` containing `"beamPortalExtension": true` |

Template files live at:
```
cli/beamable.templates/templates/<TemplateName>/
```

---

## Prerequisites

**Bash script** — no extra dependencies, works on any macOS/Linux shell.

**C# script** — requires .NET 10 SDK (for file-based `dotnet run`):
```bash
dotnet --version   # should print 10.x.x
```

---

## Running

Both scripts must be run from the `template_updater/` directory:

```bash
# Bash
cd template_updater
./update-template.sh BeamService

# C#
cd template_updater
dotnet run update-template.cs BeamService
```

On first run you will be asked to enter a **search path** — the root directory the tool
will scan for matching projects (e.g. `/Users/you/Projects`). This value is saved to
`config.json` so you only need to provide it once.

---

## Configuration — `config.json`

```json
{
  "searchPath": "/Users/yourname/Projects",
  "ignorePaths": [
    "*PackageCache*"
  ]
}
```

| Field | Description |
|---|---|
| `searchPath` | Root directory to scan for matching projects. Set once on first run; edit here to change it. |
| `ignorePaths` | Glob-style patterns for directories to **skip entirely** during the scan. Matching uses `*` as a wildcard anywhere in the path. |

**Example `ignorePaths` entries:**
```json
"ignorePaths": [
  "*PackageCache*",
  "*/.git/*",
  "*/Archive/*"
]
```

The scan prunes matching directories outright (does not descend into them), so adding
large or irrelevant trees here speeds up the scan significantly.

---

## How it works

### 1. Scan
The tool walks the file system from `searchPath`, skipping `obj/`, `bin/`,
`node_modules/`, and any paths matching `ignorePaths`. A progress indicator shows
the current directory being scanned along with the elapsed time.

### 2. Select project
Matching projects are listed sorted by most-recently-modified first. Enter the
number of the project you want to use as the source of updates.

### 3. Diff generation
For each template file the tool:
1. **Substitutes** occurrences of your project name back to the template name
   (three-pass to handle cases where the project name is a substring of the template name).
2. **Diffs** the substituted project file against the current template file.
3. **Filters** any change block where the template side contains the template name —
   these are intentional template-variable positions that must never be overwritten.

### 4. Per-hunk review
Changes are presented **one hunk at a time**. For each change you are prompted:

```
── Change 2/7  [src/MyService.cs] ──
--- template/src/BeamService.cs
+++ project/src/MyService.cs
@@ -14,6 +14,6 @@
     public override async Task Run()
-        await Task.Delay(1000);
+        await DoWork();

Apply this change? [y/N/q]
```

| Key | Action |
|---|---|
| `y` | Apply this hunk to the template |
| `n` (or Enter) | Skip this hunk, leave the template unchanged |
| `q` | Stop reviewing — already-applied hunks are kept, the rest are left untouched |

All approved hunks for a file are written in a single operation to avoid
line-number drift between hunks of the same file.

### 5. Smart apply
When writing a hunk, any line on the **template side** (`-`) that contains the
template name is preserved as-is — even if you said `y` to the surrounding change.
This prevents the template placeholder name from being accidentally replaced with
your project's name.

---

## Tips

- **Scan is slow?** Add the largest irrelevant directory trees to `ignorePaths`.
- **Wrong project listed?** The list is sorted by the file's last-modified time.
  The project you actively work on will almost always appear at the top.
- **Only want to update one thing?** Say `n` to every hunk except the one you want.
  The tool applies changes selectively — there is no "apply all or nothing".
- **Safe to re-run** — if you apply a change and the template already matches the
  project, that file will produce no diff on the next run.
