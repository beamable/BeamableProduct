using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.DockerCommands;
using CliWrap;
using Spectre.Console;

namespace cli.BackendCommands;

public class CheckDepsCommandArgs : CommandArgs
{
    public bool forceInstall;
    public bool noInstall;
}

public class CheckDepsCommandProgramsResult
{
    public List<CheckDepsProgramResult> programDependencies = new List<CheckDepsProgramResult>();
}

public class CheckDepsProgramResult
{
    public string name;
    public bool status;
}

public class CheckDepsCommandResultChannel : IResultChannel
{
    public string ChannelName => "programs";
}

public class CheckDepsCommand 
    : AppCommand<CheckDepsCommandArgs>
    , IResultSteam<CheckDepsCommandResultChannel, CheckDepsCommandProgramsResult>
    ,IStandaloneCommand, ISkipManifest

{
    public CheckDepsCommand() : base("validate", "Check that the current machine has the dependencies to run the Beamable backend.")
    {
    }

    public override void Configure()
    {
        AddOption(
            new Option<bool>("--force-install",
                "Force the installation of programmable dependencies like git, scala, java, and maven"),
            (args, i) => args.forceInstall = i);
        AddOption(
            new Option<bool>("--no-install",
                "Don't offer any automatic installation"),
            (args, i) => args.noInstall = i);
    }

    public override async Task Handle(CheckDepsCommandArgs args)
    {
        var os = CollectorManager.GetCurrentPlatform();
        if (os != OSPlatform.OSX)
        {
            throw new NotImplementedException("the CLI can only validate dependencies for OSX. Windows coming next!");
        }

        if (args.noInstall && args.forceInstall)
        {
            throw new CliException("Cannot pass both --no-install and --force-install");
        }
        
        // The beamable backend requires the following dependencies...
        //  1. openJDK 8 
        //  2. scala 2.11
        //  3. maven 
        //  4. docker
        //  5. git (implied)
        //  6. github cli
        // 
        // to validate these, try invoking each from the terminal

        Log.Information("Checking system for dependencies... Please refer to the ReadMe page for details.");
        const string readmeLink = "https://github.com/beamable/BeamableBackend/blob/main/README.md";
        Log.Information(readmeLink);
        
        var checkJava = CheckForProgram("java", args.AppContext.JavaPath, "-version", "1.8.0");
        var checkMaven = CheckForProgram("maven", "mvn", "-version", "");
        var checkScala = CheckForProgram("scala", "scala", "-version", "version 2.11.12");
        var checkGit = CheckForProgram("git", "git", "--version", "");
        var checkGitHubCli = CheckForProgram("github cli", "gh", "--version", "");
        
        // TODO: install the aws cli and sign in
        //  need to add the STS roles to verify that the user is able to run stuff. 
        // beamable-assume-service-role-policy
        // beamable-microservice-assume-role-policy
        var checkAwsCli = CheckForProgram("aws cli", "aws", "--version", "");
        var checkDocker = CheckForProgram("docker", args.AppContext.DockerPath, "--version", "");
        
        var initialResults = new CheckDepsCommandProgramsResult
        {
            programDependencies = new List<CheckDepsProgramResult>
            {
                await checkJava,
                await checkMaven,
                await checkScala,
                await checkGit,
                await checkGitHubCli,
                await checkDocker
            }
        };
        
        // this.LogResult(initialResults);
        this.SendResults(initialResults);

        var missingDeps = initialResults.programDependencies
            .Where(d => d.status == false)
            .ToList();

        if (missingDeps.Count == 0)
        {
            Log.Information("All dependencies are installed!");
            if (!args.forceInstall)
            {
                // only return if we aren't forcing the installation
                return;
            }
        }


        Log.Information("The following dependencies must be installed.");
        foreach (var missingDep in missingDeps)
        {
            Log.Information($" {missingDep.name}");
        }
        if (args.noInstall)
        {
            return;
        }

        // docker is the only special dependency that cannot be installed with 'brew'
        if (missingDeps.Any(d => d.name == "docker"))
        {
            await InstallDocker(args);
            return;
        }

        if (args.forceInstall)
        {
            missingDeps = initialResults.programDependencies.Where(x => x.name != "docker").ToList();
        }
        
        var hasBrew = await BrewUtility.CheckBrew();
        if (!hasBrew)
        {
            var shouldInstallBrew = args.Quiet || AnsiConsole.Confirm(
                prompt: "To install these dependencies, we recommend you use Brew. Would you like to download and install Brew? Please re-run this command after Brew has been installed.");
            if (!shouldInstallBrew)
            {
                Log.Information("Please install the dependencies and try this command again.");
                return;
            }
            
            await BrewUtility.InstallBrew(args);
            return;
        }


        var shouldInstall = args.Quiet || AnsiConsole.Confirm("Would you like to use Brew to install these dependencies? You may be asked for your password.");
        if (!shouldInstall)
        {
            Log.Information("Please install the dependencies and try this command again.");
            return;
        }
        
        foreach (var dep in missingDeps)
        {
            switch (dep.name)
            {
                case "java":
                    await BrewUtility.RunBrew("install --cask temurin@8");
                    break;
                case "maven":
                    await BrewUtility.RunBrew("install maven");
                    break;
                case "scala":
                    await BrewUtility.RunBrew("install coursier");
                    await BrewUtility.RunBrew("coursier", "setup");
                    await BrewUtility.RunBrew("cs", "install scala:2.11.12");
                    await BrewUtility.RunBrew("cs", "install scalac:2.11.12");
                    break;
                case "git":
                    await BrewUtility.RunBrew("install git");
                    break;
                case "github cli":
                    await BrewUtility.RunBrew("install gh");
                    break;
                default:
                    Log.Error($"The {dep.name} could not be automatically installed with brew. Please reach out to the Beamable team if you see this error.");
                    break;
            }
        }

        Log.Information("The dependencies have been installed. Please re-run this command to verify.");

    }
    
    public static async Task InstallDocker(CommandArgs args)
    {
        Log.Information("Docker must be installed manually with the installer.");
        var link = StartDockerCommand.GetDockerDownloadLink();

        Log.Information($"Download link: {link}");

        string installerFileName = Path.GetFileName(link);
        var downloadPath = Path.Combine(Path.GetTempPath(), installerFileName);
        var installerExists = File.Exists(downloadPath);

        var confirm = args.Quiet;
        if (!installerExists)
        {
            var shouldDownload = confirm || AnsiConsole.Confirm("Would you like to download the Docker installer and run it?");

            if (!shouldDownload)
            {
                Log.Information("Please install Docker manually and run this command again.");
                return;
            }
            await AnsiConsole.Status().Spinner(Spinner.Known.Aesthetic).StartAsync("Downloading...", async ctx =>
            {
                await CollectorManager.DownloadAndDecompressGzip(new HttpClient(), link, downloadPath, true, decompress: false);
            });
        }
        
        var shouldInstall = confirm || AnsiConsole.Confirm("Would you like to run the Docker installer?");
        if (!shouldInstall)
        {
            Log.Information("Please install Docker manually and run this command again.");
            Log.Information($" installer is at {downloadPath}");
            return;
        }

        Log.Information("Running Docker installer... Please re-run this command when it completes.");
        var p = Process.Start(new ProcessStartInfo(downloadPath)
        {
            UseShellExecute = true
        });
        await p.WaitForExitAsync();

    }
    
    public static async Task<CheckDepsProgramResult> CheckForProgram(string name, string program, string args, string expectedOutputSnippet)
    {
        Log.Information($"Checking system for {name}");
        Log.Debug($"Checking for {name} as program=[{program}] with args=[{args}]");
        var result = new CheckDepsProgramResult
        {
            name = name,
            status = false
        };
        var output = new StringBuilder();
        var command = CliWrap.Cli
            .Wrap(program)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None);

        try
        {
            var res = command.ExecuteAsync();
            await res;
            var success = res.Task.Result.ExitCode == 0;
            if (!success)
            {
                result.status = false;
                return result;
            }

            var outputText = output.ToString();
            Log.Debug(outputText);
            Log.Debug("");
            var hasSnippet = outputText.Contains(expectedOutputSnippet, StringComparison.InvariantCultureIgnoreCase);
            if (!hasSnippet)
            {
                Log.Debug($"{name} was installed, but did not contain the expected output=[{expectedOutputSnippet}]");
                result.status = false;
                return result;
            }

            result.status = true;
        }
        catch (Exception)
        {
            result.status = false;
        }
        return result;
    }
}