#!/usr/bin/env dotnet run
#:package clapnet@0.3.*
#:package Spectre.Console.Cli@0.53.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;
using System.Text.RegularExpressions;

const string GO_VERSION = "1.24";

return clapnet.CommandBuilder.New()
    .WithRootCommand(Root, "CLI Helper for Beamable project")
    .With(Setup, "Setup project for development")
    .With(Dev, "rebuild nuget packages and install beamable cli", "dev")
    .With(SyncRiderSettings, "Sync Rider Settings", "sync")
    .With(SyncRiderUnity, "Sync Rider Settings for Unity(shortcut command)", "sync-unity")
    .With(SyncRiderUnreal, "Sync Rider Settings for Unreal(shortcut command)", "sync-unreal")
    .Run(args);

void Root()
{
    var tt = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What's on your mind today?")
            .AddChoices("Setup project for development", "dev- rebuild nuget packages and install beamable cli", "Sync Rider Settings for Unity", "Sync Rider Settings for Unreal"));


    if (tt.StartsWith("Setup"))
    {

        Setup(new SetupSettingsOptions());
    }
    else if (tt.StartsWith("dev-"))
    {
        Dev(new DevOptions());
    }
    else if (tt.Contains("Unity"))
    {
        SyncRiderUnity(new SyncSettingsOptions());
    }
    else if (tt.StartsWith("Unreal"))
    {
        SyncRiderUnreal(new SyncSettingsOptions());
    }
}

void Setup(SetupSettingsOptions cfg)
{
    AnsiConsole.MarkupLine("Setting up the [bold][blue]Beamables[/][/]!");

    // Load environment variables from .dev.env file
    LoadDevEnvironment();
    string root = GetRootDirectory();
    string sourceFolderPath = Path.Combine(root, Environment.GetEnvironmentVariable("SOURCE_FOLDER") ?? "");


    if (File.Exists(Path.Combine(root, "build-number.txt")) && !AnsiConsole.Confirm("Looks like the installation was attempted before. Continue with installation?"))
    {
        return;
    }


    AnsiConsole.Status()
        .StartAsync("Initializing...", async ctx =>
        {
            File.WriteAllText(Path.Combine(root, "build-number.txt"), "0");
            var feedName = Environment.GetEnvironmentVariable("FEED_NAME") ?? "BeamableNugetSource";
            AnsiConsole.MarkupLine("Created build number file");
            ctx.Status("Installing Go");
            var (goInstalled, goPath) = await CheckAndInstallGo(cfg.acceptAnyGoVersion);
            await SetupTemplateDotNetConfig();
            await RunProcessAsync("dotnet", $"tool restore --tool-manifest ./cli/cli/.config/dotnet-tools.json");
            ctx.Status($"Setting up {feedName} at {sourceFolderPath}");
            await SetupNuGetSource(sourceFolderPath, feedName);
            AnsiConsole.Write($"Setting up {feedName} at {sourceFolderPath} complete");
            ctx.Status("Building otel-collector");
            await BuildOtel(goPath);

        }).Wait();
}

int SyncRiderUnity(SyncSettingsOptions cfg, string pathToWorkingDirectory = "", string pathToRestoreDirectory = "")
{
    return SyncRiderSettings(cfg, "UNITY", pathToWorkingDirectory, pathToRestoreDirectory);
}

int SyncRiderUnreal(SyncSettingsOptions cfg, string pathToWorkingDirectory = "", string pathToRestoreDirectory = "")
{
    return SyncRiderSettings(cfg, "UNREAL", pathToWorkingDirectory, pathToRestoreDirectory);
}

int SyncRiderSettings(SyncSettingsOptions cfg, string targetEngine, string pathToWorkingDirectory = "", string pathToRestoreDirectory = "")
{
    var isUnity = targetEngine.Equals("UNITY", StringComparison.OrdinalIgnoreCase);
    var isUnreal = targetEngine.Equals("UNREAL", StringComparison.OrdinalIgnoreCase);

    if (!isUnity && !isUnreal)
    {
        AnsiConsole.MarkupLine($"Invalid target engine argument value. Supported engines are [bold]UNITY[/] and [bold]UNREAL[/], got: [red]{targetEngine}[/]");
        return 1;
    }

    string baseDir = GetRootDirectory();
    cfg.VerboseLog(baseDir);

    // Handle PathToWorkingDirectory
    if (string.IsNullOrEmpty(pathToWorkingDirectory))
    {
        pathToWorkingDirectory = Path.Combine(baseDir, "client");
    }

    pathToWorkingDirectory = pathToWorkingDirectory.Replace("\\", "/");

    // Set default PathToRestoreDirectory for UNREAL
    if (isUnreal && string.IsNullOrEmpty(pathToRestoreDirectory))
    {
        pathToRestoreDirectory = Path.Combine(pathToWorkingDirectory, "Microservices");
    }
    var pathToUnixShell = cfg.GetShell().Replace("\\", "/");

    string cliRunPath = Path.Combine(baseDir, "cli", ".run");

    if (!Directory.Exists(cliRunPath))
    {
        AnsiConsole.WriteLine($"Invalid path to a .run folder. Given Path: {cliRunPath}");
        return 1;
    }
    AnsiConsole.WriteLine($"Copying all TEMPLATE- configurations into {targetEngine}- configurations.");

    string[] templateFiles = Directory.GetFiles(cliRunPath, "TEMPLATE-*.run.xml");

    var root = new Tree("Template files");

    foreach (string templateFile in templateFiles)
    {
        string fileName = Path.GetFileName(templateFile);
        string targetFileName = fileName.Replace("TEMPLATE-", $"{targetEngine}-");
        string targetFilePath = Path.Combine(cliRunPath, targetFileName);
        var rule = new Rule($"Building [green]{targetFileName}[/]");
        rule.Justification = Justify.Left;
        AnsiConsole.Write(rule);

        cfg.VerboseLog($"cp {templateFile} {targetFilePath}");
        File.Copy(templateFile, targetFilePath, true);

        // Read the file content
        string content = File.ReadAllText(targetFilePath);

        // Replace TEMPLATE- with target engine
        content = content.Replace("TEMPLATE-", $"{targetEngine}-");

        // Replace INTERPRETER_PATH
        content = Regex.Replace(content,
            @"value=""[^""]*"" name=""INTERPRETER_PATH""",
            $@"value=""{EscapeXmlAttribute(pathToUnixShell)}"" name=""INTERPRETER_PATH""");
        cfg.VerboseLog($"Replaced INTERPRETER_PATH with {pathToUnixShell}");

        // Handle special cases for Set-Local-Packages and Set-Install scripts
        if (targetFileName.Contains($"{targetEngine}-Set-Local-Packages") ||
            targetFileName.Contains($"{targetEngine}-Set-Install-"))
        {
            // Replace $PROJECT_DIR$/../client with PathToWorkingDirectory
            content = content.Replace("$PROJECT_DIR$/../client", pathToWorkingDirectory);
            cfg.VerboseLog($"Replaced $PROJECT_DIR$/../client with {pathToWorkingDirectory}");

            // Replace BeamableNugetSource with {TargetEngine}_NugetSource
            content = content.Replace("BeamableNugetSource", $"{targetEngine}_NugetSource");
            cfg.VerboseLog($"Replaced BeamableNugetSource with {targetEngine}_NugetSource");

            // Replace PathToRestore with pathToRestoreDirectory
            if (!string.IsNullOrEmpty(pathToRestoreDirectory))
            {
                content = content.Replace("PathToRestore", pathToRestoreDirectory);
                cfg.VerboseLog($"Replaced PathToRestore with {pathToRestoreDirectory}");
            }
        }
        else
        {
            // Replace WORKING_DIRECTORY
            content = Regex.Replace(content,
                @"value=""[^""]*"" name=""WORKING_DIRECTORY""",
                $@"value=""{EscapeXmlAttribute(pathToWorkingDirectory)}"" name=""WORKING_DIRECTORY""");
            cfg.VerboseLog($"Replaced [bold][teal]WORKING_DIRECTORY[/][/] with [bold]{pathToWorkingDirectory}[/]");
        }

        // Write the modified content back to the file
        File.WriteAllText(targetFilePath, content);
    }
    return 0;
}

static async Task SetupTemplateDotNetConfig()
{
    const string templateDotNetConfigDir = "./cli/beamable.templates/.config";

    if (!Directory.Exists(templateDotNetConfigDir))
    {
        Directory.CreateDirectory(templateDotNetConfigDir);
    }

    const string jsonContent = @"{
""version"": 1,
""isRoot"": true,
""tools"": {
""beamable.tools"": {
  ""version"": ""0.0.123.0"",
  ""commands"": [
    ""beam""
  ],
  ""rollForward"": false
}
}
}";

    await File.WriteAllTextAsync(Path.Combine(templateDotNetConfigDir, "dotnet-tools.json"), jsonContent);
}

static async Task SetupNuGetSource(string sourceFolderPath, string feedName)
{
    // Delete and recreate source folder
    if (Directory.Exists(sourceFolderPath))
    {
        Directory.Delete(sourceFolderPath, true);
    }
    Directory.CreateDirectory(sourceFolderPath);

    // Remove old NuGet source
    AnsiConsole.Write("Removing old source (if none exists, you'll see an error 'Unable to find any package', but that is okay)");
    try
    {
        await RunProcessAsync("dotnet", $"nuget remove source {feedName}");
    }
    catch
    {
        // Ignore errors when removing non-existent source
    }

    // Add new source
    AnsiConsole.Write("Adding new source!");
    await RunProcessAsync("dotnet", $"nuget add source \"{sourceFolderPath}\" --name {feedName}");
}

///<summary>
/// This script will be run many times as you develop.
/// Anytime you need to test a change with new code you've written
/// locally, you should run this script.
/// It will...
///  1. build all the projects that result in Nuget packages,
///  2. publish them locally to the local package feed
///  3. update the local templates
///  4. install the latest CLI globally
///  5. invalidate the nuget cache for local beamable dev packages, which
///     means that downstream projects will need to run a `dotnet restore`.
///</summary>
void Dev(DevOptions cfg)
{
    LoadDevEnvironment();
    var buildInfo = GetNextBuildNumber();
    RandomCompliment();
    string version = $"0.0.123.{buildInfo.Next}";
    string previousVersion = $"0.0.123.{buildInfo.Previous}";
    string feedName = Environment.GetEnvironmentVariable("FEED_NAME") ?? "BeamableNugetSource";
    string solution = "./build/LocalBuild/LocalBuild.sln";
    string tmpBuildOutput = "TempBuild";
    string buildArgs = $"--configuration Release -p:PackageVersion={version} -p:CombinedVersion={version} -p:InformationalVersion={version} -p:Warn=0 -p:BeamBuild=true";
    string packArgs = $"--configuration Release --no-build -o {tmpBuildOutput} -p:PackageVersion={version} -p:CombinedVersion={version} -p:InformationalVersion={version} -p:SKIP_GENERATION=true -p:BeamBuild=true";
    string pushArgs = $"--source {feedName}";
    AnsiConsole.Status()
        .StartAsync("Initializing...", async ctx =>
        {
            AnsiConsole.MarkupLine($"[bold][green]Building version {version}[/][/]");

            ctx.Status("[bold]Restoring project...[/]");
            await RunProcessAsync("dotnet", $"restore {solution}");

            ctx.Status("[bold]Building services...[/]");
            await RunProcessAsync("dotnet", $"build {solution} {buildArgs}");
            await RunProcessAsync("dotnet", "dotnet build cli/cli/cli.csproj -f net10.0");
            AnsiConsole.WriteLine("CLI built successfully");
            if (!cfg.skipUnity)
            {
                ctx.Status("[bold]Copying code to Unity...[/]");
                RunProcessAsync("dotnet", $"build cli/beamable.common -f net10.0 -t:CopyCodeToUnity -p:BEAM_COPY_CODE_TO_UNITY=true").Wait();
                AnsiConsole.WriteLine("Code copied to Unity successfully");
            }

            ctx.Status("[bold]Packing...[/]");
            await RunProcessAsync("dotnet", $"pack {solution} {packArgs}");

            ctx.Status("[bold]Pushing packages...[/]");
            RunProcessAsync("dotnet", $"nuget push {tmpBuildOutput}/*.{version}.nupkg {pushArgs}").Wait();
            AnsiConsole.WriteLine("Packages pushed successfully");
            ctx.Status("[bold]Deleting old packages...[/]");
            DeletePackagesAsync(feedName, previousVersion).Wait();

            ctx.Status("[bold]Updating templates...[/]");
            UpdateTemplatesAsync(tmpBuildOutput, version).Wait();

            ctx.Status("[bold]Cleaning up...[/]");
            if (Directory.Exists(tmpBuildOutput))
                Directory.Delete(tmpBuildOutput, true);

            InvalidateNugetCache(previousVersion);

            ctx.Status("[bold]Installing CLI globally...[/]");
            await RunProcessAsync("dotnet", $"tool install Beamable.Tools --version {version} --global --allow-downgrade --no-cache");
            AnsiConsole.WriteLine("CLI installed globally successfully");
            if (!cfg.skipUnity)
            {
                ctx.Status("[bold]Preparing Unity SDK...[/]");
                RunProcessAsync("beam", "generate-interface --engine unity --output=./client/Packages/com.beamable/Editor/BeamCli/Commands --no-log-file").Wait();

                ctx.Status("[bold]Updating Unity templates...[/]");
                await RunProcessAsync("dotnet", $"tool update Beamable.Tools --version {version} --allow-downgrade", workingDirectory: Path.GetFullPath("./cli/beamable.templates/templates/BeamService"));
                await RunProcessAsync("dotnet", $"restore BeamService.csproj --no-cache --force", workingDirectory: Path.GetFullPath("./cli/beamable.templates/templates/BeamService"));
                AnsiConsole.WriteLine("Unity prepared successfully");
            }
            var unrealPath = Path.GetFullPath("../UnrealSDK");
            if (!cfg.skipUnreal && Directory.Exists(unrealPath))
            {
                ctx.Status("[bold]Preparing UnrealSDK...[/]");
                RunProcessAsync("dotnet", $"tool update Beamable.Tools --version {version} --allow-downgrade", workingDirectory: unrealPath).Wait();
                RestoreAllCsprojInMicroservices(Path.Combine(unrealPath, "Microservices")).Wait();
                AnsiConsole.WriteLine("Unreal prepared successfully");
            }

            var samsLocalSandbox = Path.GetFullPath("../SamsLocalSandbox");
            if (!cfg.skipSamsSandbox && Directory.Exists(samsLocalSandbox))
            {
                ctx.Status("[bold]Preparing SamsSandbox...[/]");
                RunProcessAsync("dotnet", $"tool update Beamable.Tools --version {version} --allow-downgrade", workingDirectory: samsLocalSandbox).Wait();
                await RestoreAllCsprojInMicroservices(Path.Combine(samsLocalSandbox, "Microservices"));
                AnsiConsole.WriteLine("SamsSandbox prepared successfully");
            }

        }).Wait();
    AnsiConsole.MarkupLine("[bold][green]Done![/][/]");
}

static void RandomCompliment()
{
    string[] lines = File.ReadAllLines("compliments.txt");
    var random = new Random();
    string compliment = lines[random.Next(lines.Length)];
    AnsiConsole.MarkupLine(compliment);
}

static (int Previous, int Next) GetNextBuildNumber()
{
    string root = Directory.GetCurrentDirectory().Replace("\\", "/");
    string buildNumberPath = Path.Combine(root, "build-number.txt");
    if (!File.Exists(buildNumberPath))
    {
        AnsiConsole.MarkupLine("[bold][red]Error[/][/] No [bold]build-number.txt[/] file found.");
        AnsiConsole.MarkupLine("Call [bold]dotnet run Setup.cs setup[/] first.");
        Environment.Exit(1);
    }
    int current = int.Parse(File.ReadAllText(buildNumberPath).Trim());
    int previous = current;
    int next = current + 1;
    File.WriteAllText(buildNumberPath, next.ToString());
    return (previous, next);
}

static async Task DeletePackagesAsync(string feedName, string version)
{
    string[] packages = {
        "Beamable.Common",
        "Beamable.Server.Common",
        "Beamable.Microservice.Runtime",
        "Beamable.Microservice.SourceGen",
        "Beamable.Tooling.Common",
        "Beamable.UnityEngine",
        "Beamable.UnityEngine.Addressables",
        "Beamable.Tools"
    };
    var options = new ParallelOptions
    {
        MaxDegreeOfParallelism = 3
    };

    await Parallel.ForEachAsync(packages, options, async (package, token) =>
    {
        try
        {
            await RunProcessAsync("dotnet", $"nuget delete {package} {version} --source {feedName} --non-interactive");
        }
        catch
        {
            // Log error here if needed
        }
    });
}

static async Task UpdateTemplatesAsync(string tmpBuildOutput, string version)
{
    await RunProcessAsync("dotnet", "new uninstall Beamable.Templates", true);
    string templatePackage = Path.Combine(tmpBuildOutput, $"Beamable.Templates.{version}.nupkg");
    await RunProcessAsync("dotnet", $"dotnet new install {templatePackage} --force");
}

static void InvalidateNugetCache(string version)
{
    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string packagesPath = Path.Combine(home, ".nuget/packages");
    if (Directory.Exists(packagesPath))
    {
        foreach (var dir in Directory.GetDirectories(packagesPath, "beamable.*"))
        {
            string versionPath = Path.Combine(dir, version);
            if (Directory.Exists(versionPath))
            {
                Directory.Delete(versionPath, true);
            }
        }
    }
}

static async Task RestoreAllCsprojInMicroservices(string microservicesPath)
{
    if (Directory.Exists(microservicesPath))
    {
        foreach (var csproj in Directory.GetFiles(microservicesPath, "*.csproj", SearchOption.AllDirectories))
        {
            AnsiConsole.MarkupLine($"[grey]Restoring {csproj}[/]");
            await RunProcessAsync("dotnet", $"restore \"{csproj}\" --no-cache --force", workingDirectory: microservicesPath);
        }
    }
}

static async Task<string> RunProcessAsync(string fileName, string arguments, bool canFail = false, string workingDirectory = "")
{
    using var process = new Process();
    process.StartInfo.FileName = fileName;
    process.StartInfo.Arguments = arguments;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    process.StartInfo.CreateNoWindow = true;
    if (!string.IsNullOrWhiteSpace(workingDirectory))
    {
        process.StartInfo.WorkingDirectory = workingDirectory;
    }

    process.Start();

    string output = string.Empty;
    try
    {
        await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Process {fileName} {arguments} failed with exit code {process.ExitCode}. Error: {error}");
        }
    }
    catch (Exception)
    {
        if (!canFail)
        {
            throw;
        }
    }

    return output;
}


static async Task BuildOtel(string goPath)
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var goBinDir = Path.GetDirectoryName(goPath);
    try
    {
        var bashPath = SyncSettingsOptions.GetDefaultUnixShell();
        var pathExport = string.IsNullOrEmpty(goBinDir)
        ? ""
        : $"export PATH=\"$PATH:{goBinDir}\" && ";
        await RunProcessAsync(bashPath, $"-c \"{pathExport} ./build.sh --version 0.0.123\"", workingDirectory: Path.Combine(currentDirectory, "otel-collector"));
        AnsiConsole.WriteLine("Building Otel Collector done.");
    }
    finally
    {
    }
}

static async Task<(bool Installed, string GoPath)> CheckAndInstallGo(bool acceptAnyGoVersion)
{
    bool goInstalled = false;
    // Check if Go is installed
    try
    {
        var result = await RunProcessAsync("go", "version");
        var match = Regex.Match(result, @"go(\d+\.\d+\.\d+)");
        if (match.Success)
        {
            string installedVersion = match.Groups[1].Value;
            AnsiConsole.WriteLine($"Go is installed with version: {installedVersion}");

            if (installedVersion.Contains(GO_VERSION))
            {
                goInstalled = true;
                AnsiConsole.WriteLine("Found GO installation with correct version!");
            }
            else
            {
                goInstalled = true;
                AnsiConsole.WriteLine($"This script needs GO with version {GO_VERSION}, instead found {installedVersion}");
                if (!acceptAnyGoVersion)
                {
                    Environment.Exit(1);
                }
            }
        }
    }
    catch
    {
        AnsiConsole.WriteLine("Go is not installed");
    }

    if (!goInstalled)
    {
        var (success, goPath) = await InstallGo();
        if (success)
        {
            return (true, goPath);
        }
    }
    return (goInstalled, "go");
}

static async Task<(bool Success, string GoPath)> InstallGo()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        // Check for Homebrew
        try
        {
            var brewVersion = await RunProcessAsync("brew", "--version");
            AnsiConsole.WriteLine($"Homebrew is installed");
            if (!string.IsNullOrWhiteSpace(brewVersion.Replace('\n', ' ')))
            {
                AnsiConsole.WriteLine($"Version: {brewVersion.Replace('\n', ' ')}");
            }
        }
        catch
        {
            AnsiConsole.WriteLine("Homebrew is not installed, please install it before running this script!");
            Environment.Exit(1);
        }

        try
        {
            await RunProcessAsync("brew", $"install go@{GO_VERSION}");
            AnsiConsole.WriteLine($"GO with version {GO_VERSION} was successfully installed!");
            return (true, "/opt/homebrew/bin/go");
        }
        catch
        {
            AnsiConsole.WriteLine($"Failed to install GO using Homebrew!");
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // Check for Chocolatey
        try
        {
            var chocoVersion = await RunProcessAsync("choco", "--version");
            AnsiConsole.WriteLine($"Chocolatey is installed with version: {chocoVersion.Trim()}");
        }
        catch
        {
            AnsiConsole.Write("Chocolatey is not installed, please install it before running this script!");
            Environment.Exit(1);
        }

        try
        {
            await RunProcessAsync("choco", $"install golang --version={GO_VERSION} -y");
            AnsiConsole.Write($"GO with version {GO_VERSION} was successfully installed!");
            return (true, "C:\\Go\\bin\\go.exe");
        }
        catch
        {
            AnsiConsole.Write($"Failed to install GO using Chocolatey!");
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        try
        {
            await RunProcessAsync("apt-get", "--version");
            AnsiConsole.WriteLine("apt-get is available");
        }
        catch
        {
            AnsiConsole.WriteLine("apt-get is not available, trying with sudo...");
        }

        try
        {
            await RunProcessAsync("wget", $"https://go.dev/dl/go{GO_VERSION}.linux-amd64.tar.gz");
            AnsiConsole.WriteLine($"GO with version {GO_VERSION} was successfully downloaded!");
            await RunProcessAsync("sudo", $"rm -rf /usr/local/go && sudo tar -C /usr/local -xzf go{GO_VERSION}.linux-amd64.tar.gz");
            await RunProcessAsync("rm", $"go{GO_VERSION}.linux-amd64.tar.gz");
            AnsiConsole.WriteLine($"GO with version {GO_VERSION} was successfully installed!");
            return (true, "/usr/local/go/bin/go");
        }
        catch
        {
            AnsiConsole.WriteLine($"Failed to install GO using wget!");
        }
    }
    else
    {
        AnsiConsole.Write($"Unsupported OS");
        Environment.Exit(1);
    }
    return (false, "");
}

static string GetRootDirectory()
{
    return Directory.GetCurrentDirectory().Replace("\\", "/");
}

static void LoadDevEnvironment()
{
    string devEnvPath = ".dev.env";
    if (File.Exists(devEnvPath))
    {
        string[] lines = File.ReadAllLines(devEnvPath);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string value = parts[1].Trim().Trim('"');
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

static string EscapeXmlAttribute(string value)
{
    if (string.IsNullOrEmpty(value))
        return value;

    return value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");
}

class SetupSettingsOptions
{
    public bool Verbose = false;
    public bool acceptAnyGoVersion = false;

    public void VerboseLog(string message)
    {
        if (Verbose)
        {
            AnsiConsole.MarkupLine(message);
        }
    }
}

class SyncSettingsOptions
{
    public bool Verbose = false;
    public string bash_Path = "";

    public string GetShell()
    {
        if (!string.IsNullOrWhiteSpace(bash_Path))
            return bash_Path;
        return GetDefaultUnixShell();
    }

    public void VerboseLog(string message)
    {
        if (Verbose)
        {
            AnsiConsole.MarkupLine(message);
        }
    }

    public static string GetDefaultUnixShell()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return @"C:/Program Files/Git/bin/bash.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/bin/bash";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "/usr/bin/bash";
        }
        else
        {
            // Default fallback
            return "/bin/bash";
        }
    }
}


class DevOptions
{
    public bool skipUnity = false;
    public bool skipUnreal = false;
    public bool skipSamsSandbox = false;
}
