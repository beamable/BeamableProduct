using Beamable.Common.BeamCli;
using cli.Content;
using cli.Dotnet;
using cli.Services.DeveloperUserManager;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli.DeveloperUserCommands;

public class DeveloperUserPsCommand : AppCommand<DeveloperUserPsArgs>, IResultSteam<DefaultStreamResultChannel, DeveloperUserPsCommandEvent>, ISkipManifest
{
	public DeveloperUserPsCommand() : base("ps", "The ps commands manage the files for the developer users folders, it can keep watching changes in the file system and emit events to the engine")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddWatchOption(this, (args, i) => args.Watch = i);
		AddOption(new RequireProcessIdOption(), (args, i) => args.RequireProcessId = i);
	}

	public override async Task Handle(DeveloperUserPsArgs args)
	{
		RequireProcessIdOption.ConfigureRequiredProcessIdWatcher(args.RequireProcessId);
		
		var developerUserManagerService = args.DeveloperUserManagerService;
		
		// Get all available users for each type
		var allAvailableEntries = DeveloperUserManagerService.DeveloperUsersToDeveloperUsersData(developerUserManagerService.GetAllAvailableUserInfo());
		
		// Build and emit the event
		var eventToEmit = new DeveloperUserPsCommandEvent()
		{
			EventType = DeveloperUserPsCommandEvent.EVT_TYPE_FullRebuild,
			DeveloperUserReport = new DeveloperUserResult()
			{
				UpdatedUsers = allAvailableEntries.ToList()
			}
		};
		
		this.SendResults(eventToEmit);
		this.LogResult(eventToEmit);
		
		// If we are meant to watch the developer user folders, let's set up jobs to do that.
		if (args.Watch)
		{
			
			
			// Set up a task that will emit a DeveloperUserPsCommandEvent event every time local files changed.
			// The events will only contain the entries for the modified content objects and expects the engine integration to only rebuild their in-memory representation of the modified object.
			List<Task> waitTasks = new List<Task>();
			foreach (DeveloperUserType developerUserType in Enum.GetValues(typeof(DeveloperUserType)))
			{
				// This ensure that only one of the two event streams will resolve at a time.
				var watchSemaphore = new SemaphoreSlim(1, 1);
				
				waitTasks.Add(Task.Run(async () =>
				{
					var tokenSource = new CancellationTokenSource();
					await foreach (var batchedLocalFileChanges in developerUserManagerService.ListenToLocalDeveloperUserFileChanges(developerUserType, tokenSource.Token))
					{
						await watchSemaphore.WaitAsync(tokenSource.Token);

						try
						{
							var allDeletions = batchedLocalFileChanges.AllFileChanges.Where(fc => fc.WasDeleted()).Select(fc => fc.GamerTag).ToArray();
							var allUpdates = batchedLocalFileChanges.AllFileChanges.Where(fc => fc.WasChanged() || fc.WasCreated() || fc.WasRenamed()).Select(fc => fc.GamerTag).ToList();

							var allDeveloperUsers = DeveloperUserManagerService.DeveloperUsersToDeveloperUsersData(developerUserManagerService.GetAllAvailableUserInfo());
							
							var developerUserPsCommandEvent = new DeveloperUserPsCommandEvent()
							{
								EventType = DeveloperUserPsCommandEvent.EVT_TYPE_ChangedDeveloperUserInfo,
								DeveloperUserReport = new DeveloperUserResult()
								{
									UpdatedUsers = allDeveloperUsers.Where(developerUser => allUpdates.Contains(developerUser.GamerTag)).ToList(),
									DeletedUsers = allDeletions.Select(gamerTag => new DeveloperUserData
									{
										GamerTag = gamerTag
									}).ToList(),
								}
							};
						
							// Send it out.
							this.SendResults(developerUserPsCommandEvent);
							this.LogResult(developerUserPsCommandEvent);
						}
						finally
						{
							watchSemaphore.Release();
						}
					}
				}));
			}

			await Task.WhenAll(waitTasks);
		}
	}
	
	protected virtual void LogResult(object result)
	{
		var json = JsonConvert.SerializeObject(result, Formatting.Indented);
		AnsiConsole.Write(
			new Panel(new JsonText(json))
				.Collapse()
				.NoBorder());
	}
}

public class DeveloperUserPsArgs : CommandArgs
{
	public bool Watch;
	public int RequireProcessId;
}

[CliContractType, Serializable]
public class DeveloperUserPsCommandEvent
{
	/// <summary>
	/// The engine integration is expected to discard all their in-memory state about developer users and rebuild it with the information in this event.
	/// </summary>
	public const int EVT_TYPE_FullRebuild = 0;
	
	/// <summary>
	/// The 
	/// </summary>
	public const int EVT_TYPE_ChangedDeveloperUserInfo = 1;

	/// <summary>
	/// One of <see cref="EVT_TYPE_FullRebuild"/>,  <see cref="EVT_TYPE_ChangedDeveloperUserInfo"/>.
	/// The semantics of each field are defined based on the event and documented on these comments.
	/// </summary>
	public int EventType;

	/// <summary>
	/// Using the same structured that is returned from the CLI calls to return as a report in the PS command
	/// </summary>
	public DeveloperUserResult DeveloperUserReport;
}


