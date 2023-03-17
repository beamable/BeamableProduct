using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestDeleteCommand : BaseRequestCommand
{
	public BaseRequestDeleteCommand() : base("delete", "Base DELETE request command") { }
	protected override Method Method => Method.DELETE;

}
