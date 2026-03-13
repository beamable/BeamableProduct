// this file was copied from nuget package Beamable.Server.Common@4.3.6-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.6-PREVIEW.RC1

namespace Beamable.Server
{
	public interface IRealmInfo
	{
		string CustomerID { get; }
		string ProjectName { get; }
	}
}
