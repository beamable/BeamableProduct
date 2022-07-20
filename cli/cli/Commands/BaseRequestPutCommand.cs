using Beamable.Common.Api;

namespace cli;

public class BaseRequestPutCommand : BaseRequestCommand
{
	public BaseRequestPutCommand(IBeamableRequester requester) : base(requester, "put", "base PUT request command") { }
	protected override Method Method => Method.PUT;
}
