using Beamable.Common.BeamCli;

namespace cli.Services.Content;

[CliContractType]
public enum ContentStatus
{
	Created = 0,
	Deleted = 1,
	Modified = 2,
	UpToDate = 3
}

public class LocalContent
{
	public string contentId;
	public ContentStatus status;
	public string[] tags;
	public string hash;

	public string StatusString() =>
		status switch
		{
			ContentStatus.Created => "[green]created[/]",
			ContentStatus.Deleted => "[red]deleted[/]",
			ContentStatus.Modified => "[yellow]modified[/]",
			ContentStatus.UpToDate => "up to date",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
}
