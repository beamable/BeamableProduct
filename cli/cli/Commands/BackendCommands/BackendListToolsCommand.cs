using System.Diagnostics;
using Beamable.Server;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cli.BackendCommands;

public class BackendListToolsCommandArgs : CommandArgs
{
    public string backendHome;

}

public class BackendListToolsCommandResults
{
    public BackendToolList toolList = new BackendToolList();
}

public class BackendToolList
{
    public string coreProjectPath;
    public List<BackendInfraInfo> infra = new List<BackendInfraInfo>();
    public List<BackendToolInfo> tools = new List<BackendToolInfo>();
    public List<string> invalidFolders = new List<string>();
}

public class BackendInfraInfo
{
    public string name;
    public string[] dependsOn = Array.Empty<string>();
}

[DebuggerDisplay("tool=[{name}]")]
public class BackendToolInfo
{
    public string name;
    public string projectPath;
    public string mainClassName;
    public string[] profiles = Array.Empty<string>();
    public string[] dependsOn = Array.Empty<string>();
    public string[] basicServiceNames = Array.Empty<string>();
    public string[] objectSerivceNames = Array.Empty<string>();
}

public class BackendListToolsCommand 
    : AtomicCommand<BackendListToolsCommandArgs, BackendListToolsCommandResults>
, ISkipManifest, IStandaloneCommand
{
    public BackendListToolsCommand() : base("list-tools", "List all the available tools in the backend source")
    {
    }

    public override void Configure()
    {
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override Task<BackendListToolsCommandResults> GetResult(BackendListToolsCommandArgs args)
    {
        return Task.FromResult(new BackendListToolsCommandResults
        {
            toolList = GatherToolList(args.backendHome)
        });

    }

    public static DockerComposeModel GetLocalDockerComposeInfo(string backendHome)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(backendHome);

        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml 
            .Build();
        const string localDockerComposeFile = "docker/local/docker-compose.yml";
        var localDockerComposePath = Path.Combine(backendHome, localDockerComposeFile);
        var yml = File.ReadAllText(localDockerComposePath);
        var p = deserializer.Deserialize<DockerComposeModel>(yml);
        return p;
    }
    
    

    public static BackendToolList GatherToolList(string backendHome)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(backendHome);

        var result = new BackendToolList
        {
            coreProjectPath = Path.Combine(backendHome, "core")
        };
        
        var dockerComposeInfo = GetLocalDockerComposeInfo(backendHome);

        foreach (var (name, service) in dockerComposeInfo.services)
        {
            if (service.labels.ContainsKey(BackendPsCommand.BEAMABLE_LABEL)) // "core" means "infra"
            {
                result.infra.Add(new BackendInfraInfo
                {
                    name = name, 
                    dependsOn = service.dependsOn
                });
            }
        }
        
        var toolsDir = Path.Combine(backendHome, "tools");
        var toolFolders = Directory.GetDirectories(toolsDir);
        foreach (var toolFolder in toolFolders)
        {
            var tool = Path.GetFileName(toolFolder);
            Log.Debug($"inspecting tool folder=[{tool}]");

            var srcFiles = Directory.EnumerateFiles(toolFolder, "*.scala", SearchOption.AllDirectories);
            var foundEntryPoint = false;
            foreach (var srcFile in srcFiles)
            {
                using var stream = File.OpenRead(srcFile);
                using var reader = new StreamReader(stream);
                var package = "";
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (line.StartsWith("package "))
                    {
                        package = line.Substring("package ".Length); // maybe there is a comment on the end of the line?
                        continue;
                    }
                    var indexOfObject = line.IndexOf("object");
                    var indexOfExtends = line.IndexOf("extends MicroService");

                    var isServiceClass = indexOfObject > -1 && indexOfExtends > -1;
                    if (!isServiceClass) continue;

                    var startIndex = indexOfObject + "object ".Length;
                    var className = line.Substring(startIndex,  (indexOfExtends - startIndex) - 1);

                    var toolInfo = new BackendToolInfo
                    {
                        name = tool,
                        projectPath = toolFolder,
                        mainClassName = package + "." + className
                    };
                    if (!dockerComposeInfo.services.TryGetValue(tool, out var service))
                    {
                        Log.Debug($"Found a tool=[{tool}] that was not listed in the docker-compose file");
                    }
                    else
                    {
                        toolInfo.profiles = service.profiles;
                        toolInfo.dependsOn = service.dependsOn;
                       

                        if (service.services?.TryGetValue("basic", out var basic) ?? false)
                        {
                            toolInfo.basicServiceNames = basic ?? new string[] { tool };
                        }
                        if (service.services?.TryGetValue("object", out var objects) ?? false)
                        {
                            toolInfo.objectSerivceNames = objects ?? new string[] { tool };
                        }
                        
                        // var explicitBasicServices = service.ExplicitBasicServices;
                        // var explicitObjectServices = service.ExplicitObjectServices;
                        // if (explicitBasicServices != null)
                        // {
                        //     toolInfo.basicServiceNames = explicitBasicServices;
                        //     if (toolInfo.basicServiceNames.Length == 0)
                        //     {
                        //         toolInfo.basicServiceNames = new string[] { tool };
                        //     }
                        // }
                        //
                        // if (explicitObjectServices != null)
                        // {
                        //     toolInfo.objectSerivceNames = explicitObjectServices;
                        //     if (toolInfo.objectSerivceNames.Length == 0)
                        //     {
                        //         toolInfo.objectSerivceNames = new string[] { tool };
                        //     }
                        // }
                    }
                    
                    result.tools.Add(toolInfo);
                    foundEntryPoint = true;
                    break;
                }

                if (foundEntryPoint)
                {
                    break;
                } 
            }

            if (!foundEntryPoint)
            {
                result.invalidFolders.Add(toolFolder);
            }
        }

        return result;
    }
}