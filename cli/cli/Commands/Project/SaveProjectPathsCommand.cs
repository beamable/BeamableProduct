using System.CommandLine;

namespace cli.Commands.Project;

public class SaveProjectPathsCommandArgs : CommandArgs
{
    public List<string> addExtraPathsToFile = new List<string>();
    public List<string> pathsToIgnore = new List<string>();
}

public class SaveProjectPathsCommandResults
{
    
}
public class SaveProjectPathsCommand : AtomicCommand<SaveProjectPathsCommandArgs, SaveProjectPathsCommandResults>, ISkipManifest
{
    public SaveProjectPathsCommand() : base("add-paths", "Add extra paths and ignore paths for services")
    {
    }
    
    public override void Configure()
    {
        AddOption(
            new Option<List<string>>(new string[] { "--save-extra-paths" }, () => new List<string>(),
                "Overwrite the stored extra paths for where to find projects")
            {
                AllowMultipleArgumentsPerToken = true,
                Arity = ArgumentArity.ZeroOrMore
            },
            (args, i) =>
            {
                args.addExtraPathsToFile = i;
            });
		
        AddOption(
            new Option<List<string>>(new string[] { "--paths-to-ignore" }, () => new List<string>(),
                "Paths to ignore when searching for services")
            {
                AllowMultipleArgumentsPerToken = true,
                Arity = ArgumentArity.ZeroOrMore
            },
            (args, i) =>
            {
                args.pathsToIgnore = i;
            });
    }
    
    public static void SaveExtraPathFiles(ConfigService configService, SaveProjectPathsCommandArgs args)
    {
        // save the extra-paths and the paths to ignore to the config folder
        configService.SaveExtraPathsToFile(args.addExtraPathsToFile);
        configService.SavePathsToIgnoreToFile(args.pathsToIgnore);
        configService.FlushConfig();
    }

    public override Task<SaveProjectPathsCommandResults> GetResult(SaveProjectPathsCommandArgs args)
    {
        SaveExtraPathFiles(args.ConfigService, args);
        return Task.FromResult(new SaveProjectPathsCommandResults());
    }
}
