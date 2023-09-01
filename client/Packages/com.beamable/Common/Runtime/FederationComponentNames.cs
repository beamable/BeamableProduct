using Beamable.Common;
using System;
using System.Collections.Generic;

namespace beamable.common.Runtime
{
	public static class FederationComponentNames
	{
		public static readonly Dictionary<Type, string> FederationComponentToName = new Dictionary<Type, string>
		{
			[typeof(IFederatedLogin<>)] = "IFederatedLogin",
			[typeof(IFederatedInventory<>)] = "IFederatedInventory",
			[typeof(ISupportsFederatedLogin<>)] = "IFederatedLogin",
			[typeof(IFederatedInventory<>)] = "IFederatedInventory"
		};
	}
}
