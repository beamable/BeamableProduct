using System.CodeDom.Compiler;
using System.Globalization;
using System.Text.RegularExpressions;
using Csv;

namespace cli.BackendCommands;

public class AuditRealmConfigCommandArgs : CommandArgs
{
    public string backendHome;

}

public class RealmConfigUsage
{
    public string projectName;
    public string filePath;
    public int index;
    public int line;
    public int column;
    public string configNamespaceExpression;
    public string configKeyExpression;
    public string configDefaultExpression;
    public string configMethodExpression;
}

public partial class AuditRealmConfigCommand : AppCommand<AuditRealmConfigCommandArgs>
{
    [GeneratedRegex("\\.getConfig(.*)\\((.*?),(.*?),(.*?)\\)")]
    public static partial Regex ConfigRegex();

    
    
    public AuditRealmConfigCommand() : base("audit-realm-config", "Get realm config data")
    {
    }

    public override void Configure()
    {
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);

    }

    public override async Task Handle(AuditRealmConfigCommandArgs args)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(args.backendHome);
        var list = BackendListToolsCommand.GatherToolList(args.backendHome);
        
        var coreUsageTask = FindScalaFiles("core", list.coreProjectPath);
        var toolUsageTasks = list.tools.Select(t => FindScalaFiles(t.name, t.projectPath));

        var usages = new List<RealmConfigUsage>();
        usages.AddRange(await coreUsageTask);
        
        foreach (var t in toolUsageTasks)
        {
            usages.AddRange(await t);
        }

        var missingNamespace = usages.Where(u => u.configNamespaceExpression.StartsWith("**")).ToList();
        var missingKey = usages.Where(u => u.configKeyExpression.StartsWith("**")).ToList();
        var missingDefault = usages.Where(u => u.configDefaultExpression.StartsWith("**")).ToList();


        var columns = new string[]
        {
            "project", "namespace", "key", "path", "line", "column", "type", "default", "isAlarming"
        };

        var rows = new List<string[]>();
        foreach (var usage in usages)
        {
            var isAlarming = usage.configNamespaceExpression.StartsWith("**")
                || usage.configKeyExpression.StartsWith("**")
                || usage.configDefaultExpression.StartsWith("**");
            rows.Add(new string[]
            {
                usage.projectName, usage.configNamespaceExpression, usage.configKeyExpression, 
                usage.filePath.Substring(args.backendHome.Length), 
                usage.line.ToString(),
                usage.column.ToString(),
                usage.configMethodExpression,
                usage.configDefaultExpression,
                isAlarming.ToString()
            });
        }

        var csv = CsvWriter.WriteToText(columns, rows);
        File.WriteAllText("realmConfigs.csv", csv);
    }

    public static async Task<List<RealmConfigUsage>> FindScalaFiles(string project, string directory)
    {
        var output = new List<RealmConfigUsage>();
        
        var tasks = Directory.EnumerateFiles(directory, "*.scala", SearchOption.AllDirectories)
            .Select(path => (path, File.ReadAllTextAsync(path)));
            
        foreach (var (path, task) in tasks)
        {
            var content = await task;

            var lineNumberIndexes = new List<int>();
            for (var i = 0; i < content.Length; i++)
            {
                var newLineIndex = content.IndexOf("\n", i, StringComparison.Ordinal);
                if (newLineIndex < 0) break;
                lineNumberIndexes.Add(newLineIndex);
                i = newLineIndex;
            }
            //
            
            var matches= ConfigRegex().Matches(content);
            var usages = new List<RealmConfigUsage>();
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];

                var line = 0;
                for (var j = 1; j < lineNumberIndexes.Count; j++)
                {
                    if (lineNumberIndexes[j] > match.Index)
                    {
                        line = j;
                        break;
                    }
                }
                
                usages.Add(new RealmConfigUsage
                {
                    projectName = project,
                    index = match.Index,
                    line = line + 1,
                    column = match.Index - lineNumberIndexes[line - 1],
                    filePath = path,
                    configMethodExpression = match.Groups[1].Value.Trim(),
                    configNamespaceExpression = Resolve("namespace", match.Groups[2].Value.Trim()),
                    configKeyExpression = Resolve("key", match.Groups[3].Value.Trim()),
                    configDefaultExpression = Resolve("default", match.Groups[4].Value.Trim()),
                });

                string Resolve(string variable, string expression)
                {
                    try
                    {
                        if (expression.StartsWith("\"")) return expression; // string literal is good!

                        if (bool.TryParse(expression, out _)) return expression;
                        if (int.TryParse(expression, out _)) return expression;
                        if (double.TryParse(expression, out _)) return expression;
                        
                        // maybe the variable was used
                        var namedRegex = new Regex(variable + "\\s+(:\\s+.*)?(:|=)(.*)");
                        var namedMatch = namedRegex.Match(expression);
                        if (namedMatch.Success)
                        {
                            return Resolve(variable, namedMatch.Groups[3].Value.Trim());
                        }

                        // uh oh, we don't know what this is...
                        // 1. maybe its in the current file?
                        var exprRegex = new Regex(expression + "\\s+=\\s+(.*)");
                        var maybeMatch = exprRegex.Match(content);
                        if (maybeMatch.Success)
                        {
                            return Resolve(variable, maybeMatch.Groups[1].Value.Trim());
                        }

                        return "**" + expression;
                    }
                    catch
                    {
                        return "**" + expression;
                    }
                }
            }

            lock (output)
            {
                output.AddRange(usages);
            }
        }

        return output;
    }
}