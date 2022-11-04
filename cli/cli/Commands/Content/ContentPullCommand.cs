using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Newtonsoft.Json;

namespace cli.Content;

public class ContentPullCommand : AppCommand<ContentPullCommandArgs>
{
	private readonly CliRequester _requester;
	private string ManifestId = "global";
	public ContentPullCommand(CliRequester requester) : base("pull", "Pulls currently deployed content")
	{
		_requester = requester;
	}

	public override void Configure()
	{
	}

	public override async Task Handle(ContentPullCommandArgs args)
	{
		string url = $"/basic/content/manifest/public?id={ManifestId}";
		var request = await _requester.Request(Method.GET, url, null, true, ClientManifest.ParseCSV, true).Recover(ex =>
		{
			if (ex is RequesterException err && err.Status == 404)
			{
				return new ClientManifest { entries = new List<ClientContentInfo>() };
			}

			throw ex;
		});
		
		BeamableLogger.Log(JsonConvert.SerializeObject(request, Formatting.Indented));
	}
}

public class ContentPullCommandArgs : CommandArgs
{
}
