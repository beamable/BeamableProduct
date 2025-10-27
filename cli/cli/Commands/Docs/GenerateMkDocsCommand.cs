using System.CommandLine;
using System.Text;
using Beamable.Server;
using cli.Services;

namespace cli.Docs;

public class GenerateMkDocsCommandArgs : CommandArgs
{
    public string outputDirectory;
}

public class GenerateMkDocsCommandResult
{
    
}
public class GenerateMkDocsCommand 
    : AtomicCommand<GenerateMkDocsCommandArgs, GenerateMkDocsCommandResult>
    , IStandaloneCommand
    , ISkipManifest
{
    public override bool IsForInternalUse => true;

    public GenerateMkDocsCommand() : base("mkdocs", "Generate the mkdoc command suite documentation")
    {
        
    }

    public override void Configure()
    {
        AddOption(new Option<string>(new string[] { "--output", "-o" }, () => "cli-command-docs",
            "A folder where the output docs will be written"), (args,i) => args.outputDirectory = i);
    }

    public override async Task<GenerateMkDocsCommandResult> GetResult(GenerateMkDocsCommandArgs args)
    {
        // build a markdown file for each command...
        var generatorContext = args.DependencyProvider.GetService<CliGenerator>().GetCliContext();

        var docService = args.DependencyProvider.GetService<DocService>();

        var summarySb = new StringBuilder();
        generatorContext.Commands.Sort((a, b) => a.executionPath.CompareTo(b.executionPath));
        
        foreach (var command in generatorContext.Commands)
        {
            if (command == generatorContext.Root) continue;
            if (!(command.command is IAppCommand appCommand)) continue;

            
            // var hasChildren = command.command.Subcommands.Count > 0;
            var subPath = command.ExecutionPathAsFilePath(out var shouldExist);
          
            var title = command.ExecutionPathAsCapitalizedStringWithoutBeam(" ");
            // only include summary entry if it is a public command
            
            
            
            if (!command.IsInternalSubtree)
            {
                var length = command.executionPath.Split(' ').Length - 2;
                Log.Information("len " + length);
                for (var i = 0; i < length; i++)
                {
                    summarySb.Append("\t");
                }

                if (shouldExist)
                {                
                    summarySb.AppendLine($"- [{command.command.Name}]({subPath})");
                }
                else
                {
                    summarySb.AppendLine($"- {command.command.Name}");

                }
            }
            
          
            
            var doc = docService.Render(command);
            
            var path = Path.Combine(args.outputDirectory, subPath);
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(path, doc);
        }

        var summaryText = summarySb.ToString();
        await File.WriteAllTextAsync(Path.Combine(args.outputDirectory, "SUMMARY.md"), summaryText);

        return new GenerateMkDocsCommandResult();
    }
}