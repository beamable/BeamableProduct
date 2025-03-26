// this file was copied from nuget package Beamable.Server.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Server.Common/4.1.5

namespace Beamable.Server
{
	public interface IRealmInfo
	{
		string CustomerID { get; }
		string ProjectName { get; }
	}
}
