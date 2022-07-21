using Beamable.Common.Api;

namespace cli;

public class BaseRequestGetCommand : BaseRequestCommand
{
	public BaseRequestGetCommand(CliRequester requester) : base(requester, "get", "base GET request command") { }
	protected override Method Method => Method.GET;
}
