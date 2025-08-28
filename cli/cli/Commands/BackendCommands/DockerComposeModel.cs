
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace cli.BackendCommands;

public class DockerComposeModel
{
    public Dictionary<string, DockerComposeService> services;
}

public class DockerComposeService
{
    public string[] profiles = Array.Empty<string>();
    public string[] dependsOn = Array.Empty<string>();
    
    // [YamlMember(Alias = "x-beam-services", ApplyNamingConventions = false)]
    
    [YamlMember(Alias = "x-beam-services", ApplyNamingConventions = false)]
    public Dictionary<string, string[]> services;
    public Dictionary<string, string> labels = new Dictionary<string, string>();

    // public string[] ExplicitBasicServices => (services?.ContainsKey("basic") ?? false) ? services["basic"] : null;
    // public string[] ExplicitObjectServices => (services?.ContainsKey("object") ?? false) ? services["object"] : null;
}
