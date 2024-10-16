using Beamable.Common.BeamCli;
using System;
using System.Collections.Generic;

namespace Beamable.Server
{
	// these types are shims and represent architectural nonsense. 
	//  the actual implementations are in the common.tooling package, 
	//  but we cannot reference that in Unity, and since the types
	//  are marked as CliContractType, they won't be generated. 
	//  Unity won't serialize these ANYWAY, because of the dictionary issue. 
	[Serializable, CliContractType]
	public class FederationsConfig : Dictionary<string, FederationInstanceConfig[]>
	{
		public FederationsConfig() { }
	}

	public class FederationInstanceConfig
	{
		public string @interface;
	}
}
