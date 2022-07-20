using Beamable.Common.Api;

namespace cli;

public class BaseRequestSetCommand : BaseRequestCommand
{
	public BaseRequestSetCommand(IBeamableRequester requester) : base(requester, "delete", "base DELETE request command") { }
	protected override Method Method => Method.DELETE;
	
}
