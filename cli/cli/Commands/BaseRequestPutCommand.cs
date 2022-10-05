using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestPutCommand : BaseRequestCommand
{
	public BaseRequestPutCommand(CliRequester requester) : base(requester, "put", "base PUT request command") { }
	protected override Method Method => Method.PUT;
}
