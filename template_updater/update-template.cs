using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// ── Args ───────────────────────────────────────────────────────────────────────
var validTemplates = new[] { "BeamService", "BeamStorage", "PortalExtensionApp" };

if (args.Length < 1) { Usage(); return; }

string templateName = args[0];
if (!validTemplates.Contains(templateName))
{
    Console.Error.WriteLine($"Error: Unsupported template '{templateName}'.");
    Usage();
    return;
}

// ── Paths ──────────────────────────────────────────────────────────────────────
// Run this script from the template_updater/ directory (same as the .sh script).
string workDir      = Directory.GetCurrentDirectory();
string templatesDir = Path.GetFullPath(Path.Combine(workDir, "..", "cli", "beamable.templates", "templates"));
string configFile   = Path.Combine(workDir, "config.json");
string templateDir  = Path.Combine(templatesDir, templateName);

if (!Directory.Exists(templateDir))
{
    Console.Error.WriteLine($"Error: Template directory not found: {templateDir}");
    return;
}

// ── 1. Config ──────────────────────────────────────────────────────────────────
JsonObject config = LoadOrCreateConfig(configFile);
string searchPath  = config["searchPath"]?.GetValue<string>() ?? "";
var ignorePaths    = (config["ignorePaths"]?.AsArray() ?? new JsonArray())
    .Select(n => n?.GetValue<string>() ?? "")
    .Where(s => s.Length > 0)
    .ToList();

if (string.IsNullOrEmpty(searchPath))
{
    Console.WriteLine("No search path configured. Where should the script search for projects?");
    Console.Write("Enter absolute path: ");
    searchPath = (Console.ReadLine() ?? "").Trim().TrimEnd('/');
    Console.WriteLine();

    if (!Directory.Exists(searchPath))
    {
        Console.Error.WriteLine($"Error: Directory not found: {searchPath}");
        return;
    }

    config["searchPath"] = searchPath;
    File.WriteAllText(configFile, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Search path saved to config.json.\n");
}

// ── 2. Find matching projects ──────────────────────────────────────────────────
Console.WriteLine($"Searching for '{templateName}' projects under {searchPath} ...");

var scanSw    = Stopwatch.StartNew();
var foundPairs = new List<(long mtime, string dir)>();

int consoleWidth = ConsoleWidth();

switch (templateName)
{
    case "BeamService":
    case "BeamStorage":
    {
        string beamType = templateName == "BeamService" ? "service" : "storage";
        string marker   = $"<BeamProjectType>{beamType}</BeamProjectType>";

        foreach (string f in EnumerateFiles(searchPath, "*.csproj", ignorePaths, [templatesDir]))
        {
            Console.Write($"\r  \x1b[2m{Truncate(f, consoleWidth - 4)}\x1b[0m");
            if (FileContains(f, marker))
            {
                long mtime = new DateTimeOffset(File.GetLastWriteTimeUtc(f)).ToUnixTimeSeconds();
                foundPairs.Add((mtime, Path.GetDirectoryName(f)!));
            }
        }
        Console.Write($"\r{new string(' ', consoleWidth - 1)}\r");
        break;
    }
    case "PortalExtensionApp":
    {
        foreach (string f in EnumerateFiles(searchPath, "package.json", ignorePaths, [templatesDir]))
        {
            Console.Write($"\r  \x1b[2m{Truncate(f, consoleWidth - 4)}\x1b[0m");
            if (FileContains(f, "\"beamPortalExtension\": true"))
            {
                long mtime = new DateTimeOffset(File.GetLastWriteTimeUtc(f)).ToUnixTimeSeconds();
                foundPairs.Add((mtime, Path.GetDirectoryName(f)!));
            }
        }
        Console.Write($"\r{new string(' ', consoleWidth - 1)}\r");
        break;
    }
}

scanSw.Stop();
Console.WriteLine($"Scan completed in {scanSw.Elapsed.TotalSeconds:F2}s — found {foundPairs.Count} project(s).");
Console.WriteLine();

if (foundPairs.Count == 0)
{
    Console.WriteLine($"No '{templateName}' projects found on this machine.");
    return;
}

// ── 3. Select project ──────────────────────────────────────────────────────────
var sorted = foundPairs.OrderByDescending(p => p.mtime).ToList();
var labels = sorted
    .Select(p => $"[{DateTimeOffset.FromUnixTimeSeconds(p.mtime).LocalDateTime:yyyy-MM-dd HH:mm}]  {p.dir}")
    .ToList();

Console.WriteLine($"Found {sorted.Count} project(s):");
for (int idx = 0; idx < labels.Count; idx++)
    Console.WriteLine($"  {idx + 1}) {labels[idx]}");

int choice = 0;
while (choice < 1 || choice > sorted.Count)
{
    Console.Write("\nSelect a project (enter number): ");
    if (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > sorted.Count)
    {
        Console.WriteLine($"Invalid selection. Enter a number between 1 and {sorted.Count}.");
        choice = 0;
    }
}

string selectedDir = sorted[choice - 1].dir;
Console.WriteLine($"\nSelected: {selectedDir}\n");

// ── 4. Determine project name ──────────────────────────────────────────────────
string sourceName  = templateName;
string projectName;

switch (templateName)
{
    case "BeamService":
    case "BeamStorage":
    {
        string? csproj = Directory.GetFiles(selectedDir, "*.csproj").FirstOrDefault();
        if (csproj == null) { Console.Error.WriteLine($"Error: No .csproj found in {selectedDir}"); return; }
        projectName = Path.GetFileNameWithoutExtension(csproj);
        break;
    }
    case "PortalExtensionApp":
    {
        string pkgPath = Path.Combine(selectedDir, "package.json");
        if (!File.Exists(pkgPath)) { Console.Error.WriteLine($"Error: package.json not found in {selectedDir}"); return; }
        var pkg = JsonNode.Parse(File.ReadAllText(pkgPath));
        projectName = pkg?["name"]?.GetValue<string>() ?? "";
        if (string.IsNullOrEmpty(projectName)) { Console.Error.WriteLine("Error: Could not read 'name' from package.json"); return; }
        break;
    }
    default:
        projectName = templateName;
        break;
}

Console.WriteLine($"Project name  : {projectName}");
Console.WriteLine($"Template name : {sourceName}");
Console.WriteLine();

// ── 5. Collect template files ──────────────────────────────────────────────────
var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".template.config", "obj", "bin", "node_modules" };

var templateFiles = Directory.EnumerateFiles(templateDir, "*", SearchOption.AllDirectories)
    .Where(f =>
    {
        var rel = Path.GetRelativePath(templateDir, f);
        return !rel.Split(Path.DirectorySeparatorChar).Any(seg => skipDirs.Contains(seg));
    })
    .Where(f => Path.GetFileName(f) != ".DS_Store")
    .Select(f => Path.GetRelativePath(templateDir, f))
    .OrderBy(f => f)
    .ToList();

// ── 6. Build diff ──────────────────────────────────────────────────────────────
Console.WriteLine("Generating diff...");
var diffSw = Stopwatch.StartNew();

string tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
Directory.CreateDirectory(tmpDir);

try
{
    var fileDiffs = new List<FileDiff>();

    foreach (string tmplRel in templateFiles)
    {
        string projRel  = tmplRel.Replace(sourceName, projectName);
        string projPath = Path.Combine(selectedDir, projRel);
        string tmplPath = Path.Combine(templateDir, tmplRel);

        if (!File.Exists(projPath)) continue;

        string substContent = Substitute(File.ReadAllText(projPath), sourceName, projectName);
        string tmpSubst     = Path.Combine(tmpDir, tmplRel + ".subst");
        Directory.CreateDirectory(Path.GetDirectoryName(tmpSubst)!);
        File.WriteAllText(tmpSubst, substContent);

        string rawDiff = RunDiff(tmplPath, tmpSubst, $"template/{tmplRel}", $"project/{projRel}");
        if (string.IsNullOrEmpty(rawDiff)) continue;

        string filteredDiff = FilterDiff(rawDiff, sourceName);
        if (string.IsNullOrWhiteSpace(filteredDiff)) continue;

        var hunks = ParseHunks(filteredDiff);
        if (hunks.Count > 0)
            fileDiffs.Add(new FileDiff(tmplRel, tmplPath, hunks));
    }

    diffSw.Stop();
    Console.WriteLine($"Diff generated in {diffSw.Elapsed.TotalSeconds:F2}s");
    Console.WriteLine();

    // ── 7. Review and apply per hunk ──────────────────────────────────────────
    int totalHunks = fileDiffs.Sum(f => f.Hunks.Count);

    if (totalHunks == 0)
    {
        Console.WriteLine($"No changes detected. Template is already up to date with '{projectName}'.");
        return;
    }

    Console.WriteLine($"Found {totalHunks} change(s) across {fileDiffs.Count} file(s). Reviewing one by one...");
    Console.WriteLine("  y = apply   n = skip   q = quit\n");

    int hunkNum      = 0;
    int appliedTotal = 0;
    bool quit        = false;

    foreach (var fileDiff in fileDiffs)
    {
        if (quit) break;

        var approvedHunks = new List<DiffHunk>();

        foreach (var hunk in fileDiff.Hunks)
        {
            if (quit) break;

            hunkNum++;
            Console.WriteLine($"\x1b[1m── Change {hunkNum}/{totalHunks}  [{fileDiff.TmplRel}] ──\x1b[0m");
            PrintColoredHunk(hunk);
            Console.WriteLine();

            string answer;
            while (true)
            {
                Console.Write("Apply this change? [y/N/q] ");
                answer = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
                if (answer is "" or "y" or "yes" or "n" or "no" or "q" or "quit") break;
                Console.WriteLine("  Please enter y, n, or q.");
            }
            Console.WriteLine();

            if (answer is "q" or "quit") { quit = true; break; }
            if (answer is "y" or "yes")  approvedHunks.Add(hunk);
        }

        if (approvedHunks.Count > 0)
        {
            string partialDiff = ReconstructDiff(approvedHunks);
            string merged      = SmartApplyDiff(fileDiff.TmplPath, partialDiff, sourceName);
            File.WriteAllText(fileDiff.TmplPath, merged);
            appliedTotal += approvedHunks.Count;
            Console.WriteLine($"  \x1b[32m✓\x1b[0m Updated: {fileDiff.TmplRel} ({approvedHunks.Count}/{fileDiff.Hunks.Count} hunk(s) applied)");
            Console.WriteLine();
        }
    }

    if (quit)
        Console.WriteLine("Stopped early. Remaining changes were not reviewed.");

    Console.WriteLine($"Done. Applied {appliedTotal}/{totalHunks} change(s) in the '{templateName}' template.");
}
finally
{
    Directory.Delete(tmpDir, recursive: true);
}

// ── Helper functions ───────────────────────────────────────────────────────────

static void Usage()
{
    Console.Error.WriteLine("Usage: dotnet run update-template.cs <TemplateName>");
    Console.Error.WriteLine("  TemplateName: BeamService | BeamStorage | PortalExtensionApp");
}

static int ConsoleWidth()
{
    try { return Math.Max(Console.WindowWidth, 80); }
    catch { return 100; }
}

static string Truncate(string s, int maxLen) =>
    s.Length <= maxLen ? s : "..." + s[^(maxLen - 3)..];

static JsonObject LoadOrCreateConfig(string path)
{
    if (File.Exists(path))
    {
        try
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is JsonObject obj) return obj;
        }
        catch { /* fall through to default */ }
    }
    return new JsonObject
    {
        ["searchPath"] = "",
        ["ignorePaths"] = new JsonArray("*PackageCache*"),
    };
}

/// <summary>
/// BFS file enumeration that prunes ignored directories entirely
/// (equivalent to find's -prune: never descends into them).
/// </summary>
static IEnumerable<string> EnumerateFiles(
    string root, string pattern,
    List<string> ignoreGlobs, string[] prunePaths)
{
    var queue = new Queue<string>();
    queue.Enqueue(root);

    while (queue.Count > 0)
    {
        string dir = queue.Dequeue();

        if (prunePaths.Any(p => dir.StartsWith(p, StringComparison.OrdinalIgnoreCase))) continue;
        if (MatchesAnyGlob(dir, ignoreGlobs)) continue;

        IEnumerable<string> files;
        try   { files = Directory.GetFiles(dir, pattern); }
        catch { files = []; }
        foreach (string f in files) yield return f;

        IEnumerable<string> subdirs;
        try   { subdirs = Directory.GetDirectories(dir); }
        catch { subdirs = []; }

        foreach (string sub in subdirs)
        {
            string name = Path.GetFileName(sub);
            if (name is "obj" or "bin" or "node_modules") continue;
            if (MatchesAnyGlob(sub, ignoreGlobs)) continue;
            queue.Enqueue(sub);
        }
    }
}

/// <summary>
/// Simple glob match: strips '*' wildcards and checks if the path contains the literal remainder.
/// Handles patterns like "*PackageCache*".
/// </summary>
static bool MatchesAnyGlob(string path, List<string> patterns)
{
    foreach (string pattern in patterns)
    {
        string literal = pattern.Replace("*", "");
        if (literal.Length > 0 && path.Contains(literal, StringComparison.OrdinalIgnoreCase))
            return true;
    }
    return false;
}

static bool FileContains(string path, string text)
{
    try   { return File.ReadAllText(path).Contains(text); }
    catch { return false; }
}

/// <summary>
/// Three-pass substitution: freeze SOURCE_NAME → swap PROJECT_NAME → thaw SOURCE_NAME.
/// Prevents PROJECT_NAME (when it is a substring of SOURCE_NAME) from corrupting
/// SOURCE_NAME occurrences already present in the file.
/// </summary>
static string Substitute(string content, string sourceName, string projectName)
{
    const string ph = "__BEAM_TMPL_NAME_PLACEHOLDER__";
    return content
        .Replace(sourceName,  ph)
        .Replace(projectName, sourceName)
        .Replace(ph,          sourceName);
}

/// <summary>Runs `diff -u` and returns the unified diff output (empty string if files are identical).</summary>
static string RunDiff(string origPath, string substPath, string labelOrig, string labelSubst)
{
    var psi = new ProcessStartInfo("diff")
    {
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
        UseShellExecute        = false,
    };
    psi.ArgumentList.Add("-u");
    psi.ArgumentList.Add("-L"); psi.ArgumentList.Add(labelOrig);
    psi.ArgumentList.Add("-L"); psi.ArgumentList.Add(labelSubst);
    psi.ArgumentList.Add(origPath);
    psi.ArgumentList.Add(substPath);

    using var proc = Process.Start(psi)!;
    string output = proc.StandardOutput.ReadToEnd();
    proc.WaitForExit();
    return output;
}

/// <summary>
/// Reads a unified diff and drops any change block where a removed line ('-' side)
/// contains sourceName. Those are template-variable positions that must not be updated.
/// </summary>
static string FilterDiff(string diffText, string sourceName)
{
    var lines     = diffText.Split('\n');
    var sb        = new StringBuilder();
    string hdr1   = "", hdr2 = "";
    int i         = 0;

    while (i < lines.Length)
    {
        string line = lines[i];

        if (line.StartsWith("---"))  { hdr1 = line; hdr2 = ""; i++; continue; }
        if (line.StartsWith("+++")) { hdr2 = line;              i++; continue; }
        if (!line.StartsWith("@@")) {                            i++; continue; }

        string hunkHdr = line; i++;

        var hunk = new List<string>();
        while (i < lines.Length && !lines[i].StartsWith("@@") && !lines[i].StartsWith("---"))
            hunk.Add(lines[i++]);

        var filtered    = new StringBuilder();
        bool hasChanges = false;
        int j           = 0;

        while (j < hunk.Count)
        {
            if (!hunk[j].StartsWith("-") && !hunk[j].StartsWith("+"))
            {
                filtered.Append(hunk[j++] + "\n");
                continue;
            }

            var minus = new List<string>();
            var plus  = new List<string>();
            bool bad  = false;

            while (j < hunk.Count && (hunk[j].StartsWith("-") || hunk[j].StartsWith("+")))
            {
                if (hunk[j].StartsWith("-"))
                {
                    if (hunk[j].Contains(sourceName)) bad = true;
                    minus.Add(hunk[j]);
                }
                else plus.Add(hunk[j]);
                j++;
            }

            if (!bad)
            {
                foreach (var l in minus) filtered.Append(l + "\n");
                foreach (var l in plus)  filtered.Append(l + "\n");
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            if (hdr1.Length > 0) { sb.Append(hdr1 + "\n"); sb.Append(hdr2 + "\n"); hdr1 = ""; }
            sb.Append(hunkHdr + "\n");
            sb.Append(filtered);
        }
    }

    return sb.ToString();
}

/// <summary>Splits a filtered unified diff into individual hunks.</summary>
static List<DiffHunk> ParseHunks(string filteredDiff)
{
    var lines  = filteredDiff.Split('\n');
    var result = new List<DiffHunk>();
    string hdr1 = "", hdr2 = "";
    int i = 0;

    while (i < lines.Length)
    {
        if (lines[i].StartsWith("---"))  { hdr1 = lines[i++]; continue; }
        if (lines[i].StartsWith("+++")) { hdr2 = lines[i++]; continue; }
        if (!lines[i].StartsWith("@@")) { i++; continue; }

        string hunkHdr = lines[i++];
        var body = new StringBuilder();
        while (i < lines.Length && !lines[i].StartsWith("@@") && !lines[i].StartsWith("---"))
            body.Append(lines[i++] + "\n");

        result.Add(new DiffHunk(hdr1, hdr2, hunkHdr, body.ToString()));
    }

    return result;
}

/// <summary>Rebuilds a valid unified diff string from a subset of hunks.</summary>
static string ReconstructDiff(List<DiffHunk> hunks)
{
    if (hunks.Count == 0) return "";
    var sb = new StringBuilder();
    sb.Append(hunks[0].Hdr1 + "\n");
    sb.Append(hunks[0].Hdr2 + "\n");
    foreach (var h in hunks)
    {
        sb.Append(h.HunkHdr + "\n");
        sb.Append(h.Body);
    }
    return sb.ToString();
}

/// <summary>
/// Applies a pre-built unified diff to origPath selectively:
///   - If any removed line contains sourceName → keep the original template lines.
///   - Otherwise → apply the substituted project lines.
/// Returns the merged file content as a string.
/// </summary>
static string SmartApplyDiff(string origPath, string diffText, string sourceName)
{
    string[] origLines = File.ReadAllLines(origPath);

    if (string.IsNullOrEmpty(diffText))
        return string.Join("\n", origLines) + (origLines.Length > 0 ? "\n" : "");

    var result = new StringBuilder();
    int pos    = 1;

    var minus = new List<string>();
    var plus  = new List<string>();

    void Flush()
    {
        if (minus.Count == 0 && plus.Count == 0) return;

        bool bad = minus.Any(l => l.Length > 1 && l[1..].Contains(sourceName));
        if (bad)
        {
            foreach (var _ in minus)
                result.Append(origLines[pos++ - 1] + "\n");
        }
        else
        {
            pos += minus.Count;
            foreach (var l in plus)
                result.Append(l[1..] + "\n");
        }

        minus.Clear();
        plus.Clear();
    }

    foreach (string rawLine in diffText.Split('\n'))
    {
        if (rawLine.StartsWith("---") || rawLine.StartsWith("+++")) continue;

        if (rawLine.StartsWith("@@"))
        {
            Flush();
            var m = Regex.Match(rawLine, @"^@@ -(\d+)");
            if (m.Success)
            {
                int tgt = int.Parse(m.Groups[1].Value);
                while (pos < tgt && pos <= origLines.Length)
                    result.Append(origLines[pos++ - 1] + "\n");
            }
            continue;
        }

        if (rawLine.StartsWith("-")) { minus.Add(rawLine); continue; }
        if (rawLine.StartsWith("+")) { plus.Add(rawLine);  continue; }

        Flush();
        if (pos <= origLines.Length)
            result.Append(origLines[pos++ - 1] + "\n");
    }

    Flush();
    while (pos <= origLines.Length)
        result.Append(origLines[pos++ - 1] + "\n");

    return result.ToString();
}

static void PrintColoredHunk(DiffHunk hunk)
{
    foreach (string line in new[] { hunk.Hdr1, hunk.Hdr2, hunk.HunkHdr }
        .Concat(hunk.Body.Split('\n')))
    {
        if      (line.StartsWith("---") || line.StartsWith("+++")) Console.WriteLine($"\x1b[1;34m{line}\x1b[0m");
        else if (line.StartsWith("@@"))                            Console.WriteLine($"\x1b[1;36m{line}\x1b[0m");
        else if (line.StartsWith("-"))                             Console.WriteLine($"\x1b[31m{line}\x1b[0m");
        else if (line.StartsWith("+"))                             Console.WriteLine($"\x1b[32m{line}\x1b[0m");
        else                                                        Console.WriteLine(line);
    }
}

// ── Record types ───────────────────────────────────────────────────────────────
record DiffHunk(string Hdr1, string Hdr2, string HunkHdr, string Body);
record FileDiff(string TmplRel, string TmplPath, List<DiffHunk> Hunks);
