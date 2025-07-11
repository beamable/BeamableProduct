using Beamable.Common.BeamCli.Contracts;
using Spectre.Console;

namespace cli.Content;

public class ContentStatusCommand : AppCommand<ContentStatusCommandArgs>
{
	const int DEFAULT_TABLE_LIMIT = 100;

	private ContentService _contentService;

	public ContentStatusCommand() : base("status", "Show current status of the content")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddOption(new ConfigurableOptionFlag("show-up-to-date", "Show up to date content"), (args, b) => args.showUpToDate = b);
		AddOption(new ConfigurableIntOption(nameof(ContentStatusCommandArgs.limit), "Limit content displayed amount (default: 100)"), (args, s) => args.limit = s);
		AddOption(new ConfigurableIntOption(nameof(ContentStatusCommandArgs.skip), "Skips content amount"), (args, s) => args.skip = s);
	}

	public override async Task Handle(ContentStatusCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };

		var localContentFileTasks = args.ManifestIds.Select(async m => await _contentService.GetAllContentFiles(manifestId: m)).ToArray();
		var localContentFiles = await Task.WhenAll(localContentFileTasks);

		for (var i = 0; i < localContentFiles.Length; i++)
		{
			var files = localContentFiles[i];
			DisplayStatusTable(files, args.showUpToDate, args.limit, args.skip);
		}
	}

	public static void DisplayStatusTable(LocalContentFiles files, bool showUpToDate, int limit, int skipAmount)
	{
		var totalCount = files.ContentFiles.Count;
		var table = new Table();
		table.AddColumn("Current status");
		table.AddColumn("ID");
		table.AddColumn(new TableColumn("tags").RightAligned());

		var toShow = files.ContentFiles;
		if (!showUpToDate)
		{
			toShow = toShow.Where(content => content.GetStatus() is not ContentStatus.UpToDate).ToList();
		}

		if (!showUpToDate && toShow.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]Your local content is up to date with remote.[/]");
			return;
		}


		var paginatedToShow = toShow.Skip(skipAmount).Take(limit > 0 ? limit : DEFAULT_TABLE_LIMIT).ToList();
		foreach (var content in paginatedToShow)
		{
			var tags = content.GetTagStatus().Select(pair =>
			{
				switch (pair.Value)
				{
					case TagStatus.LocalOnly:
						return $"[green][[+]]{pair.Key}[/]";
					case TagStatus.RemoteOnly:
						return $"[red][[-]]{pair.Key}[/]";
					case TagStatus.LocalAndRemote:
						return pair.Key;
					default:
						throw new ArgumentOutOfRangeException();
				}
			});
			table.AddRow(content.GetStatusString(), content.Id, string.Join(",", tags));
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine($"Content: {paginatedToShow.Count} out of {totalCount}");
	}
}

public class ContentStatusCommandArgs : ContentCommandArgs
{
	public bool showUpToDate;
	public int limit;
	public int skip;
}
