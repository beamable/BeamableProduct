using Beamable.Common;
using cli.Commands.Project;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli.FederationCommands;

/*


 _______  _______  _______    _______  _______  __   __  __   __  _______  __    _  ______
|       ||       ||       |  |       ||       ||  |_|  ||  |_|  ||   _   ||  |  | ||      |
|    ___||    ___||_     _|  |       ||   _   ||       ||       ||  |_|  ||   |_| ||  _    |
|   | __ |   |___   |   |    |       ||  | |  ||       ||       ||       ||       || | |   |
|   ||  ||    ___|  |   |    |      _||  |_|  ||       ||       ||       ||  _    || |_|   |
|   |_| ||   |___   |   |    |     |_ |       || ||_|| || ||_|| ||   _   || | |   ||       |
|_______||_______|  |___|    |_______||_______||_|   |_||_|   |_||__| |__||_|  |__||______|


 */

public class GetLocalSettingsIFederatedGameServerCommandArgs : CommandArgs, FederationLocalSettingsCommand.ILocalSettingsArgs
{
	public string BeamoId { get; set; }
	public string FederationId { get; set; }
}

public class GetLocalSettingsIFederatedGameServerCommand : AtomicCommand<GetLocalSettingsIFederatedGameServerCommandArgs, LocalSettings_IFederatedGameServer>
{
	public GetLocalSettingsIFederatedGameServerCommand() : base(typeof(IFederatedGameServer<>).GetNameWithoutGenericArity(), $"Get the local settings for the {typeof(IFederatedGameServer<>)}  local routing key")
	{
	}

	public override void Configure()
	{
		FederationLocalSettingsCommand.AddFederationLocalSettingsSharedOptions(this);
	}

	public override async Task<LocalSettings_IFederatedGameServer> GetResult(GetLocalSettingsIFederatedGameServerCommandArgs args)
	{
		// Ensure we can actually get the settings for this service.
		FederationLocalSettingsCommand.ValidateFederationLocalSettingsSharedOptions(args, typeof(IFederatedGameServer<>));

		// Read the settings.
		var settings = await ReadProjectSettingsCommand.ReadSettings(args, new() { args.BeamoId });
		var expectedLocalSettingKey = FederationUtils.BuildLocalSettingKey(typeof(IFederatedGameServer<>), args.FederationId);
		foreach (ProjectSettingsOutput s in settings.settings)
		{
			foreach (var kvp in s.settings)
			{
				if (kvp.key.Equals(expectedLocalSettingKey))
				{
					return JsonConvert.DeserializeObject<LocalSettings_IFederatedGameServer>(kvp.value);
				}
			}
		}

		// If we got to here, let's initialize it as the default
		return new LocalSettings_IFederatedGameServer { contentIds = Array.Empty<string>() };
	}
}

/*

  ________  _______  ___________       ______    ______   ___      ___  ___      ___       __      _____  ___   ________
 /"       )/"     "|("     _   ")     /" _  "\  /    " \ |"  \    /"  ||"  \    /"  |     /""\    (\"   \|"  \ |"      "\
(:   \___/(: ______) )__/  \\__/     (: ( \___)// ____  \ \   \  //   | \   \  //   |    /    \   |.\\   \    |(.  ___  :)
 \___  \   \/    |      \\_ /         \/ \    /  /    ) :)/\\  \/.    | /\\  \/.    |   /' /\  \  |: \.   \\  ||: \   ) ||
  __/  \\  // ___)_     |.  |         //  \ _(: (____/ //|: \.        ||: \.        |  //  __'  \ |.  \    \. |(| (___\ ||
 /" \   :)(:      "|    \:  |        (:   _) \\        / |.  \    /:  ||.  \    /:  | /   /  \\  \|    \    \ ||:       :)
(_______/  \_______)     \__|         \_______)\"_____/  |___|\__/|___||___|\__/|___|(___/    \___)\___|\____\)(________/

 */

public class SetLocalSettingsIFederatedGameServerCommandArgs : CommandArgs, FederationLocalSettingsCommand.ILocalSettingsArgs
{
	public string BeamoId { get; set; }
	public string FederationId { get; set; }

	public string[] ContentIds { get; set; }
}

public class SetLocalSettingsIFederatedGameServerCommand : AtomicCommand<SetLocalSettingsIFederatedGameServerCommandArgs, LocalSettings_IFederatedGameServer>
{
	public SetLocalSettingsIFederatedGameServerCommand() : base(typeof(IFederatedGameServer<>).GetNameWithoutGenericArity(), $"Get the local settings for the {typeof(IFederatedGameServer<>)}  local routing key")
	{
	}

	public override void Configure()
	{
		FederationLocalSettingsCommand.AddFederationLocalSettingsSharedOptions(this);
		var contentIdsOpt = new Option<string[]>("--content-ids", "The ids for the services you wish to deploy. Ignoring this option deploys all services") { AllowMultipleArgumentsPerToken = true };
		AddOption(contentIdsOpt, (args, i) => args.ContentIds = i.Length == 0 ? Array.Empty<string>() : i);
	}

	public override async Task<LocalSettings_IFederatedGameServer> GetResult(SetLocalSettingsIFederatedGameServerCommandArgs args)
	{
		// Ensure we can actually get the settings for this service.
		FederationLocalSettingsCommand.ValidateFederationLocalSettingsSharedOptions(args, typeof(IFederatedGameServer<>));

		// Validate the given ids. 
		{
			var notFoundContentIds = new List<string>();
			foreach (string contentId in args.ContentIds)
			{
				if (!args.ContentService.GetLocalCache("global").HasContent(contentId))
					notFoundContentIds.Add(contentId);
			}

			if (notFoundContentIds.Count > 0)
			{
				throw new CliException("Given content does not exist locally so we can't set it as a filter.", 2, true);
			}
		}

		// If we got to here, that means the content ids are valid and we can set them
		var localSettingKey = FederationUtils.BuildLocalSettingKey(typeof(IFederatedGameServer<>), args.FederationId);

		var defaultSettings = new LocalSettings_IFederatedGameServer { contentIds = args.ContentIds };

		await WriteProjectSettingsCommand.WriteSettings(args, args.BeamoId, new(),
			new() { new() { key = localSettingKey, value = JsonConvert.SerializeObject(defaultSettings) } }, true);

		return defaultSettings;
	}
}
