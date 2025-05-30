using cli.Utils;
using Newtonsoft.Json;
using System.CommandLine;
using Beamable.Server;

namespace cli.Notifications;

public class NotificationServerCommandArgs : CommandArgs
{
	public bool noFilterList;
}

public class NotificationServerOutput
{
	public string path;
	public string body;
}

public class NotificationServerCommand : StreamCommand<NotificationServerCommandArgs, NotificationServerOutput>
{
	public NotificationServerCommand()
		: base("server", "Listen to server events")
	{
	}

	public override void Configure()
	{
		var noFilterOpt = new Option<bool>("--no-filter", () => false,
			"When true, do not send any approved list of messages, such that all server messages will be sent");
		noFilterOpt.AddAlias("-n");
		AddOption(noFilterOpt, (args, i) => args.noFilterList = i);
	}

	public override async Task Handle(NotificationServerCommandArgs args)
	{
		var cancelToken = new CancellationToken();
		var handle = WebsocketUtil.ConfigureWebSocketForServerNotifications(args, new[] { "content.manifest", "realm-config.refresh", "beamo.service_registration_changed" }, cancelToken);
		await WebsocketUtil.RunServerNotificationListenLoop(handle, message =>
		{
			var bodyJson = JsonConvert.SerializeObject(message.body);
			Log.Information($"{message.path} -- {bodyJson}");
			SendResults(new NotificationServerOutput { path = message.path, body = bodyJson });
		}, cancelToken);
	}
}
