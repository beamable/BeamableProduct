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
            var snippet = LoadComponentSnippet(element.TagName, args.snippetsDirectory);
            if (snippet?.Config?.Disabled == true) continue;

            var fileName = $"{element.TagName}.md";
            var filePath = Path.Combine(args.outputDirectory, fileName);
            var markdown = RenderComponentDoc(element, snippet);
            await File.WriteAllTextAsync(filePath, markdown);
            result.generatedFiles.Add(filePath);
            Log.Information("Generated {File}", filePath);

            var label = snippet?.Config?.Title ?? TagNameToLabel(element.TagName);
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

        sb.AppendLine("---");
        sb.AppendLine("beam_components: true");
        sb.AppendLine("---");
        var pageTitle = snippet?.Config?.Title ?? TagNameToLabel(element.TagName);
        sb.AppendLine($"# {pageTitle}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(snippet?.About))
        {
            var aboutText = snippet.About.Replace("{{description}}", element.Description?.Trim() ?? "");
            sb.AppendLine(aboutText);
            sb.AppendLine();
        }
        else if (!string.IsNullOrWhiteSpace(element.Description))
        {
            sb.AppendLine(element.Description.Trim());
            sb.AppendLine();
        }

        sb.AppendLine(RenderInteractiveDemo(element, snippet));
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

        if (!string.IsNullOrWhiteSpace(snippet?.Notes))
        {
            sb.AppendLine("## Notes");
            sb.AppendLine();
            sb.AppendLine(snippet.Notes);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    static string RenderInteractiveDemo(CemDeclaration element, ComponentSnippet snippet = null)
    {
        var config = snippet?.Config;
        var componentStyle = snippet?.Style;
        var attrs = element.Attributes ?? new List<CemAttribute>();
        var hiddenSet = config?.Hidden != null
            ? new HashSet<string>(config.Hidden, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>();
        // Auto-hide properties that have explicit bindings (not editable via controls).
        if (config?.Bindings != null)
            foreach (var key in config.Bindings.Keys)
                hiddenSet.Add(key);
        var groupedAttrs = config?.Groups?.Values
            .SelectMany(v => v)
            .Select(a => a.ToLowerInvariant())
            .ToHashSet() ?? new HashSet<string>();
        var demoAttrs = attrs.Where(a => a.Name != null && a.Name != "dark" && a.Name != "light"
            && !hiddenSet.Contains(a.Name)
            && !groupedAttrs.Contains(a.Name.ToLowerInvariant())).ToList();
        var demoId = $"demo-{element.TagName}";
        var compId = $"comp-{element.TagName}";
        var previewId = $"preview-{element.TagName}";
        var vappId = $"vapp-{element.TagName}";
        var lightToggleId = $"light-toggle-{element.TagName}";
        var htmlSnippetId = $"html-{element.TagName}";
        var rawSlot = snippet?.SlotHtml ?? config?.Slot ?? TagNameToLabel(element.TagName);
        var previewSlot = string.Join(" ", rawSlot.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
        var slotJs = JsonSerializer.Serialize(rawSlot);

        var sb = new StringBuilder();

        // IMPORTANT: Python-Markdown type-6 HTML blocks terminate at the first blank line.
        // No blank lines and no 4-space-indented lines inside this block.
        sb.AppendLine("<div class=\"web-component-demo\">");
        if (!string.IsNullOrWhiteSpace(componentStyle))
        {
            var css = string.Join(" ", componentStyle.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
            sb.AppendLine($"<style>{css}</style>");
        }
        sb.AppendLine("<style>.web-component-demo .demo-copy-btn{opacity:0;transition:opacity .2s;background:var(--md-default-bg-color);border:none;color:var(--md-default-fg-color--light);cursor:pointer;padding:4px 5px;border-radius:3px;line-height:0;position:absolute;top:.4rem;right:.4rem;display:flex;align-items:center;justify-content:center}.web-component-demo .demo-snippet-wrap:hover .demo-copy-btn,.web-component-demo .demo-copy-btn:focus{opacity:1}.web-component-demo .demo-copy-btn:hover{color:var(--md-accent-fg-color)}.web-component-demo .demo-preview-wrap{position:relative;display:flex;flex-direction:column}.web-component-demo .demo-preview-overlay{position:absolute;top:.4rem;left:.5rem;right:.5rem;display:flex;align-items:center;justify-content:space-between;pointer-events:none}.web-component-demo .demo-theme-btn{pointer-events:auto;font-size:0.68em;font-family:var(--md-text-font,sans-serif);letter-spacing:.05em;text-transform:uppercase;opacity:.5;cursor:pointer;background:none;border:none;color:var(--md-default-fg-color);padding:0;line-height:1}.web-component-demo .demo-theme-btn:hover{opacity:.9}.web-component-demo .demo-preview-badge{font-size:0.68em;font-family:var(--md-text-font,sans-serif);letter-spacing:.05em;text-transform:uppercase;opacity:.5;color:var(--md-default-fg-color);line-height:1}.web-component-demo .v-application{background:transparent!important;overflow:visible!important;font-family:Rubik,sans-serif!important}.web-component-demo .v-application.theme--dark{color:#fff}.web-component-demo .v-application.theme--light{color:rgba(0,0,0,.87)}.v-dialog.theme--dark,.v-dialog.theme--dark .v-card{color:#fff;font-family:Rubik,sans-serif}.v-dialog.theme--light,.v-dialog.theme--light .v-card{color:rgba(0,0,0,.87);font-family:Rubik,sans-serif}.v-dialog .v-btn{color:inherit}.web-component-demo .v-application thead th,.web-component-demo .v-application tfoot th,.web-component-demo .v-application thead td,.web-component-demo .v-application tfoot td{--pico-font-weight:initial;--pico-border-width:initial}.web-component-demo .demo-controls>table{margin-top:0}.web-component-demo .demo-controls{--pico-form-element-spacing-vertical:.3rem;--pico-form-element-spacing-horizontal:.5rem}.web-component-demo .md-typeset__scrollwrap{margin:0!important;overflow:visible!important}.web-component-demo .md-typeset__table{display:block!important;width:100%!important;overflow:visible!important;padding:0!important}.web-component-demo .demo-controls table,.web-component-demo .demo-controls th,.web-component-demo .demo-controls td{border:0!important;box-shadow:none!important}.web-component-demo .demo-controls td:first-child{width:1%;white-space:nowrap}.web-component-demo .demo-controls select,.web-component-demo .demo-controls input:not([type=checkbox]){width:100%!important;box-sizing:border-box}.web-component-demo .v-application table{display:table!important;width:100%!important}.web-component-demo .v-application table:not([class]){font-size:inherit;background-color:transparent;border:none;border-radius:0;max-width:none;overflow:visible;display:table!important}.web-component-demo .v-application table:not([class]) th,.web-component-demo .v-application table:not([class]) td{min-width:auto!important;padding:0 16px!important;vertical-align:middle!important;border:none!important;background-color:transparent;box-shadow:none!important}.web-component-demo .v-application table:not([class]) th{font-size:.75rem;height:48px;user-select:none;white-space:nowrap}.web-component-demo .v-application table:not([class]) td{font-size:.875rem;height:48px}.web-component-demo .v-application .v-data-table{border-radius:4px}.web-component-demo .v-application .v-data-table th{font-weight:700!important;vertical-align:middle!important}.web-component-demo .v-application .v-data-table td{vertical-align:middle!important}.web-component-demo .v-application .v-alert .v-alert__content{color:inherit!important}.web-component-demo .beamable-theme.v-application .v-btn{color:inherit}.web-component-demo .beamable-theme.v-application .v-card__text{color:inherit}.web-component-demo .v-application .v-data-footer .v-btn .v-icon{color:inherit}.web-component-demo .v-application table th[role=columnheader]::after{content:none!important;display:none!important}.web-component-demo .demo-toggle{pointer-events:auto;display:flex;align-items:center;gap:0.4rem;cursor:pointer;margin:0}.web-component-demo .demo-toggle input{display:none}.web-component-demo .demo-toggle-slider{position:relative;width:28px;height:14px;background:#888;border-radius:7px;transition:background .25s}.web-component-demo .demo-toggle-slider::after{content:'';position:absolute;top:2px;left:2px;width:10px;height:10px;background:#fff;border-radius:50%;transition:transform .25s}.web-component-demo .demo-toggle input:checked+.demo-toggle-slider{background:var(--md-accent-fg-color,#51AEB9)}.web-component-demo .demo-toggle input:checked+.demo-toggle-slider::after{transform:translateX(14px)}.web-component-demo .demo-toggle-label{font-size:0.68em;font-family:var(--md-text-font,sans-serif);letter-spacing:.05em;text-transform:uppercase;opacity:.5;color:var(--md-default-fg-color);line-height:1;min-width:2.5em}</style>");
        // Two-column grid: left = preview + snippet, right = controls
        var previewWidth = string.IsNullOrWhiteSpace(config?.PreviewWidth) ? "60%" : config.PreviewWidth;
        var gridCols = $"{previewWidth} 1fr";
        sb.AppendLine($"<div id=\"{demoId}\" class=\"demo-wrapper\" style=\"display:grid;grid-template-columns:{gridCols};gap:1rem;\">");
        sb.AppendLine("<div style=\"min-height:150px;\">");
        // Preview: checkerboard background with dark/light toggle
        sb.AppendLine("<div class=\"demo-preview-wrap\" style=\"height:100%;\">");
        sb.AppendLine($"<div id=\"{previewId}\" class=\"demo-preview\" style=\"flex:1;padding:2.5rem;border-radius:6px;display:flex;align-items:center;justify-content:center;background:repeating-conic-gradient(rgba(200,200,220,0.1) 0% 25%,transparent 0% 50%) 0 0/20px 20px,#2a2a40;\">");
        // v-application wrapper: registerVuetifyLayoutElement (beam-card etc.) uses closest('.v-application') for theming
        sb.AppendLine($"<div id=\"{vappId}\" class=\"v-application v-application--is-ltr theme--dark beamable-theme\" data-app>");
        if (!string.IsNullOrWhiteSpace(snippet?.Preview))
        {
            var previewHtml = string.Join(" ", snippet.Preview.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
            sb.AppendLine(previewHtml);
        }
        var componentHtml = $"<{element.TagName} id=\"{compId}\" dark>{previewSlot}</{element.TagName}>";
        var wrapperTemplate = snippet?.Wrapper ?? "<beam-card elevation=\"4\" style=\"padding: 20px 40px\">\n    {{component}}\n</beam-card>";
        var wrappedHtml = wrapperTemplate.Replace("{{component}}", componentHtml);
        var wrappedOneLine = string.Join(" ", wrappedHtml.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
        sb.AppendLine(wrappedOneLine);
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"demo-preview-overlay\">");
        sb.AppendLine("<span class=\"demo-preview-badge\">preview</span>");
        sb.AppendLine($"<label class=\"demo-toggle\"><input type=\"checkbox\" id=\"{lightToggleId}\" checked><span class=\"demo-toggle-slider\"></span><span class=\"demo-toggle-label\">Dark</span></label>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        // Controls table: right column, scoped with .pico
        sb.AppendLine("<div class=\"demo-controls pico\" style=\"width:100%;\">");
        // Split attributes: non-booleans first in a table, booleans last in a two-column grid
        var nonBoolAttrs = demoAttrs.Where(a => InferType(a) != "boolean").ToList();
        var boolAttrs = demoAttrs.Where(a => InferType(a) == "boolean").ToList();
        sb.AppendLine("<table style=\"width:100%;\">");
        sb.AppendLine("<tbody>");

        foreach (var attr in nonBoolAttrs)
        {
            var inputId = $"{demoId}-{attr.Name}";
            var desc = EscapeHtml(attr.Description ?? "");
            string cfgDefault = null;
            config?.Defaults?.TryGetValue(attr.Name, out cfgDefault);
            var controlHtml = RenderControl(attr, inputId, element.Members, cfgDefault);
            sb.AppendLine($"<tr><td><code title=\"{desc}\">{EscapeHtml(attr.Name)}</code></td><td>{controlHtml}</td></tr>");
        }

        if (config?.Groups != null)
        {
            foreach (var (groupName, groupMembers) in config.Groups)
            {
                var groupLabel = TitleCase(groupName);
                var groupSelectId = $"{demoId}-group-{groupName}";
                string initialGroupVal = "";
                foreach (var ga in groupMembers)
                {
                    string d = null;
                    config.Defaults?.TryGetValue(ga, out d);
                    if (d?.ToLower() is "true" or "1") { initialGroupVal = ga; break; }
                }
                var emptySelected = string.IsNullOrEmpty(initialGroupVal) ? " selected" : "";
                var groupOptions = string.Join("", groupMembers.Select(ga =>
                {
                    var sel = ga == initialGroupVal ? " selected" : "";
                    return $"<option value=\"{EscapeHtml(ga)}\"{sel}>{EscapeHtml(ga)}</option>";
                }));
                var groupAttrsEsc = EscapeHtml(string.Join(",", groupMembers));
                sb.AppendLine($"<tr><td><code>{EscapeHtml(groupLabel)}</code></td><td><select id=\"{groupSelectId}\" data-group=\"{EscapeHtml(groupName)}\" data-group-attrs=\"{groupAttrsEsc}\"><option value=\"\"{emptySelected}>&lt;none&gt;</option>{groupOptions}</select></td></tr>");
            }
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        // Boolean attributes: two-column grid of checkboxes
        if (boolAttrs.Count > 0)
        {
            sb.AppendLine("<div class=\"demo-bool-grid\" style=\"display:grid;grid-template-columns:1fr 1fr;gap:0 .75rem;margin-top:.25rem;\">");
            foreach (var attr in boolAttrs)
            {
                var inputId = $"{demoId}-{attr.Name}";
                var desc = EscapeHtml(attr.Description ?? "");
                string cfgDefault = null;
                config?.Defaults?.TryGetValue(attr.Name, out cfgDefault);
                var controlHtml = RenderControl(attr, inputId, element.Members, cfgDefault);
                sb.AppendLine($"<label title=\"{desc}\" style=\"display:flex;align-items:center;gap:.35rem;font-size:.85em;padding:.15rem 0;cursor:pointer;\">{controlHtml}<code style=\"font-size:.95em;\">{EscapeHtml(attr.Name)}</code></label>");
            }
            sb.AppendLine("</div>");
        }
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        // HTML snippet: full-width below the two-column grid
        sb.AppendLine("<div class=\"demo-snippet-wrap\" style=\"position:relative;margin-top:.75rem;\">");
        sb.AppendLine($"<pre id=\"{htmlSnippetId}\" style=\"margin:0;padding:0.75rem 2.5rem 0.75rem 0.75rem;background:var(--md-code-bg-color,#1e1e2e);color:var(--md-code-fg-color,#cdd6f4);border-radius:4px;overflow-x:auto;font-size:0.85em;font-family:var(--md-code-font,monospace);line-height:1.6;white-space:pre;\"></pre>");
        sb.AppendLine($"<button class=\"demo-copy-btn\" id=\"copy-{element.TagName}\" title=\"Copy\"><svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"currentColor\"><path d=\"M19 21H8V7h11m0-2H8a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2m-3-4H4a2 2 0 0 0-2 2v14h2V3h12V1z\"/></svg></button>");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>");
        sb.AppendLine("(function () {");
        sb.AppendLine($"  var comp = document.getElementById('{compId}');");
        sb.AppendLine($"  var wrapper = document.getElementById('{demoId}');");
        sb.AppendLine($"  var htmlSnippet = document.getElementById('{htmlSnippetId}');");
        sb.AppendLine($"  var copyBtn = document.getElementById('copy-{element.TagName}');");
        sb.AppendLine("  if (!comp || !wrapper) return;");
        // Neutralize MkDocs .md-typeset :not([class]) selectors by adding a class
        // to every unclassed element inside the component preview area.
        sb.AppendLine("  function tagUnclassed(root) { root.querySelectorAll(':not([class])').forEach(function(el) { if (el.nodeType === 1 && el.tagName !== 'STYLE' && el.tagName !== 'SCRIPT') el.classList.add('beam-el'); }); }");
        sb.AppendLine("  tagUnclassed(comp);");
        sb.AppendLine("  new MutationObserver(function() { tagUnclassed(comp); }).observe(comp, { childList: true, subtree: true });");
        // Property bindings — values that are set via JS property assignment
        // (not HTML attributes) because they may be arrays, objects, etc.
        // Applied twice: once as own properties before the custom element upgrades
        // (for getPropsData to pick up), and again on vce-ready after the Vue
        // instance exists (reactive property forwarders are in place by then).
        if (config?.Bindings is { Count: > 0 })
        {
            var bindingsObj = new Dictionary<string, string>();
            foreach (var (prop, value) in config.Bindings)
            {
                var jsValue = JsonSerializer.Serialize(value);
                bindingsObj[prop] = jsValue;
                // Pre-upgrade: set as own property for getPropsData
                sb.AppendLine($"  comp[{JsonSerializer.Serialize(prop)}] = {jsValue};");
            }
            // Post-upgrade: re-apply via reactive property setters
            sb.AppendLine("  comp.addEventListener('vce-ready', function() {");
            foreach (var (prop, jsVal) in bindingsObj)
            {
                sb.AppendLine($"    comp[{JsonSerializer.Serialize(prop)}] = {jsVal};");
            }
            sb.AppendLine("  });");
            // Expose bindings for buildHtml() to include in the snippet
            var bindingsJson = "{" + string.Join(",", bindingsObj.Select(kv =>
                JsonSerializer.Serialize(kv.Key) + ":" + kv.Value)) + "}";
            sb.AppendLine($"  var _bindings = {bindingsJson};");
        }
        else
        {
            sb.AppendLine("  var _bindings = {};");
        }
        // Sync $vuetify.theme.dark at startup to match the dark attribute
        sb.AppendLine("  comp.addEventListener('vce-ready', function() {");
        sb.AppendLine("    var vm = comp.getVueInstance ? comp.getVueInstance() : null;");
        sb.AppendLine("    if (vm && vm.$vuetify) vm.$vuetify.theme.dark = true;");
        sb.AppendLine("  });");
        sb.AppendLine($"  var _slot = {slotJs};");
        // buildHtml: reads [data-attr] inputs (avoids internal attrs like id/vce-ready).
        // Format: <tag first-attr\n<pad>second-attr>\n  content\n</tag>
        sb.AppendLine("  function buildHtml() {");
        sb.AppendLine($"    var tag = '{element.TagName}';");
        sb.AppendLine("    var inputs = wrapper.querySelectorAll('[data-attr]');");
        sb.AppendLine("    var attrList = [];");
        sb.AppendLine("    inputs.forEach(function(input) {");
        sb.AppendLine("      var attr = input.getAttribute('data-attr');");
        sb.AppendLine("      var type = input.getAttribute('data-type');");
        sb.AppendLine("      if (type === 'boolean') { if (input.checked) attrList.push(attr); }");
        sb.AppendLine("      else { if (input.value !== '') attrList.push(attr + '=\"' + input.value + '\"'); }");
        sb.AppendLine("    });");
        sb.AppendLine("    wrapper.querySelectorAll('select[data-group]').forEach(function(sel) {");
        sb.AppendLine("      if (sel.value !== '') attrList.push(sel.value);");
        sb.AppendLine("    });");
        // Include property bindings in the HTML snippet as attributes.
        // For non-string values (arrays, objects), JSON.stringify produces the attribute value.
        sb.AppendLine("    for (var bk in _bindings) {");
        sb.AppendLine("      var bv = _bindings[bk];");
        sb.AppendLine("      if (typeof bv === 'string') attrList.push(bk + '=\"' + bv + '\"');");
        sb.AppendLine("      else {");
        sb.AppendLine("        var json = JSON.stringify(bv, null, 2);");
        sb.AppendLine("        var innerPad = ' '.repeat(tag.length + 2 + bk.length + 2);");
        sb.AppendLine("        attrList.push(bk + '={' + json.replace(/\\n/g, '\\n' + innerPad) + '}');");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("    var multiLine = _slot.indexOf('\\n') !== -1;");
        sb.AppendLine("    var indented = '  ' + _slot.replace(/\\n/g, '\\n  ');");
        sb.AppendLine("    if (attrList.length === 0) {");
        sb.AppendLine("      if (!multiLine) return '<' + tag + '>' + _slot + '</' + tag + '>';");
        sb.AppendLine("      return '<' + tag + '>\\n' + indented + '\\n</' + tag + '>';");
        sb.AppendLine("    }");
        sb.AppendLine("    var pad = ' '.repeat(tag.length + 2);");
        sb.AppendLine("    return '<' + tag + ' ' + attrList.join('\\n' + pad) + '>\\n' + indented + '\\n</' + tag + '>';");
        sb.AppendLine("  }");
        // highlightHtml: syntax-colours using MkDocs Material CSS vars.
        // Lines joined with &#10; (numeric LF entity) so innerHTML reliably renders newlines in <pre>.
        sb.AppendLine("  function highlightHtml(raw) {");
        sb.AppendLine("    var KW = 'color:var(--md-code-hl-keyword-color)';");
        sb.AppendLine("    var AT = 'color:rgb(63,110,198)';");
        sb.AppendLine("    var ST = 'color:var(--md-code-hl-string-color)';");
        sb.AppendLine("    var PT = 'color:var(--md-code-hl-punctuation-color)';");
        sb.AppendLine("    function sp(c, t) { return '<span style=\"' + c + '\">' + t + '</span>'; }");
        sb.AppendLine("    function esc(s) { return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/\"/g,'&quot;'); }");
        sb.AppendLine("    function hlAttr(a) {");
        sb.AppendLine("      var m;");
        sb.AppendLine("      if ((m = a.match(/^([\\w-]+)=\"(.*)\"$/))) return sp(AT, m[1]) + sp(PT, '=') + sp(ST, '&quot;' + esc(m[2]) + '&quot;');");
        sb.AppendLine("      if ((m = a.match(/^([\\w-]+)=\\{(.*)\\}$/))) return sp(AT, m[1]) + sp(PT, '={') + sp(ST, esc(m[2])) + sp(PT, '}');");
        sb.AppendLine("      if ((m = a.match(/^([\\w-]+)$/))) return sp(AT, m[1]);");
        sb.AppendLine("      return esc(a);");
        sb.AppendLine("    }");
        sb.AppendLine("    function hlAttrs(s) {");
        sb.AppendLine("      var parts = [], m, openBinding = false, depth = 0;");
        sb.AppendLine("      var r = s.trim();");
        sb.AppendLine("      while (r.length) {");
        sb.AppendLine("        if ((m = r.match(/^([\\w-]+)=\"([^\"]*)\"\\s*/))) { parts.push(hlAttr(m[1] + '=\"' + m[2] + '\"')); r = r.slice(m[0].length); }");
        sb.AppendLine("        else if ((m = r.match(/^([\\w-]+)=\\{([^}]*)\\}\\s*/))) { parts.push(hlAttr(m[1] + '={' + m[2] + '}')); r = r.slice(m[0].length); }");
        sb.AppendLine("        else if ((m = r.match(/^([\\w-]+)=\\{(.*)$/))) {");
        sb.AppendLine("          var tail = m[2];");
        sb.AppendLine("          depth = 1;");
        sb.AppendLine("          for (var ci = 0; ci < tail.length; ci++) { if (tail[ci]==='{') depth++; else if (tail[ci]==='}') depth--; }");
        sb.AppendLine("          parts.push(sp(AT, m[1]) + sp(PT, '={') + sp(ST, esc(tail)));");
        sb.AppendLine("          openBinding = true; break;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if ((m = r.match(/^([\\w-]+)\\s*/))) { parts.push(hlAttr(m[1])); r = r.slice(m[0].length); }");
        sb.AppendLine("        else break;");
        sb.AppendLine("      }");
        sb.AppendLine("      return { html: parts.join(' '), openBinding: openBinding, depth: depth };");
        sb.AppendLine("    }");
        sb.AppendLine("    var lines = raw.split('\\n'), result = [], inTag = false, inBinding = false, bindDepth = 0, m;");
        sb.AppendLine("    for (var i = 0; i < lines.length; i++) {");
        sb.AppendLine("      var L = lines[i];");
        sb.AppendLine("      var indent = L.match(/^(\\s*)/)[1];");
        sb.AppendLine("      var T = L.trimStart();");
        sb.AppendLine("      if (inBinding) {");
        sb.AppendLine("        for (var ci = 0; ci < T.length; ci++) { if (T[ci]==='{') bindDepth++; else if (T[ci]==='}') bindDepth--; }");
        sb.AppendLine("        if (bindDepth <= 0) {");
        sb.AppendLine("          inBinding = false;");
        sb.AppendLine("          var lastBrace = T.lastIndexOf('}');");
        sb.AppendLine("          var before = T.slice(0, lastBrace);");
        sb.AppendLine("          var after = T.slice(lastBrace + 1);");
        sb.AppendLine("          var cl = after.trimEnd().slice(-1) === '>';");
        sb.AppendLine("          result.push(indent + sp(ST, esc(before)) + sp(PT, '}') + (cl ? sp(PT,'&gt;') : esc(after)));");
        sb.AppendLine("          if (cl) inTag = false;");
        sb.AppendLine("        } else { result.push(indent + sp(ST, esc(T))); }");
        sb.AppendLine("      } else if (inTag) {");
        sb.AppendLine("        var cl = T.trimEnd().slice(-1) === '>';");
        sb.AppendLine("        var body = cl ? T.trimEnd().slice(0,-1).trim() : T.trim();");
        sb.AppendLine("        var ha = hlAttrs(body);");
        sb.AppendLine("        result.push(indent + ha.html + (cl ? sp(PT,'&gt;') : ''));");
        sb.AppendLine("        if (ha.openBinding) { inBinding = true; bindDepth = ha.depth; }");
        sb.AppendLine("        else if (cl) inTag = false;");
        sb.AppendLine("      } else if ((m = T.match(/^<\\/([\\w-]+)>(.*)$/))) {");
        sb.AppendLine("        result.push(indent + sp(PT,'&lt;/') + sp(KW,m[1]) + sp(PT,'&gt;') + esc(m[2]));");
        sb.AppendLine("      } else if ((m = T.match(/^<([\\w-]+)(.*)$/))) {");
        sb.AppendLine("        var tag = m[1], rest = m[2];");
        sb.AppendLine("        var open = sp(PT,'&lt;') + sp(KW,tag);");
        sb.AppendLine("        if (rest === '') { result.push(indent + open); inTag = true; }");
        sb.AppendLine("        else if (rest.charAt(0) === '>') {");
        sb.AppendLine("          var innerM = rest.match(/^>(.*?)<\\/([\\w-]+)>$/);");
        sb.AppendLine("          if (innerM) result.push(indent + open + sp(PT,'&gt;') + esc(innerM[1]) + sp(PT,'&lt;/') + sp(KW,innerM[2]) + sp(PT,'&gt;'));");
        sb.AppendLine("          else result.push(indent + open + sp(PT,'&gt;') + esc(rest.slice(1)));");
        sb.AppendLine("        } else {");
        sb.AppendLine("          var fullM = rest.match(/^\\s+(.*?)>(.*)<\\/([\\w-]+)>$/);");
        sb.AppendLine("          if (fullM) {");
        sb.AppendLine("            result.push(indent + open + ' ' + hlAttrs(fullM[1]).html + sp(PT,'&gt;') + esc(fullM[2]) + sp(PT,'&lt;/') + sp(KW,fullM[3]) + sp(PT,'&gt;'));");
        sb.AppendLine("          } else {");
        sb.AppendLine("            var cl = rest.trimEnd().slice(-1) === '>';");
        sb.AppendLine("            var at = cl ? rest.trimEnd().slice(0,-1).trim() : rest.trim();");
        sb.AppendLine("            var ha = hlAttrs(at);");
        sb.AppendLine("            result.push(indent + open + (at ? ' ' + ha.html : '') + (cl ? sp(PT,'&gt;') : ''));");
        sb.AppendLine("            if (ha.openBinding) { inTag = true; inBinding = true; bindDepth = ha.depth; }");
        sb.AppendLine("            else if (!cl) inTag = true;");
        sb.AppendLine("          }");
        sb.AppendLine("        }");
        sb.AppendLine("      } else { result.push(indent + esc(T)); }");
        sb.AppendLine("    }");
        sb.AppendLine("    return result.join('&#10;');");
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
        sb.AppendLine("  function applyGroup(sel) {");
        sb.AppendLine("    var groupAttrs = sel.dataset.groupAttrs.split(',');");
        sb.AppendLine("    groupAttrs.forEach(function(a) { comp.removeAttribute(a); });");
        sb.AppendLine("    if (sel.value !== '') comp.setAttribute(sel.value, '');");
        sb.AppendLine("    updateSnippet();");
        sb.AppendLine("  }");
        sb.AppendLine("  wrapper.querySelectorAll('select[data-group]').forEach(function(sel) {");
        sb.AppendLine("    applyGroup(sel);");
        sb.AppendLine("    sel.addEventListener('change', function() { applyGroup(sel); });");
        sb.AppendLine("  });");
        sb.AppendLine("  updateSnippet();");
        // Light/dark toggle (defaults to dark)
        sb.AppendLine($"  var lightToggle = document.getElementById('{lightToggleId}');");
        sb.AppendLine($"  var previewEl = document.getElementById('{previewId}');");
        sb.AppendLine($"  var vappEl = document.getElementById('{vappId}');");
        sb.AppendLine("  var _isDark = true;");
        sb.AppendLine("  var _darkBg = 'repeating-conic-gradient(rgba(200,200,220,0.1) 0% 25%,transparent 0% 50%) 0 0/20px 20px,#2a2a40';");
        sb.AppendLine("  var _lightBg = 'repeating-conic-gradient(rgba(128,128,128,0.07) 0% 25%,transparent 0% 50%) 0 0/20px 20px,var(--md-code-bg-color,#1e1e2e)';");
        sb.AppendLine("  var _toggleLabel = lightToggle ? lightToggle.parentElement.querySelector('.demo-toggle-label') : null;");
        sb.AppendLine("  if (lightToggle) {");
        sb.AppendLine("    lightToggle.addEventListener('change', function () {");
        sb.AppendLine("      _isDark = lightToggle.checked;");
        // set/remove dark attribute (for Vuetify shadow-DOM components)
        sb.AppendLine("      if (_isDark) { comp.setAttribute('dark',''); } else { comp.removeAttribute('dark'); }");
        // toggle v-application theme class (for light-DOM layout components)
        sb.AppendLine("      if (vappEl) { vappEl.classList.toggle('theme--dark',_isDark); vappEl.classList.toggle('theme--light',!_isDark); }");
        // set $vuetify.theme.dark on the actual component instance (not the root wrapper) so beamThemeProvider cascades to all children
        sb.AppendLine("      var vm = comp.getVueInstance ? comp.getVueInstance() : null; if (vm && vm.$vuetify) vm.$vuetify.theme.dark = _isDark;");
        sb.AppendLine("      if (previewEl) previewEl.style.background = _isDark ? _darkBg : _lightBg;");
        sb.AppendLine("      if (_toggleLabel) _toggleLabel.textContent = _isDark ? 'Dark' : 'Light';");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        // Copy button — swap SVG to checkmark on success, restore after 1.5s
        sb.AppendLine("  var _copySvg = '<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"currentColor\"><path d=\"M19 21H8V7h11m0-2H8a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2m-3-4H4a2 2 0 0 0-2 2v14h2V3h12V1z\"/></svg>';");
        sb.AppendLine("  var _checkSvg = '<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"currentColor\"><path d=\"M21 7 9 19l-5.5-5.5 1.41-1.41L9 16.17 19.59 5.59 21 7z\"/></svg>';");
        sb.AppendLine("  if (copyBtn) {");
        sb.AppendLine("    copyBtn.addEventListener('click', function () {");
        sb.AppendLine("      var text = buildHtml();");
        sb.AppendLine("      function showCheck() { copyBtn.innerHTML = _checkSvg; setTimeout(function () { copyBtn.innerHTML = _copySvg; }, 1500); }");
        sb.AppendLine("      if (navigator.clipboard) {");
        sb.AppendLine("        navigator.clipboard.writeText(text).then(showCheck);");
        sb.AppendLine("      } else {");
        sb.AppendLine("        var ta = document.createElement('textarea');");
        sb.AppendLine("        ta.value = text; document.body.appendChild(ta); ta.select();");
        sb.AppendLine("        document.execCommand('copy'); document.body.removeChild(ta);");
        sb.AppendLine("        showCheck();");
        sb.AppendLine("      }");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        if (!string.IsNullOrWhiteSpace(snippet?.Script))
        {
            foreach (var line in snippet.Script.Split('\n'))
                sb.AppendLine($"  {line.TrimEnd('\r')}");
        }
        sb.AppendLine("})();");
        sb.AppendLine("</script>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    /// <summary>Renders the appropriate HTML control for a CEM attribute.</summary>
    static string RenderControl(CemAttribute attr, string inputId, List<CemMember> members, string configDefault = null)
    {
        var typeText = attr.Type?.Text ?? "string";
        var defaultVal = BuildDefaultValue(attr, members);
        var inferredType = InferType(attr);
        // Config defaults override CEM defaults
        if (configDefault != null)
        {
            defaultVal = inferredType == "boolean"
                ? (configDefault.ToLower() is "true" or "1" ? "true" : "false")
                : configDefault;
        }

        if (inferredType == "boolean")
        {
            var checkedAttr = defaultVal == "true" ? " checked" : "";
            return $"<input id=\"{inputId}\" type=\"checkbox\" data-attr=\"{attr.Name}\" data-type=\"boolean\"{checkedAttr}>";
        }

        if (inferredType == "number")
        {
            var numDefault = string.IsNullOrWhiteSpace(defaultVal) ? "" : defaultVal;
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
            return $"<select id=\"{inputId}\" data-attr=\"{attr.Name}\" data-type=\"string\"><option value=\"\"{emptySelected}>&lt;none&gt;</option>{options}</select>";
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
            "number" => "",
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

    // Loads snippet sections from {snippetsDir}/{tagName}.md, if it exists.
    static ComponentSnippet LoadComponentSnippet(string tagName, string snippetsDir)
    {
        if (string.IsNullOrWhiteSpace(snippetsDir)) return new ComponentSnippet(null, null);
        var filePath = Path.Combine(snippetsDir, $"{tagName}.md");
        if (!File.Exists(filePath)) return new ComponentSnippet(null, null);
        var content = File.ReadAllText(filePath);
        var sections = ParseSnippetSections(content);
        sections.TryGetValue("about", out var about);
        sections.TryGetValue("notes", out var notes);
        sections.TryGetValue("style", out var style);
        sections.TryGetValue("slot", out var slotHtml);
        sections.TryGetValue("preview", out var preview);
        sections.TryGetValue("script", out var script);
        sections.TryGetValue("wrapper", out var wrapper);
        ComponentConfig config = null;
        if (sections.TryGetValue("config", out var configJson) && !string.IsNullOrWhiteSpace(configJson))
        {
            try { config = JsonSerializer.Deserialize<ComponentConfig>(configJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { /* ignore malformed config */ }
        }
        return new ComponentSnippet(about?.Trim(), notes?.Trim(), config, style?.Trim(), slotHtml?.Trim(), preview?.Trim(), script?.Trim(), wrapper?.Trim());
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

record ComponentSnippet(string About, string Notes, ComponentConfig Config = null, string Style = null, string SlotHtml = null, string Preview = null, string Script = null, string Wrapper = null);

class ComponentConfig
{
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("hidden")]
    public List<string> Hidden { get; set; } = new();
    [JsonPropertyName("defaults")]
    public Dictionary<string, string> Defaults { get; set; } = new();
    [JsonPropertyName("slot")]
    public string Slot { get; set; }
    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }
    [JsonPropertyName("groups")]
    public Dictionary<string, List<string>> Groups { get; set; } = new();
    [JsonPropertyName("bindings")]
    public Dictionary<string, JsonElement> Bindings { get; set; } = new();
    [JsonPropertyName("preview-width")]
    public string PreviewWidth { get; set; }
}

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
