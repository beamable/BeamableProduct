// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407161549
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407161549

namespace Beamable.Server
{
	public interface IRealmInfo
	{
		string CustomerID { get; }
		string ProjectName { get; }
	}
}
