// this file was copied from nuget package Beamable.Server.Common@3.0.2
// https://www.nuget.org/packages/Beamable.Server.Common/3.0.2

namespace Beamable.Server
{
	public interface IRealmInfo
	{
		string CustomerID { get; }
		string ProjectName { get; }
	}
}
