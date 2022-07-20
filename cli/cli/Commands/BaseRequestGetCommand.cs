using Beamable.Common.Api;

namespace cli;

public class BaseRequestGetCommand : BaseRequestCommand
{
	public BaseRequestGetCommand(IBeamableRequester requester) : base(requester, "get", "base GET request command") { }
	protected override Method Method => Method.GET;
}
