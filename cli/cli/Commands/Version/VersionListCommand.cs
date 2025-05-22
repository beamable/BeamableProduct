using cli.Services;
using System.CommandLine;
using Beamable.Server;

namespace cli.Version;

public class VersionListCommandArgs : CommandArgs
{
	public int limit;
	public bool includeRc;
	public bool includeProd;
}
public class VersionListCommand : AppCommand<VersionListCommandArgs>, IStandaloneCommand
{
	public VersionListCommand() : base("list", "Show the most recent available versions")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<int>("--limit", () => 10, "How many package versions to display"), (args, i) => args.limit = i);
		AddOption(new Option<bool>("--include-rc", () => false, "Should release candidates be shown"), (args, i) => args.includeRc = i);
		AddOption(new Option<bool>("--include-release", () => true, "Should stable releases be shown"), (args, i) => args.includeProd = i);
		AddAlias("ls");
	}

	public override async Task Handle(VersionListCommandArgs args)
	{
		var service = args.DependencyProvider.GetService<VersionService>();
		var data = await service.GetBeamableToolPackageVersions();

		switch (args.includeRc, args.includeProd)
		{
			case (true, false):
				data = data.Where(d => d.packageVersion.Contains("preview.rc")).ToArray();
				break;
			case (false, true):
				data = data.Where(d => !d.packageVersion.Contains("preview")).ToArray();
				break;
			case (true, true):
				data = data.Where(d => !d.packageVersion.Contains("preview.nightly")).ToArray();
				break;
			case (false, false):
				Log.Warning("Must either get release candidates or full release builds.");
				return;
		}


		var set = data.TakeLast(args.limit).Reverse();

		foreach (var q in set)
		{
			Log.Information(q.packageVersion);
		}
	}
}
