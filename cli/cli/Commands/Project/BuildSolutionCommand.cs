using System.Reflection;
using System.Text.Json;
using cli.Services;
using cli.Utils;
using CliWrap;
using microservice.Extensions;
using Serilog;
using Task = System.Threading.Tasks.Task;

namespace cli.Commands.Project;

public class BuildSolutionCommandArgs : CommandArgs, IHasSolutionFileArg
{
    public string SlnFilePath;


    public string SolutionFilePath
    {
        get => SlnFilePath;
        set => SlnFilePath = value;
    }
}

public class BuildSolutionCommandResults
{
    
}
public class BuildSolutionCommand : StreamCommand<BuildSolutionCommandArgs, BuildSolutionCommandResults>
{
    public BuildSolutionCommand() : base("build-sln", "Builds all local projects with a temp solution file")
    {
    }

    public override void Configure()
    {
        SolutionCommandArgs.ConfigureSolutionFlag(this, _ => throw new CliException("Must have a valid .beamable folder"));
    }

    public override async Task Handle(BuildSolutionCommandArgs args)
    {
        await Build(args);
    }

    public static async Task Build<TArgs>(TArgs args)
        where TArgs : CommandArgs, IHasSolutionFileArg
    {
        var beamo = args.BeamoLocalSystem;
        
        // var slnName = "temp.sln";
        // var slnPath = Path.GetFullPath(Path.Combine(args.ConfigService.BaseDirectory, ".beamable", "temp", "buildsln", slnName));
        // if (File.Exists(slnPath))
        // {
        //     File.Delete(slnPath);
        // }
        //
        // await OpenSolutionCommand.CreateSolution(args, slnPath);

        var buildDirRoot = Path.Combine("bin", "beamApp");
        var buildDirSupport = Path.Combine(buildDirRoot, "support");
        var buildDirApp = Path.Combine(buildDirRoot, "app");

        var errorPathDir = Path.Combine(args.ConfigService.ConfigDirectoryPath, "temp", "buildLogs");
        Directory.CreateDirectory(errorPathDir);

        { // before doing the solution build, clean up all existing /bin/beamApp folders
            foreach (var (beamoId, http) in beamo.BeamoManifest.HttpMicroserviceLocalProtocols)
            {
                var projectFolder = Path.GetDirectoryName(http.Metadata.absolutePath);
                var projectDirRoot = Path.Combine(projectFolder, buildDirRoot);
                if (Directory.Exists(projectDirRoot))
                {
                    Directory.Delete(projectDirRoot, true);
                }
            }
        }
        
        var forDeployment = true;
        var forceCpu = true;
        var dotnetPath = args.AppContext.DotnetPath;
        var slnPath = args.SolutionFilePath;
        var buildLogFile = Path.Combine(errorPathDir, "publish.json");


        var customLoggerDllPath = Assembly.GetExecutingAssembly().Location;
        var customLoggerArg = $"cli.Utils.{nameof(MsBuildSolutionLogger)},{customLoggerDllPath}";

        var productionArgs = forDeployment
            ? "-p:BeamGenProps=\"disable\" -p:GenerateClientCode=\"false\" -p:CopyToLinkedProjects=\"false\""
            : "";
        var runtimeArg = forceCpu
            ? "--runtime unix-x64"
            : "--use-current-runtime";
        var buildArgs = $"publish {slnPath} " +
                        $"--verbosity minimal " +
                        $"--no-self-contained {runtimeArg} " +
                        $"--configuration Release " +

                        // make sure the builds produce a deterministic output so that docker imageIds end up being the same.
                        $"-p:Deterministic=\"True\" " +
                        $"-p:ErrorLogDirectory={errorPathDir.EnquotePath()} " +
                        $"{productionArgs} " +

                        $"-logger:{customLoggerArg.EnquotePath()} " + 
                        // put the entire build in the support directly- 
                        //  and after wards, we will copy only the app pieces to the 
                        //  /app folder.
                        $"-p:PublishDir={buildDirSupport.EnquotePath()} " +
                        
                        // trick; do a "publish" command, but make nothing publishable. 
                        //  this prevents any projects from actually being published
                        //  EXCEPT the ones that pay attention to the 'BeamPublish' flag.
                        //  Microservice projects override `IsPublishable` when `BeamPublish`
                        //  is enabled. 
                        $"-p:BeamPublish=\"true\"";
        
        Log.Verbose($"Running dotnet publish {buildArgs}");
        using var cts = new CancellationTokenSource();

        var command = CliExtensions.GetDotnetCommand(dotnetPath, buildArgs)
            .WithEnvironmentVariables(new Dictionary<string, string>
            {
                ["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1", 
                ["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1",
                
                ["BEAM_MSBUILD_LOG_PATH"] = buildLogFile,
                // this makes it so that no projects publish, unless those projects
                //  explicitly set the `IsPublishable` property to true. Our
                //  microservices do this in the .props file when BeamPublish it set.
                ["IsPublishable"] = "false"
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                if (line == null) return;
                Log.Information(line);
                // logMessage?.Invoke(new ServicesBuildCommandOutput
                // {
                //     message = line
                // });
            }))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(cts.Token);
        // BuildImageSourceOutput

        await command;

        { // read the build log
            var json = File.ReadAllText(buildLogFile);
            var log = JsonSerializer.Deserialize<SolutionLogs>(json, new JsonSerializerOptions
            {
                IncludeFields = true
            });

        }


        { // copy all the tid-bits out of /support into /app
            foreach (var (beamoId, http) in beamo.BeamoManifest.HttpMicroserviceLocalProtocols)
            {
                var projectFolder = Path.GetDirectoryName(http.Metadata.absolutePath);
                var projectBuildDirSupport = Path.Combine(projectFolder, buildDirSupport);
                var projectBuildDirApp = Path.Combine(projectFolder, buildDirApp);

                if (!Directory.Exists(projectBuildDirSupport))
                {
                    // the build failed and there is nothing to move. 
                    continue;
                }
                
                Directory.CreateDirectory(projectBuildDirApp);
                
                var filesToMove = Directory.GetFiles(projectBuildDirSupport, beamoId + ".*", SearchOption.TopDirectoryOnly);
                foreach (var fileToMove in filesToMove)
                {
                    var target = Path.Combine(projectBuildDirApp, Path.GetFileName(fileToMove));
                    File.Move(fileToMove, target);
                }
            }
        }
        

    }
}