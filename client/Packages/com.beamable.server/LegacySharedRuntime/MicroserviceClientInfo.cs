// this file was copied from nuget package Beamable.Server.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.0-PREVIEW.RC2


using System.Collections.Generic;

namespace Beamable.Server.Editor
{
	public class MicroserviceClientInfo
	{
		public string ServiceName { get; set; }

		public List<FederationComponent> FederationComponents { get; set; } = new List<FederationComponent>();
		public bool IsUsedForFederation => FederationComponents.Count > 0;
	}
}
