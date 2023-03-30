using Beamable.Common;
using Beamable.Common.Dependencies;

namespace Beamable.Server
{
	public interface IMicroservicePrefixService
	{
		Promise<string> GetPrefix(string serviceName);
	}
}
