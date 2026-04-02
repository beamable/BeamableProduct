using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Beamable.Server;

namespace cli.Docs;

public class GenerateWebComponentDocsCommandArgs : CommandArgs
{
    public string webTypesFile;
    public string outputDirectory;
    public string snippetsDirectory;
}

public class GenerateWebComponentDocsCommandResult
{
    public List<string> generatedFiles = new();
}

public class GenerateWebComponentDocsCommand
    : AtomicCommand<GenerateWebComponentDocsCommandArgs, GenerateWebComponentDocsCommandResult>
        , IStandaloneCommand
        , ISkipManifest
{
    public override bool IsForInternalUse => true;

    public GenerateWebComponentDocsCommand() : base("web-components", "Generate the docs for web components")
    {
    }

    public override void Configure()
    {
        AddOption(new Option<string>("--components", "Path to a web-types.json file describing the web components"),
            (args, v) => args.webTypesFile = v);
        AddOption(new Option<string>(new[] { "--output-dir", "-o" }, () => "web-component-docs",
            "A folder where the output markdown files will be written"),
            (args, v) => args.outputDirectory = v);
        AddOption(new Option<string>("--snippets",
            "Optional path to a folder containing {tag-name}.md snippet files"),
            (args, v) => args.snippetsDirectory = v);
    }

    public override async Task<GenerateWebComponentDocsCommandResult> GetResult(GenerateWebComponentDocsCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.webTypesFile))
            throw new CliException("--web-types is required");

        if (!File.Exists(args.webTypesFile))
            throw new CliException($"web-types file not found: {args.webTypesFile}");

        var json = await File.ReadAllTextAsync(args.webTypesFile);
        var manifest = JsonSerializer.Deserialize<CustomElementsManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var elements = manifest?.Modules
            ?.SelectMany(m => m.Declarations ?? new List<CemDeclaration>())
            .Where(d => d.Kind == "custom-element" && !string.IsNullOrWhiteSpace(d.TagName))
            .ToList();

        if (elements is not { Count: > 0 })
        {
            Log.Warning("No custom-element declarations found in manifest");
            return new GenerateWebComponentDocsCommandResult();
        }

        Directory.CreateDirectory(args.outputDirectory);

        var result = new GenerateWebComponentDocsCommandResult();
        var summarySb = new StringBuilder();

        foreach (var element in elements)
        {
            var fileName = $"{element.TagName}.md";
            var filePath = Path.Combine(args.outputDirectory, fileName);
            var snippet = LoadComponentSnippet(element.TagName, args.snippetsDirectory);
            var markdown = RenderComponentDoc(element, snippet);
            await File.WriteAllTextAsync(filePath, markdown);
            result.generatedFiles.Add(filePath);
            Log.Information("Generated {File}", filePath);

            var label = TagNameToLabel(element.TagName);
            summarySb.AppendLine($"- [{label}]({fileName})");
        }

        var summaryPath = Path.Combine(args.outputDirectory, "SUMMARY.md");
        await File.WriteAllTextAsync(summaryPath, summarySb.ToString());
        result.generatedFiles.Add(summaryPath);

        return result;
    }

    static string RenderComponentDoc(CemDeclaration element, ComponentSnippet snippet = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# `<{element.TagName}>`");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(element.Description))
        {
            sb.AppendLine(element.Description.Trim());
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(snippet?.About))
        {
            sb.AppendLine(snippet.About);
            sb.AppendLine();
        }

        // Interactive demo — unified controls + attributes table + live preview + HTML snippet
        sb.AppendLine("## Interactive Demo");
        sb.AppendLine();
        sb.AppendLine(RenderInteractiveDemo(element));
        sb.AppendLine();

        // Events
        if (element.Events is { Count: > 0 })
        {
            sb.AppendLine("## Events");
            sb.AppendLine();
            sb.AppendLine("| Event | Description |");
            sb.AppendLine("|-------|-------------|");
            foreach (var evt in element.Events)
            {
                var desc = EscapeMarkdownCell(evt.Description ?? "");
                sb.AppendLine($"| `{evt.Name}` | {desc} |");
            }
            sb.AppendLine();
        }

        // Slots
        if (element.Slots is { Count: > 0 })
        {
            sb.AppendLine("## Slots");
            sb.AppendLine();
            sb.AppendLine("| Slot | Description |");
            sb.AppendLine("|------|-------------|");
            foreach (var slot in element.Slots)
            {
                var slotName = string.IsNullOrEmpty(slot.Name) ? "(default)" : slot.Name;
                var desc = EscapeMarkdownCell(slot.Description ?? "");
                sb.AppendLine($"| `{slotName}` | {desc} |");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(snippet?.Notes))
        {
            sb.AppendLine("## Notes");
            sb.AppendLine();
            sb.AppendLine(snippet.Notes);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    static string RenderInteractiveDemo(CemDeclaration element)
    {
        var attrs = element.Attributes ?? new List<CemAttribute>();
        var demoAttrs = attrs.Where(a => a.Name != null).ToList();
        var demoId = $"demo-{element.TagName}";
        var compId = $"comp-{element.TagName}";
        var themeId = $"{demoId}-theme";
        var htmlSnippetId = $"html-{element.TagName}";
        var slotText = TagNameToLabel(element.TagName);

        var sb = new StringBuilder();

        // IMPORTANT: Python-Markdown type-6 HTML blocks terminate at the first blank line.
        // No blank lines and no 4-space-indented lines inside this block.
        sb.AppendLine("<div class=\"web-component-demo\">");
        sb.AppendLine($"<div id=\"{demoId}\" class=\"demo-wrapper\">");
        // Preview
        sb.AppendLine("<div class=\"demo-preview\" style=\"margin-bottom:1rem;\">");
        sb.AppendLine($"<{element.TagName} id=\"{compId}\">{slotText}</{element.TagName}>");
        sb.AppendLine("</div>");
        // HTML snippet
        sb.AppendLine("<div style=\"position:relative;margin-bottom:1rem;\">");
        sb.AppendLine($"<pre id=\"{htmlSnippetId}\" style=\"margin:0;padding:0.75rem 3.5rem 0.75rem 0.75rem;background:var(--md-code-bg-color,#1e1e2e);color:var(--md-code-fg-color,#cdd6f4);border-radius:4px;overflow-x:auto;font-size:0.82rem;font-family:var(--md-code-font,monospace);line-height:1.6;white-space:pre;\"></pre>");
        sb.AppendLine($"<button id=\"copy-{element.TagName}\" style=\"position:absolute;top:0.5rem;right:0.5rem;padding:2px 8px;font-size:0.75rem;cursor:pointer;border-radius:3px;\" title=\"Copy HTML\">Copy</button>");
        sb.AppendLine("</div>");
        // Controls table (3 columns: Attribute | Control | Description)
        sb.AppendLine("<div class=\"demo-controls\" style=\"overflow-x:auto;\">");
        sb.AppendLine("<table class=\"demo-attrs-table\">");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr><th>Attribute</th><th>Control</th><th>Description</th></tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");
        sb.AppendLine($"<tr><td><code>dark mode</code></td><td><input id=\"{themeId}\" type=\"checkbox\"></td><td>Toggle dark/light theme for this preview.</td></tr>");

        foreach (var attr in demoAttrs)
        {
            var inputId = $"{demoId}-{attr.Name}";
            var desc = EscapeHtml(attr.Description ?? "");
            var controlHtml = RenderControl(attr, inputId, element.Members);
            sb.AppendLine($"<tr><td><code>{EscapeHtml(attr.Name)}</code></td><td>{controlHtml}</td><td>{desc}</td></tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>");
        sb.AppendLine("(function () {");
        sb.AppendLine($"  var comp = document.getElementById('{compId}');");
        sb.AppendLine($"  var wrapper = document.getElementById('{demoId}');");
        sb.AppendLine($"  var htmlSnippet = document.getElementById('{htmlSnippetId}');");
        sb.AppendLine($"  var copyBtn = document.getElementById('copy-{element.TagName}');");
        sb.AppendLine("  if (!comp || !wrapper) return;");
        // buildHtml: one attribute per line for readability
        sb.AppendLine("  function buildHtml() {");
        sb.AppendLine($"    var tag = '{element.TagName}';");
        sb.AppendLine("    var attrList = [];");
        sb.AppendLine("    for (var i = 0; i < comp.attributes.length; i++) {");
        sb.AppendLine("      var a = comp.attributes[i];");
        sb.AppendLine("      attrList.push(a.value === '' ? '  ' + a.name : '  ' + a.name + '=\"' + a.value + '\"');");
        sb.AppendLine("    }");
        // Format: <tag\n  attr>\n  content\n</tag>  (> closes on last-attr line)
        sb.AppendLine($"    if (attrList.length === 0) return '<' + tag + '>{slotText}</' + tag + '>';");
        sb.AppendLine($"    return '<' + tag + '\\n' + attrList.join('\\n') + '>\\n  {slotText}\\n</' + tag + '>';");
        sb.AppendLine("  }");
        // highlightHtml: tracks inTag state so content lines aren't mistaken for attr names.
        // Uses MkDocs Material CSS vars so colors match the site theme automatically.
        sb.AppendLine("  function highlightHtml(raw) {");
        sb.AppendLine("    var KW = 'color:var(--md-code-hl-keyword-color)';");
        sb.AppendLine("    var AT = 'color:var(--md-code-hl-attribute-color)';");
        sb.AppendLine("    var ST = 'color:var(--md-code-hl-string-color)';");
        sb.AppendLine("    var PT = 'color:var(--md-code-hl-punctuation-color)';");
        sb.AppendLine("    function sp(c, t) { return '<span style=\"' + c + '\">' + t + '</span>'; }");
        sb.AppendLine("    function esc(s) { return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/\"/g,'&quot;'); }");
        sb.AppendLine("    var lines = raw.split('\\n'), result = [], inTag = true;");
        sb.AppendLine("    for (var i = 0; i < lines.length; i++) {");
        sb.AppendLine("      var L = lines[i], m;");
        // line 0: <tagname  or  <tagname>content</tagname>
        sb.AppendLine("      if (i === 0 && (m = L.match(/^<([\\w-]+)(>.*)?$/))) {");
        sb.AppendLine("        var open = sp(PT, '&lt;') + sp(KW, m[1]);");
        sb.AppendLine("        if (m[2]) { inTag = false;");
        sb.AppendLine("          result.push(open + m[2].replace(/>(.*?)<\\/([\\w-]+)>$/, function(_, c, n) { return sp(PT, '&gt;') + esc(c) + sp(PT, '&lt;/') + sp(KW, n) + sp(PT, '&gt;'); }));");
        sb.AppendLine("        } else { result.push(open); }");
        // inside opening tag: attr lines, last one ends with >
        sb.AppendLine("      } else if (inTag) {");
        sb.AppendLine("        var close = L.slice(-1) === '>';");
        sb.AppendLine("        var body = close ? L.slice(0, -1) : L;");
        sb.AppendLine("        if ((m = body.match(/^(\\s+)([\\w-]+)=\"(.*)\"$/))) {");
        sb.AppendLine("          result.push(m[1] + sp(AT, m[2]) + sp(PT, '=') + sp(ST, '&quot;' + esc(m[3]) + '&quot;') + (close ? sp(PT, '&gt;') : ''));");
        sb.AppendLine("        } else if ((m = body.match(/^(\\s+)([\\w-]+)$/))) {");
        sb.AppendLine("          result.push(m[1] + sp(AT, m[2]) + (close ? sp(PT, '&gt;') : ''));");
        sb.AppendLine("        } else { result.push(esc(L)); }");
        sb.AppendLine("        if (close) inTag = false;");
        // closing tag: </tagname>
        sb.AppendLine("      } else if ((m = L.match(/^<\\/([\\w-]+)>$/))) {");
        sb.AppendLine("        result.push(sp(PT, '&lt;/') + sp(KW, m[1]) + sp(PT, '&gt;'));");
        // content / slot text — just escape
        sb.AppendLine("      } else { result.push(esc(L)); }");
        sb.AppendLine("    }");
        sb.AppendLine("    return result.join('\\n');");
        sb.AppendLine("  }");
        sb.AppendLine("  function updateSnippet() {");
        sb.AppendLine("    if (htmlSnippet) htmlSnippet.innerHTML = highlightHtml(buildHtml());");
        sb.AppendLine("  }");
        sb.AppendLine("  function applyInput(input) {");
        sb.AppendLine("    var attr = input.dataset.attr;");
        sb.AppendLine("    var type = input.dataset.type;");
        sb.AppendLine("    if (type === 'boolean') {");
        sb.AppendLine("      if (input.checked) { comp.setAttribute(attr, ''); }");
        sb.AppendLine("      else { comp.removeAttribute(attr); }");
        sb.AppendLine("    } else {");
        sb.AppendLine("      if (input.value !== '') { comp.setAttribute(attr, input.value); }");
        sb.AppendLine("      else { comp.removeAttribute(attr); }");
        sb.AppendLine("    }");
        sb.AppendLine("    updateSnippet();");
        sb.AppendLine("  }");
        sb.AppendLine("  wrapper.querySelectorAll('input[data-attr], select[data-attr]').forEach(function (input) {");
        sb.AppendLine("    applyInput(input);");
        sb.AppendLine("    input.addEventListener('input', function () { applyInput(input); });");
        sb.AppendLine("    input.addEventListener('change', function () { applyInput(input); });");
        sb.AppendLine("  });");
        sb.AppendLine("  updateSnippet();");
        sb.AppendLine($"  var themeToggle = document.getElementById('{themeId}');");
        sb.AppendLine("  if (themeToggle) {");
        sb.AppendLine("    themeToggle.addEventListener('change', function () {");
        sb.AppendLine("      if (window.__beamSetDark) window.__beamSetDark(themeToggle.checked);");
        sb.AppendLine("    });");
        sb.AppendLine("    setTimeout(function () {");
        sb.AppendLine("      if (window.__beamVuetify && window.__beamVuetify.framework) {");
        sb.AppendLine("        themeToggle.checked = window.__beamVuetify.framework.theme.dark;");
        sb.AppendLine("      }");
        sb.AppendLine("    }, 100);");
        sb.AppendLine("  }");
        sb.AppendLine("  if (copyBtn) {");
        sb.AppendLine("    copyBtn.addEventListener('click', function () {");
        sb.AppendLine("      var text = buildHtml();");
        sb.AppendLine("      if (navigator.clipboard) {");
        sb.AppendLine("        navigator.clipboard.writeText(text).then(function () {");
        sb.AppendLine("          copyBtn.textContent = 'Copied!';");
        sb.AppendLine("          setTimeout(function () { copyBtn.textContent = 'Copy'; }, 1500);");
        sb.AppendLine("        });");
        sb.AppendLine("      } else {");
        sb.AppendLine("        var ta = document.createElement('textarea');");
        sb.AppendLine("        ta.value = text;");
        sb.AppendLine("        document.body.appendChild(ta);");
        sb.AppendLine("        ta.select();");
        sb.AppendLine("        document.execCommand('copy');");
        sb.AppendLine("        document.body.removeChild(ta);");
        sb.AppendLine("        copyBtn.textContent = 'Copied!';");
        sb.AppendLine("        setTimeout(function () { copyBtn.textContent = 'Copy'; }, 1500);");
        sb.AppendLine("      }");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine("})();");
        sb.AppendLine("</script>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    /// <summary>Renders the appropriate HTML control for a CEM attribute.</summary>
    static string RenderControl(CemAttribute attr, string inputId, List<CemMember> members)
    {
        var typeText = attr.Type?.Text ?? "string";
        var defaultVal = BuildDefaultValue(attr, members);
        var inferredType = InferType(attr);

        if (inferredType == "boolean")
        {
            var checkedAttr = defaultVal == "true" ? " checked" : "";
            return $"<input id=\"{inputId}\" type=\"checkbox\" data-attr=\"{attr.Name}\" data-type=\"boolean\"{checkedAttr}>";
        }

        if (inferredType == "number")
        {
            var numDefault = string.IsNullOrWhiteSpace(defaultVal) ? "0" : defaultVal;
            return $"<input id=\"{inputId}\" type=\"number\" data-attr=\"{attr.Name}\" data-type=\"number\" value=\"{EscapeHtml(numDefault)}\">";
        }

        var enumValues = ExtractEnumValues(typeText);
        var strDefault = defaultVal.Trim('\'');

        if (enumValues.Count > 0)
        {
            // All enums (open and closed) render as a <select> dropdown
            var emptySelected = string.IsNullOrEmpty(strDefault) ? " selected" : "";
            var options = string.Join("", enumValues.Select(v =>
            {
                var selected = v == strDefault ? " selected" : "";
                return $"<option value=\"{EscapeHtml(v)}\"{selected}>{EscapeHtml(v)}</option>";
            }));
            return $"<select id=\"{inputId}\" data-attr=\"{attr.Name}\" data-type=\"string\"><option value=\"\"{emptySelected}></option>{options}</select>";
        }

        // Plain text input
        return $"<input id=\"{inputId}\" type=\"text\" data-attr=\"{attr.Name}\" data-type=\"string\" value=\"{EscapeHtml(strDefault)}\">";
    }

    /// <summary>Parses quoted union type text (e.g. `'a' | 'b' | string`) and returns the literal values.</summary>
    static List<string> ExtractEnumValues(string typeText)
    {
        if (string.IsNullOrWhiteSpace(typeText)) return new List<string>();
        var values = new List<string>();
        foreach (var part in typeText.Split('|'))
        {
            var trimmed = part.Trim();
            if (trimmed.Length >= 2 && trimmed.StartsWith("'") && trimmed.EndsWith("'"))
                values.Add(trimmed.Substring(1, trimmed.Length - 2));
        }
        return values;
    }

    /// <summary>HTML-escapes a string for safe embedding in attribute values or text content.</summary>
    static string EscapeHtml(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    // attributes in CEM don't carry `default`; the matching field member does.
    static string ResolveDefault(CemAttribute attr, List<CemMember> members)
    {
        var member = members?.FirstOrDefault(m => m.Kind == "field" && m.Name == attr.Name);
        return member?.Default ?? attr.Default ?? "";
    }

    static string InferType(CemAttribute attr)
    {
        var typeStr = attr.Type?.Text?.ToLowerInvariant() ?? "string";
        if (typeStr.Contains("boolean") || typeStr == "bool") return "boolean";
        if (typeStr.Contains("number") || typeStr.Contains("int") || typeStr.Contains("float")) return "number";
        return "string";
    }

    static string BuildDefaultValue(CemAttribute attr, List<CemMember> members)
    {
        var raw = ResolveDefault(attr, members);
        var type = InferType(attr);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (type == "boolean") return raw.ToLower() is "true" or "1" ? "true" : "false";
            if (type == "number") return raw;
            return $"'{raw}'";
        }
        return type switch
        {
            "boolean" => "false",
            "number" => "0",
            _ => "''"
        };
    }

    // "beam-btn" → "Button", "beam-data-table" → "Data Table"
    static string TagNameToLabel(string tagName)
    {
        // strip a leading vendor prefix (everything up to and including the first hyphen)
        var withoutPrefix = tagName.Contains('-') ? tagName[(tagName.IndexOf('-') + 1)..] : tagName;
        // split on hyphens, title-case each word, join with spaces
        return string.Join(" ", withoutPrefix.Split('-').Select(TitleCase));
    }

    static string TitleCase(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];

    static string EscapeMarkdownCell(string value) =>
        value.Replace("\n", " ").Replace("|", "\\|").Trim();

    // Loads About/Notes snippets from {snippetsDir}/{tagName}.md, if it exists.
    static ComponentSnippet LoadComponentSnippet(string tagName, string snippetsDir)
    {
        if (string.IsNullOrWhiteSpace(snippetsDir)) return new ComponentSnippet(null, null);
        var filePath = Path.Combine(snippetsDir, $"{tagName}.md");
        if (!File.Exists(filePath)) return new ComponentSnippet(null, null);
        var content = File.ReadAllText(filePath);
        var sections = ParseSnippetSections(content);
        sections.TryGetValue("about", out var about);
        sections.TryGetValue("notes", out var notes);
        return new ComponentSnippet(about?.Trim(), notes?.Trim());
    }

    // Splits markdown on level-1 headings into a case-insensitive section dictionary.
    static Dictionary<string, string> ParseSnippetSections(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = content.Split('\n');
        string currentSection = null;
        var sectionContent = new StringBuilder();
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("# "))
            {
                if (currentSection != null)
                    result[currentSection] = sectionContent.ToString().Trim();
                currentSection = line.Substring(2).Trim();
                sectionContent.Clear();
            }
            else if (currentSection != null)
            {
                sectionContent.AppendLine(line);
            }
        }
        if (currentSection != null)
            result[currentSection] = sectionContent.ToString().Trim();
        return result;
    }
}

// ── Per-component supplemental docs (optional Docs/Components/{tagName}.md) ──

record ComponentSnippet(string About, string Notes);

// ── Custom Elements Manifest deserialization model ───────────────────────────
// Schema: https://github.com/webcomponents/custom-elements-manifest

public class CustomElementsManifest
{
    public string SchemaVersion { get; set; }
    public List<CemModule> Modules { get; set; }
}

public class CemModule
{
    public string Kind { get; set; }
    public string Path { get; set; }
    public List<CemDeclaration> Declarations { get; set; }
}

public class CemDeclaration
{
    public string Kind { get; set; }

    [JsonPropertyName("tagName")]
    public string TagName { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public List<CemAttribute> Attributes { get; set; }
    public List<CemMember> Members { get; set; }
    public List<CemSlot> Slots { get; set; }
    public List<CemEvent> Events { get; set; }
}

public class CemAttribute
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Default { get; set; }
    public CemType Type { get; set; }
}

public class CemMember
{
    public string Kind { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Default { get; set; }
    public CemType Type { get; set; }
}

public class CemType
{
    public string Text { get; set; }
}

public class CemSlot
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class CemEvent
{
    public string Name { get; set; }
    public string Description { get; set; }
}
