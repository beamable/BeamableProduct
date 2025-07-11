using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserCleanCapturedUsersBufferCommand : AtomicCommand<DeveloperUserCleanCapturedUsersBufferArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserCleanCapturedUsersBufferCommand() : base("clean-captured-user-buffer", "Clean up the captured users files based on a rolling buffer, the criteria is the oldest to the newer")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableIntOption("rolling-buffer-size", "The max amount of captured users that you can have before starting to delete the oldest (0 means infinity)"), (args, s) => { args.RollingBufferSize = s; });
	}
	
	public override Task<DeveloperUserResult> GetResult(DeveloperUserCleanCapturedUsersBufferArgs args)
	{
		var developerUsers = args.DeveloperUserManagerService.RemoveOlderEntriesFromCachedBuffer(args.RollingBufferSize);
		return Task.FromResult(new DeveloperUserResult
		{
			DeletedUsers = DeveloperUserManagerService.DeveloperUsersToDeveloperUsersData(developerUsers).ToList()
		});
	}
}

public class DeveloperUserCleanCapturedUsersBufferArgs : ContentCommandArgs
{
	public int RollingBufferSize;
}
