using System.CommandLine;
using System.Text;
using Beamable.Common.BeamCli;
using Beamable.Server;
using Microsoft.Build.Evaluation;

namespace cli.CheckCommands;

public interface IChecksArgs
{
    
}

[Serializable]
public class CreateChecksCommandArgs : CommandArgs, IChecksArgs
{
    public List<string> autoFixFilters = new List<string>();
}

[Serializable]
public class CheckResultsForBeamoId
{
    public string beamoId;
    public List<RequiredFileEdit> fileEdits = new List<RequiredFileEdit>();
}

[Serializable]
public class CheckFixedResult
{
    public string code;
    public string status;
}

[Serializable]
public class CheckFixedResultChannel : IResultChannel
{
    public string ChannelName => "fixed";
}

public class CreateChecksCommand : StreamCommand<CreateChecksCommandArgs, CheckResultsForBeamoId>
                                 , IResultSteam<CheckFixedResultChannel, CheckFixedResult>
{
    public override bool AutoLogOutput => true;

    public CreateChecksCommand() : base("scan", "Scan the workspace for known issues")
    {
    }

    public override void Configure()
    {
        AddOption(new Option<List<string>>(new string[]{"--fix", "-f"}, "Automatically fix known issues for the given code prefixes, or * to fix everything ")
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        }, (args, i) => args.autoFixFilters = i);
    }

    public override Task Handle(CreateChecksCommandArgs args)
    {
        var (checks, fileCache) = ComputeChecks(args);
        foreach (var check in checks)
        {
            if (check.fileEdits.Count == 0) continue; // don't noise-up the world.
            SendResults(check);
        }
        
        var acceptAll = args.autoFixFilters.Any(x => x == "*" || x == "all");
        foreach (var project in checks)
        {
            foreach (var check in project.fileEdits)
            {
                var code = check.code;
                var shouldFix = acceptAll || args.autoFixFilters.Any(f => code.StartsWith(f));
                if (!shouldFix) continue;
                
                Log.Information($"Performing fix for {project.beamoId}. {check.title}");
                ((IResultSteam<CheckFixedResultChannel, CheckFixedResult>)this).SendResults(new CheckFixedResult
                {
                    code = code,
                    status = "done"
                });
                fileCache[check.filePath] = check.Apply(fileCache[check.filePath]);
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// A set of functions that should be applied to all csproj files
    /// </summary>
    private static readonly ProjectFileEditFunction[] ProjectFunctions = new ProjectFileEditFunction[]
    {
        EnsureAtLeastNet8,
        EnsureUsingLatestMongo,
        EnsureDockerIgnoreAllowsBeamApp,
        EnsureNoUsingSerilogCommands,
        MapLobbyFederationArg
    };
    
    public static (List<CheckResultsForBeamoId>, FileCache) ComputeChecks<TArgs>(TArgs args) where TArgs : CommandArgs, IChecksArgs
    {
        var results = new List<CheckResultsForBeamoId>();
        var manifest = args.BeamoLocalSystem.BeamoManifest;
        var cache = new FileCache();
        
        foreach (var (beamoId, http) in manifest.HttpMicroserviceLocalProtocols)
        {
            HandleProject(beamoId, http.Metadata.msbuildProject);
        }
        
        foreach (var (beamoId, http) in manifest.EmbeddedMongoDbLocalProtocols)
        {
            HandleProject(beamoId, http.Metadata.msbuildProject);
        }

        void HandleProject(string beamoId, Project project)
        {
            var projectResults = new CheckResultsForBeamoId
            {
                beamoId = beamoId
            };

            var (allText, lineNumberToIndex, lines) = ReadLineNumbers(project.FullPath, cache);
            cache[project.FullPath] = allText;
            foreach (var projectFunction in ProjectFunctions)
            {
                var edits = projectFunction(beamoId, lineNumberToIndex, lines, project, cache);
                if (edits != null)
                {
                    foreach (var edit in edits)
                    {
                        if (edit == null) continue;
                        projectResults.fileEdits.Add(edit);
                    }
                }
            }

            // sort the edits backwards to forwards so that edits non-destructively. 
            projectResults.fileEdits.Sort((a, b) => b.startIndex.CompareTo(a.startIndex));

            if (projectResults.fileEdits.Count > 0)
            {
                results.Add(projectResults);
            }
        }
        
        return (results, cache);
    }

    private static Dictionary<string, (string allText, Dictionary<int, int> lineNumberToIndex, List<string> lines)>
        _memoizationTable =
            new Dictionary<string, (string allText, Dictionary<int, int> lineNumberToIndex, List<string> lines)>();
    
    static (string allText, Dictionary<int, int> lineNumberToIndex, List<string> lines) ReadLineNumbers(string path, FileCache cache)
    {
        if (_memoizationTable.TryGetValue(path, out var existing))
        {
            return existing;
        }
        var allText = File.ReadAllText(path);
        cache.Add(path, allText);
        var lineNumberToStringIndex = new Dictionary<int, int>
        {
            [0] = 0
        };
        var lines = new List<string>();
        var lineNumber = 0;
        var buffer = new StringBuilder();
        for (var i = 0; i < allText.Length - 1; i++)
        {
            var c = allText[i];
            var nc = allText[i + 1];
            switch (c)
            {
                case '\r' when nc == '\n':
                    lineNumber++;
                    lineNumberToStringIndex[lineNumber] = i; // do we need to +1 here for the extra character?
                    i++;
                    lines.Add(buffer.ToString());
                    buffer.Clear();
                    break;
                case '\n':
                    lineNumber++;
                    lineNumberToStringIndex[lineNumber] = i; 
                    lines.Add(buffer.ToString());
                    buffer.Clear();
                    break;
                default:
                    buffer.Append(c);
                    break;
            }
        }

        if (allText.Length > 0)
        {
            buffer.Append(allText[^1]);
            lines.Add(buffer.ToString());
        }

        _memoizationTable[path] = (allText, lineNumberToStringIndex, lines);
        return (allText, lineNumberToStringIndex, lines);
    }

    static List<RequiredFileEdit> MapLobbyFederationArg(string beamoId, Dictionary<int, int> _, List<string> __,
        Project project, FileCache cache)
    {
        //Promise<ServerInfo> CreateGameServer(Lobby
        var sources = project.GetItems("Compile");
        var edits = new List<RequiredFileEdit>();
        foreach (var source in sources)
        {
            var path = source.GetMetadataValue("FullPath");
            if (string.IsNullOrEmpty(path)) continue;
            var (allText, lineNumberToIndex, lines) = ReadLineNumbers(path, cache);

            const string oldNamespaceRef = "using Beamable.Experimental.Api.Lobbies;";
            const string newNamespaceRef = "using Beamable.Api.Autogenerated.Models;";
            
            const string offensiveText = "Promise<ServerInfo> CreateGameServer(Lobby ";
            const string lessOffensiveText = "Promise<ServerInfo> CreateGameServer([Parameter(\"lobby\")] Beamable.Api.Autogenerated.Models.Lobby autoGenerated"; // rename the variable too

            var hasOldNamespace = false;
            var hasNewNamespace = false;

            var needToReplaceOpenBracket = false;
            string oldVariableName = null;

            for (var lineIndex = 0 ; lineIndex < lines.Count - 1; lineIndex ++)
            {
                var line = lines[lineIndex];

                if (needToReplaceOpenBracket)
                {
                    if (line.Contains("{"))
                    {
                        needToReplaceOpenBracket = false;
                        // replace this with the jazz
                        var bracketEdit = new RequiredFileEdit
                        {
                            filePath = path,
                            code = $"{beamoId}_updateGameServerFederationArg_helper",
                            beamoId = beamoId,
                            title = "Use Autogenerated Lobby Type Helper",
                            description =
                                "As of CLI 5.0.0, GameServer federation uses the auto-generated Lobby type instead of the old one. ",
                            replacementText = $"{line}{Environment.NewLine}\t\t\tvar {oldVariableName} = autoGenerated{oldVariableName}.ConvertLobbyType();"
                        };
                        bracketEdit.SetLocationAsLineReplacement(lineNumberToIndex, lineIndex);
                        edits.Add(bracketEdit);
                    }
                }
                
                
                hasOldNamespace |= line.StartsWith(oldNamespaceRef, StringComparison.InvariantCulture);
                hasNewNamespace |= line.StartsWith(newNamespaceRef, StringComparison.InvariantCulture);
                
                var index = line.IndexOf(offensiveText, StringComparison.InvariantCultureIgnoreCase);
                if (index < 0) continue;
                
                if (hasNewNamespace)
                {
                    // the Lobby reference is pointing towards the right type now
                    continue;
                }
                
                // identify what the old variable was called by reading until the close paren
                var closingIndex = line.IndexOf(")", index, StringComparison.InvariantCulture);
                oldVariableName = line.Substring(index + offensiveText.Length,
                    closingIndex - (index + offensiveText.Length));

                var sb = new StringBuilder();
                sb.Append(line.Substring(0, index));
                sb.Append(lessOffensiveText);
                sb.Append(line.Substring(index + offensiveText.Length));
                
                if (line.Contains("{"))
                {
                    sb.AppendLine();
                    sb.AppendLine($"\t\t\tvar {oldVariableName} = autoGenerated{oldVariableName}.ConvertLobbyType();");
                }
                else
                {
                    needToReplaceOpenBracket = true;
                }

                var newSnippet = sb.ToString();
               
                
                var edit = new RequiredFileEdit
                {
                    filePath = path,
                    code = $"{beamoId}_updateGameServerFederationArg",
                    beamoId = beamoId,
                    title = "Use Autogenerated Lobby Type",
                    description =
                        "As of CLI 5.0.0, GameServer federation uses the auto-generated Lobby type instead of the old one. ",
                    replacementText = newSnippet
                };
                edit.SetLocationAsLineReplacement(lineNumberToIndex, lineIndex);
                edits.Add(edit);
            }
        }

        return edits;

        
    }

    static List<RequiredFileEdit> EnsureNoUsingSerilogCommands(string beamoId, Dictionary<int, int> _, List<string> __,
        Project project, FileCache cache)
    {
        var sources = project.GetItems("Compile");
        var edits = new List<RequiredFileEdit>();
        foreach (var source in sources)
        {
            var path = source.GetMetadataValue("FullPath");
            if (string.IsNullOrEmpty(path)) continue;
            var (allText, lineNumberToIndex, lines) = ReadLineNumbers(path, cache);

            // foreach (var line in lines)
            for (var lineIndex = 0 ; lineIndex < lines.Count - 1; lineIndex ++)
            {
                var line = lines[lineIndex];
                if (!line.StartsWith("using Serilog")) continue;
                
                var edit = new RequiredFileEdit
                {
                    filePath = path,
                    code = $"{beamoId}_removeSerilog",
                    beamoId = beamoId,
                    title = "Remove Serilog",
                    description =
                        "As of CLI 5.0.0, Serilog has been removed and should not be used. ",
                    replacementText = $"using Beamable.Server;"
                };
                edit.SetLocationAsLineReplacement(lineNumberToIndex, lineIndex);
                edits.Add(edit);
            }
        }

        return edits;
    }

    /// <summary>
    /// As of CLI 4.1.2, the docker build context is the project folder, NOT the .beamable folder
    /// and the .dockerignore file started getting used again. Sadly, the ignore file is
    /// ignoring the /bin/beamApp folder
    /// We need to add an explicit inclusion for this folder
    /// </summary>
    /// <param name="beamoId"></param>
    /// <param name="lineNumberToIndex"></param>
    /// <param name="project"></param>
    /// <returns></returns>
    static List<RequiredFileEdit> EnsureDockerIgnoreAllowsBeamApp(string beamoId, Dictionary<int, int> _, List<string> __, 
        Project project, FileCache cache)
    {
        // identify if there is a .dockerignore file...
        var csProjFile = project.FullPath;
        var folder = Path.GetDirectoryName(csProjFile);
        var dockerIgnoreFile = Path.Combine(folder, ".dockerignore");

        // if the file doesn't exist; then great, no modification is required!
        if (!File.Exists(dockerIgnoreFile)) return null;

        var (allText, lineNumberToIndex, lines) = ReadLineNumbers(dockerIgnoreFile, cache);
        
        foreach (var line in lines)
        {
            if (line == "!**/beamApp")
            {
                // hooray! The inclusion is allowed!
                // TODO: note; if this was BEFORE the _exclusion_, then this check would fail.
                return null;
            }
        }
        
        // sadly, we need to inject something into the file...
        var edit = new RequiredFileEdit
        {
            filePath = dockerIgnoreFile,
            code = $"{beamoId}_dockerIgnoreFix",
            beamoId = beamoId,
            title = "Adjust .dockerignore File",
            description =
                "As of CLI 4.1.2, the .dockerignore file needs a line to explicitly include the /bin/beamApp folder.",
            replacementText = $"{Environment.NewLine}!**/beamApp"
        };
        edit.SetLocationAsAppend(lineNumberToIndex, allText);
        return new List<RequiredFileEdit> { edit };
    }

    /// <summary>
    /// As of CLI 4, it is invalid to use MongoDB.Driver at 2.15.1.
    /// There is a security vulnerability.
    /// https://github.com/advisories/GHSA-7j9m-j397-g4wx
    ///
    /// As of CLI 5, it is required to be using at least 3.0.0 to support the OTEL instrumentation
    /// </summary>
    static List<RequiredFileEdit> EnsureUsingLatestMongo(string beamoId, Dictionary<int, int> lineNumberToIndex, List<string> lines, 
        Project project, FileCache cache)
    {

        var packages = project.GetItems("PackageReference");
        var mongoPackage = packages.FirstOrDefault(p =>
            string.Equals(p.EvaluatedInclude, "MongoDB.Driver", StringComparison.InvariantCulture));
        if (mongoPackage == null) return null;
        
        var versionTag = mongoPackage.Metadata.FirstOrDefault(m =>
            string.Equals(m.Name, "version", StringComparison.InvariantCultureIgnoreCase));
        if (versionTag == null) return null;
        
        var mongoVersion = versionTag.EvaluatedValue;

        var versionParts = mongoVersion.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (versionParts.Length < 3) return null;
        var versionNumbers = versionParts.Select(int.Parse).ToArray();
        if (versionNumbers[0] > 3) return null; // anything above 3 is fine.
        if (versionNumbers[1] >= 3) return null; // anything above 3.3 is fine.
        // but everything else needs this message...
        

        var lineNumber = mongoPackage.Xml.Location.Line - 1;
        var line = lines[lineNumber];
        var tabbing = line.Substring(0, mongoPackage.Xml.Location.Column - 1);
        
        var edit = new RequiredFileEdit
        {
            code = $"{beamoId}_updateMongo",
            filePath = project.FullPath,
            beamoId = beamoId,
            title = "Upgrade Mongo Version",
            description = $"The project is referencing an older version of MongoDB.Diver. It must be upgraded to 3.3.0. ",
            replacementText = $"{tabbing}<PackageReference Include=\"MongoDB.Driver\" Version=\"3.3.0\"/>",
        };
        edit.SetLocationAsLineReplacement(lineNumberToIndex, lineNumber);
       
        return new List<RequiredFileEdit> { edit };
    }

    /// <summary>
    /// As of CLI 4, it is invalid for the project to use anything less than net8.
    /// We stopped publishing nuget packages for net6 and 7. 
    /// </summary>
    static List<RequiredFileEdit> EnsureAtLeastNet8(string beamoId, Dictionary<int, int> lineNumberToIndex, List<string> _, Project project,FileCache cache)
    {
        var property = project.GetProperty("TargetFramework");
        if (property == null) return null;

        var framework = property.EvaluatedValue.Trim();

        if (framework == "net6.0" || framework == "net7.0")
        {
            var location = property.Xml.Location;
            var edit = new RequiredFileEdit
            {
                code = $"{beamoId}_net8",
                filePath = property.Project.FullPath,
                beamoId = beamoId,
                title = "Upgrade Target Framework",
                description = $"The project is currently using {framework}, but a net8.0 is required",
                replacementText = "<TargetFramework>net8.0</TargetFramework>",
            };
            if (!edit.TrySetLocation(location, lineNumberToIndex, property.Xml.OuterElement))
            {
                return null;
            }
            
            return new List<RequiredFileEdit> { edit };
        }
        
        return null;
    }
}
