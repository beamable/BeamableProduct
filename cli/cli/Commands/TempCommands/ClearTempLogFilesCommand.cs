using Serilog;
using System.CommandLine;

namespace cli.TempCommands;

[Serializable]
public class ClearTempLogFilesCommandArgs : CommandArgs
{
	public string olderThan;
}

[Serializable]
public class ClearTempLogFilesCommandOutput
{
	public List<string> deletedFiles = new List<string>();
	public List<string> failedToDeleteFiles = new List<string>();
}

public class ClearTempLogFilesCommand : AtomicCommand<ClearTempLogFilesCommandArgs, ClearTempLogFilesCommandOutput>
{
	public ClearTempLogFilesCommand() : base("logs", "Clear temp log files")
	{
	}

	public override void Configure()
	{
		var olderThanOption = new Option<string>(
			name: "--older-than",
			description: "Only clear logs older than a given value. " +
						 "This string should be in a duration format.\n\n " +
						 "The duration format should be a number, followed by a time unit. Valid time units " +
						 "include seconds (s), minutes (m), hours (h), days (d), and months(mo). Please note that " +
						 "the month unit is short-hand for 30 days. " +
						 "Here are a few examples, \n" +
						 "	--older-than 30m (30 minutes)\n" +
						 "  --older-than 18mo (18 months)\n" +
						 "  --older-than 12d (12 days)\n");
		olderThanOption.AddAlias("-ot");
		AddOption(olderThanOption, (args, i) =>
		{
			args.olderThan = i;
		});
	}

	public override Task<ClearTempLogFilesCommandOutput> GetResult(ClearTempLogFilesCommandArgs args)
	{
		TimeSpan olderThan = default;
		if (string.IsNullOrEmpty(args.olderThan))
		{
			olderThan = TimeSpan.Zero;
		}
		else if (!TryParse(args.olderThan, out olderThan))
		{
			throw new CliException("Invalid --older-than value.");
		}
		return Task.FromResult(CleanLogs(args.AppContext, olderThan));
	}


	public static ClearTempLogFilesCommandOutput CleanLogs(IAppContext appCtx, TimeSpan olderThan)
	{
		if (!appCtx.TryGetTempLogFilePath(out var logFile))
		{
			throw new CliException("there is no log path available to clean");
		}
		var dir = Path.GetDirectoryName(Path.GetFullPath(logFile));
		Log.Debug($"cleaning log files in dir=[{dir}]");

		var entries = Directory.GetFiles(dir);
		Log.Debug($"found {entries.Length} log files");

		return CleanLogs(olderThan, entries);
	}

	public static ClearTempLogFilesCommandOutput CleanLogs(TimeSpan olderThan, string[] entries)
	{
		var now = DateTime.Now;
		var deletedFiles = new List<string>();
		var failedFiles = new List<string>();
		for (var i = 0; i < entries.Length; i++)
		{
			var timeDiff = now - File.GetLastWriteTime(entries[i]);
			var shouldDelete = timeDiff > olderThan;
			if (shouldDelete)
			{
				Log.Debug($"Deleting log file=[{entries[i]}]");
				try
				{
					File.Delete(entries[i]);
					deletedFiles.Add(entries[i]);
				}
				catch
				{
					Log.Warning($"Unable to delete log file=[{entries[i]}]");
					failedFiles.Add(entries[i]);
				}
			}
		}

		return new ClearTempLogFilesCommandOutput { deletedFiles = deletedFiles, failedToDeleteFiles = failedFiles };
	}

	public static bool TryParse(string str, out TimeSpan value)
	{
		value = default;
		ReadOnlySpan<char> span = str;
		// scan through the str looking for the first non-number character
		int i = 0;
		for (i = 0; i < str.Length; i++)
		{
			var c = span[i];
			if (!char.IsDigit(c))
			{
				break;
			}
		}

		var digitsSpan = span.Slice(0, i);
		var unitSpan = span.Slice(i);

		if (!int.TryParse(digitsSpan, out var amount))
		{
			return false;
		}

		switch (unitSpan.Length)
		{
			case 1:
				switch (unitSpan[0])
				{
					case 's':
						value = TimeSpan.FromSeconds(amount);
						break;
					case 'm':
						value = TimeSpan.FromMinutes(amount);
						break;
					case 'h':
						value = TimeSpan.FromHours(amount);
						break;
					case 'd':
						value = TimeSpan.FromDays(amount);
						break;
					default:
						return false;
				}
				break;
			case 2:
				if (unitSpan[0] == 'm' && unitSpan[1] == 'o')
				{
					value = TimeSpan.FromDays(amount * 30); // TODO: rough approximation of months.
					break;
				}
				else
				{
					return false;
				}
			default:
				return false;
		}

		return true;
	}
}
