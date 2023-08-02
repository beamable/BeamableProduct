using Beamable.Server.Editor;
using System.Collections.Generic;

namespace Beamable.Server
{
	public class MicroserviceClientInfo
	{
		public string ServiceName { get; set; }
		
		public List<FederationComponent> FederationComponents { get; set; } = new List<FederationComponent>();
		public bool IsUsedForFederation => FederationComponents.Count > 0;
	}
}
