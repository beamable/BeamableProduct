using System.CommandLine;

namespace cli.Content;

public class ContentCreateLocalManifestCommandArgs : CommandArgs
{
    public string manifestId;
}

public class ContentCreateLocalManifestCommandResult
{
    
}

public class ContentCreateLocalManifestCommand : AtomicCommand<ContentCreateLocalManifestCommandArgs, ContentCreateLocalManifestCommandResult>
{
    public ContentCreateLocalManifestCommand() : base("new-manifest", "Create a local empty content manifest for the current realm")
    {
    }

    public override void Configure()
    {
        AddArgument(new Argument<string>("manifest-id", "The manifest id to create locally"),
            (args, i) => args.manifestId = i);
    }

    public override Task<ContentCreateLocalManifestCommandResult> GetResult(ContentCreateLocalManifestCommandArgs args)
    {
        var contentService = args.DependencyProvider.GetService<ContentService>();
        var realmFolder = contentService.GetContentRealmFolder();
        var manifestFolder = Path.Combine(realmFolder, args.manifestId);
        Directory.CreateDirectory(manifestFolder);
        return Task.FromResult(new ContentCreateLocalManifestCommandResult());
    }
}