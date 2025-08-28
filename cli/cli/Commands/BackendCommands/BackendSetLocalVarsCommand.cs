using System.CommandLine;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Beamable.Server;
using CliWrap;

namespace cli.BackendCommands;

public class BackendSetLocalVarsCommandArgs : CommandArgs
{
    public string repoUrl;
    public string backendHome;
    public string githubToken;
}

public class BackendSetLocalVarsCommandResults
{
    
}
public class BackendSetLocalVarsCommand 
    : AtomicCommand<BackendSetLocalVarsCommandArgs, BackendSetLocalVarsCommandResults>
,IStandaloneCommand, ISkipManifest
{
    public BackendSetLocalVarsCommand() : base("set-local-vars", "Set local variables from github")
    {
    }

    public override void Configure()
    {
        BackendCommandGroup.AddBackendRepoOption(this, (args, i) => args.repoUrl = i);
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);

        AddOption(
            new Option<string>(new string[] { "--gh-token" },
                "Pass a github token for authentication. By default, the `gh auth token` utility will be used."),
            (args, i) => args.githubToken = i);
    }

    public override async Task<BackendSetLocalVarsCommandResults> GetResult(BackendSetLocalVarsCommandArgs args)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(args.backendHome);
        
        // need to get the gh-token
        if (string.IsNullOrEmpty(args.githubToken))
        {
            args.githubToken = await GetGithubToken();
        }

        Log.Information("Fetching github variables...");
        var githubVariables = await GetAllGithubVariables(args.repoUrl, args.githubToken);

        Log.Information("Writing configuration files...");
        var allTemplatePaths = Directory.GetFiles(args.backendHome, "*.liquid", SearchOption.AllDirectories);
        var targetFolder = Path.DirectorySeparatorChar + "target" + Path.DirectorySeparatorChar;
        foreach (var templatePath in allTemplatePaths)
        {
            if (templatePath.Contains(targetFolder))
            {
                Log.Debug($"Skipping this path because it is in a target/ folder. path=[{templatePath}]");
                continue;
            }
            
            Log.Debug($"found template file=[{templatePath}]");
            var confPath = templatePath.Substring(0, templatePath.Length - ".liquid".Length);
           
            var template = File.ReadAllText(templatePath);

            foreach (var variable in githubVariables.variables)
            {
                template = template.Replace($"{{{{ {variable.name} }}}}", variable.value);
            }
            
            Log.Information($" {confPath}");
            File.WriteAllText(confPath, template);
        }
        
        Log.Information("All done!");
        return new BackendSetLocalVarsCommandResults();
    }

    public static async Task<string> GetGithubToken()
    {
        // TODO: make handling the case where the user is logged out of gh cli easier.
        var stdOutBuffer = new StringBuilder();
        await Cli.Wrap("gh")
            .WithArguments("auth token")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                Log.Error($"gh cli error: {line}");
                Log.Error("Make sure to log into github cli with `github auth login`");
            }))
            .ExecuteAsync();
        return stdOutBuffer.ToString().Trim();
    }

    private static async Task<GitHubVarsResponse> GetAllGithubVariables(string repo, string githubToken)
    {
        var pageSize = 30;
        var page = 1; // annoying, github pages are 1 based, so 1 is the first page, not 0.  
        var client = new HttpClient();
        var results = await MakeGitHubRequestForPage(client, repo, githubToken, page, pageSize);
        var accrued = results;
        
        while (results.total_count > accrued.variables.Count)
        {
            page++;
            results = await MakeGitHubRequestForPage(client, repo, githubToken, page, pageSize);
            accrued.variables.AddRange(results.variables);
        }
        return accrued;
    }
    
    private static async Task<GitHubVarsResponse> MakeGitHubRequestForPage(HttpClient client, string repo, string githubToken, int page, int perPage)
    {
        var uri = $"https://api.github.com/repos/{repo}/environments/local/variables?per_page={perPage}&page={page}";

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("dotnet-client");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubVarsResponse>(json, new JsonSerializerOptions
        {
            IncludeFields = true
        });
    }
    
    private class GitHubVarsResponse
    {
        public int total_count;
        public List<GitHubVar> variables;
    }

    [DebuggerDisplay("{name}: {value}")]
    private class GitHubVar
    {
        public string name;
        public string value;
    }
}