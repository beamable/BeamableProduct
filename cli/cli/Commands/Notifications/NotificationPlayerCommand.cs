using Beamable.Api.Autogenerated.Auth;
using Beamable.Api.Autogenerated.Models;
using Beamable.Api.Autogenerated.Realms;
using Beamable.Common.Content;
using cli.Utils;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;

namespace cli.Notifications;

public class NotificationPlayerCommandArgs : CommandArgs
{
	public string contextRegexStr;
}

public class NotificationPlayerOutput
{
	public string context;
	public string payload;
}

class NotificationPlayerModel
{
	public string messageFull;
	public string context;

	public NotificationPlayerModel() { }
	public NotificationPlayerModel(string messageFull, string context)
	{
		this.messageFull = messageFull;
		this.context = context;
	}
}

public class NotificationPlayerCommand : StreamCommand<NotificationPlayerCommandArgs, NotificationPlayerOutput>
{
	public NotificationPlayerCommand()
		: base("player", "Listen for player notifications")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>(new string[] { "--context", "-c" }, () => ".*",
			"A regex to filter for notification channels"), (args, s) => args.contextRegexStr = s);
	}

	public override async Task Handle(NotificationPlayerCommandArgs args)
	{
		var regex = new Regex(args.contextRegexStr);


		var ws = new ClientWebSocket();
		var cancelToken = new CancellationToken();
		var config = await args.DependencyProvider.GetService<IRealmsApi>().GetClientDefaults();

		Log.Debug($"provider=[{config.websocketConfig.provider}] url=[{config.websocketConfig.uri.Value}]");

		if (config.websocketConfig.provider == "pubnub")
		{
			throw new CliException(
				$@"Only realms with beam notifications are supported. This realm currently has {config.websocketConfig.provider}.
Try setting the realm config to beam with this command, 
""beam config realm set --key-values 'notification|publisher::beamable'""");
		}


		var authApi = args.DependencyProvider.GetService<IBeamAuthApi>();
		var tokenAuthResult = await authApi.PostRefreshToken(new RefreshTokenAuthRequest
		{
			customerId = new OptionalString(args.AppContext.Cid),
			realmId = new OptionalString(args.AppContext.Pid),
			refreshToken = new OptionalString(args.AppContext.RefreshToken)
		});

		ws.Options.SetRequestHeader("Authorization", $"Bearer {tokenAuthResult.accessToken.Value}");
		await ws.ConnectAsync(new Uri(config.websocketConfig.uri + "/connect"), cancelToken);

		while (ws.State != WebSocketState.Closed)
		{
			var content = await WebsocketUtil.ReadMessage(ws, cancelToken);
			var message = JsonConvert.DeserializeObject<NotificationPlayerModel>(content);
			if (!regex.IsMatch(message.context)) continue;
			Log.Information($"{message.context} -- {message.messageFull}");
			SendResults(new NotificationPlayerOutput
			{
				context = message.context,
				payload = message.messageFull
			});
		}

	}
}