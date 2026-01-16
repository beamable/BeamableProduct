#!/usr/bin/env dotnet run
#:package clapnet@0.2.*
#:package Spectre.Console.Cli@0.53.0

using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Spectre.Console;

const string GO_VERSION = "1.24";

return clapnet.CommandBuilder.New()
    .With(Setup, "Setup")
    .With(SyncRiderSettings, "Sync Rider Settings", "sync")
    .With(SyncRiderUnity, "Sync Rider Settings for Unity(shortcut command)", "sync-unity")
    .With(SyncRiderUnreal, "Sync Rider Settings for Unreal(shortcut command)", "sync-unreal")
    .Run(args);


void Setup(SetupSettingsOptions cfg)
{
    AnsiConsole.MarkupLine("Setting up the [bold][blue]Beamables[/][/]!");

    // Load environment variables from .dev.env file
    LoadDevEnvironment();
    string root = GetRootDirectory();
    string sourceFolderPath = Path.Combine(root, Environment.GetEnvironmentVariable("SOURCE_FOLDER") ?? "");
    Console.WriteLine("Creating build number file");
    File.WriteAllText(Path.Combine(root, "build-number.txt"), "0");
    var feedName = Environment.GetEnvironmentVariable("FEED_NAME") ?? "BeamableNugetSource";
    var goCheck = CheckAndInstallGo(cfg.acceptAnyGoVersion);
    var dotnetConfigRestore = RunProcessAsync("dotnet", $"tool restore --tool-manifest ./cli/cli/.config/dotnet-tools.json");
    var configCreationTask = SetupTemplateDotNetConfig();
    Console.WriteLine($"Setting up {feedName} at {sourceFolderPath}");
    SetupNuGetSource(sourceFolderPath, feedName).Wait();
    var (goInstalled, goPath) = goCheck.Result;
    configCreationTask.Wait(1000 * 60 * 5);
    dotnetConfigRestore.Wait(1000 * 60 * 5);
    BuildOtel(goPath).Wait(1000 * 60 * 60);
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

    if(!isUnity && !isUnreal)
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
        Console.WriteLine("Removing old source (if none exists, you'll see an error 'Unable to find any package', but that is okay)");
        try
        {
            await RunProcessAsync("dotnet", $"nuget remove source {feedName}");
        }
        catch
        {
            // Ignore errors when removing non-existent source
        }

        // Add new source
        Console.WriteLine("Adding new source!");
        await RunProcessAsync("dotnet", $"nuget add source \"{sourceFolderPath}\" --name {feedName}");
}
static async Task<string> RunProcessAsync(string fileName, string arguments)
{
    using var process = new Process();
    process.StartInfo.FileName = fileName;
    process.StartInfo.Arguments = arguments;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    process.StartInfo.CreateNoWindow = true;

    process.Start();

    string output = await process.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new Exception($"Process {fileName} {arguments} failed with exit code {process.ExitCode}. Error: {error}");
    }

    return output;
}

static async Task BuildOtel(string goPath)
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var goBinDir = Path.GetDirectoryName(goPath);
    try{
        Directory.SetCurrentDirectory(Path.Combine(currentDirectory,"otel-collector"));
        var bashPath = SyncSettingsOptions.GetDefaultUnixShell();
        AnsiConsole.WriteLine("Building Otel Collector...");
        var pathExport = string.IsNullOrEmpty(goBinDir) 
        ? "" 
        : $"export PATH=\"$PATH:{goBinDir}\" && ";
    await RunProcessAsync(bashPath, $"{pathExport} build.sh --version 0.0.123");
        AnsiConsole.WriteLine("Building Otel Collector done.");
    }
    finally {
        Directory.SetCurrentDirectory(currentDirectory);
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
            Console.WriteLine($"Go is installed with version: {installedVersion}");

            if (installedVersion.Contains(GO_VERSION))
            {
                goInstalled = true;
                Console.WriteLine("Found GO installation with correct version!");
            }
            else
            {
                goInstalled = true;
                Console.WriteLine($"This script needs GO with version {GO_VERSION}, instead found {installedVersion}");
                if(!acceptAnyGoVersion)
                {
                    Environment.Exit(1);
                }
            }
        }
    }
    catch
    {
        Console.WriteLine("Go is not installed");
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
            Console.WriteLine($"Homebrew is installed");
            Console.WriteLine($"Version: {brewVersion.Split('\n')[0]}");
        }
        catch
        {
            Console.WriteLine("Homebrew is not installed, please install it before running this script!");
            Environment.Exit(1);
        }

        try
        {
            await RunProcessAsync("brew", $"install go@{GO_VERSION}");
            Console.WriteLine($"GO with version {GO_VERSION} was successfully installed!");
            return (true, "/opt/homebrew/bin/go");
        }
        catch
        {
            Console.WriteLine($"Failed to install GO using Homebrew!");
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // Check for Chocolatey
        try
        {
            var chocoVersion = await RunProcessAsync("choco", "--version");
            Console.WriteLine($"Chocolatey is installed with version: {chocoVersion.Trim()}");
        }
        catch
        {
            Console.WriteLine("Chocolatey is not installed, please install it before running this script!");
            Environment.Exit(1);
        }

        try
        {
            await RunProcessAsync("choco", $"install golang --version={GO_VERSION} -y");
            Console.WriteLine($"GO with version {GO_VERSION} was successfully installed!");
            return (true, "C:\\Go\\bin\\go.exe");
        }
        catch
        {
            Console.WriteLine($"Failed to install GO using Chocolatey!");
        }
    }
    else
    {
        Console.WriteLine($"Unsupported OS");
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

class SetupSettingsOptions {
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

class SyncSettingsOptions {
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
