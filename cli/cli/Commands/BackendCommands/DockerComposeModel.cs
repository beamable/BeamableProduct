namespace cli.BackendCommands;

public class DockerComposeModel
{
    public Dictionary<string, DockerComposeService> services;
}

public class DockerComposeService
{
    public string[] profiles = Array.Empty<string>();
    public string[] dependsOn = Array.Empty<string>();
}