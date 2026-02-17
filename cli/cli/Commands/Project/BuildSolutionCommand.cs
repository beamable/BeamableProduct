using System.Reflection;
using System.Text.Json;
using Beamable.Server;
using cli.OtelCommands;
using cli.Services;
using cli.Utils;
using CliWrap;
using microservice.Extensions;
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
    
    // I haven't thought too hard about this yet; this command
    //  was developed as a way to test solution-level building
    //  for usage in the plan/release flows. 
    public override bool IsForInternalUse => true;

    public BuildSolutionCommand() : base("build-sln", "Builds all local projects with a temp solution file")
    {
    }

    public override void Configure()
    {
        SolutionCommandArgs.ConfigureSolutionFlag(this, _ => throw new CliException("Must have a valid .beamable folder"));
    }

    public override async Task Handle(BuildSolutionCommandArgs args)
    {
        var results = await Build(args);
        foreach (var (beamoId, result) in results)
        {
            if (!result.Success)
            {
                Log.Error($"failed to build {beamoId}");
                foreach (var error in result.report.errors)
                {
                    Log.Error($" {error.formattedMessage}");
                }
            }
        }
    }

    public static async Task<Dictionary<string, BuildImageSourceOutput>> Build<TArgs>(TArgs args, 
        bool forDeployment=true,
        bool forceCpu=true,
        int maxParallelCount=8)
        where TArgs : CommandArgs, IHasSolutionFileArg
    {
        var beamo = args.BeamoLocalSystem;
        var resultMap = new Dictionary<string, BuildImageSourceOutput>();
        
        
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
        
        var dotnetPath = args.AppContext.DotnetPath;
        var slnPath = args.SolutionFilePath;
        var buildLogFile = Path.Combine(errorPathDir, "publish.json");


        var customLoggerDllPath = Assembly.GetExecutingAssembly().Location;
        var customLoggerArg = $"cli.Utils.{nameof(MsBuildSolutionLogger)},{customLoggerDllPath}";

        var productionArgs = forDeployment
            ? "-p:BeamGenProps=\"disable\" -p:GenerateClientCode=\"false\" -p:CopyToLinkedProjects=\"false\""
            : "";
        var runtimeArg = forceCpu
            ? $"--runtime unix-x64 -p:BeamPlatform=lin -p:BeamRunningArchitecture=x64 -p:BeamCollectorPlatformArchArg=\"--platform {DownloadCollectorCommand.OS_LINUX} --arch {DownloadCollectorCommand.ARCH_X64}\" "
            : $"--use-current-runtime ";
        var buildArgs = $"publish {slnPath} " +
                        $"--verbosity minimal " +
                        $"--no-self-contained {runtimeArg} " +
                        $"--configuration Release " +
                        $"-maxcpucount:{maxParallelCount} " +

                        // make sure the builds produce a deterministic output so that docker imageIds end up being the same.
                        $"-p:Deterministic=\"True\" " +
                        $"{productionArgs} " +

                        // use a custom logger, so we can get the errors back for each
                        //  project one by one
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
                
                // control where the custom log file goes
                [MsBuildSolutionLogger.LOG_PATH_ENV_VAR] = buildLogFile,
                
                // this makes it so that no projects publish, unless those projects
                //  explicitly set the `IsPublishable` property to true. Our
                //  microservices do this in the .props file when BeamPublish it set.
                ["IsPublishable"] = "false"
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                if (line == null) return;
                Log.Verbose(line);
            }))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(cts.Token);
        await command;

        { // read the build log
            string json = "{}";
            if (File.Exists(buildLogFile))
            {
                json = File.ReadAllText(buildLogFile);
            }
            var results = JsonSerializer.Deserialize<SolutionLogs>(json, new JsonSerializerOptions
            {
                IncludeFields = true
            });

            foreach (var (projectPath, result) in results.projects)
            {
                foreach (var (beamoId, http) in beamo.BeamoManifest.HttpMicroserviceLocalProtocols)
                {
                    if (http.Metadata.absolutePath == projectPath)
                    {
                        var projectFolder = Path.GetDirectoryName(http.Metadata.absolutePath);
                        var projectBuildDirSupport = Path.Combine(projectFolder, buildDirSupport);
                        var projectBuildDirApp = Path.Combine(projectFolder, buildDirApp);
                        var projectBuildDirRoot = Path.Combine(projectFolder, buildDirRoot);

                        resultMap[beamoId] = new BuildImageSourceOutput
                        {
                            service = beamoId,
                            outputDirApp = projectBuildDirApp,
                            outputDirSupport = projectBuildDirSupport,
                            outputDirRoot = projectBuildDirRoot,
                            report = new ProjectErrorReport
                            {
                                isSuccess = result.success,
                                errors = result.errors.Select(x => new ProjectErrorResult
                                {
                                    line = x.lineNumber,
                                    column = x.colNumber,
                                    level = "error",
                                    formattedMessage = $"error {x.code}: {x.message}",
                                    uri = x.file
                                }).ToList()
                            }
                        };
                    }
                }
            }

        }


        { // copy all the tid-bits out of /support into /app
            foreach (var (beamoId, http) in beamo.BeamoManifest.HttpMicroserviceLocalProtocols)
            {
                if (!resultMap.TryGetValue(beamoId, out var result))
                {
                    // this build has failed, and there is no point in file-copying...
                    continue;
                }
                
                if (!Directory.Exists(result.outputDirSupport))
                {
                    // the build failed and there is nothing to move. 
                    continue;
                }
                
                Directory.CreateDirectory(result.outputDirApp);
                
                var filesToMove = Directory.GetFiles(result.outputDirSupport, beamoId + ".*", SearchOption.TopDirectoryOnly);
                foreach (var fileToMove in filesToMove)
                {
                    var target = Path.Combine(result.outputDirApp, Path.GetFileName(fileToMove));
                    File.Move(fileToMove, target);
                }
            }
        }


        return resultMap;
    }
}