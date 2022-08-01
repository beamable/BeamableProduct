using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestDeleteCommand : BaseRequestCommand
{
	public BaseRequestDeleteCommand(CliRequester requester, IAppContext ctx, IAuthApi authApi) : base(requester,ctx, authApi, "delete", "base DELETE request command") { }
	protected override Method Method => Method.DELETE;

}
