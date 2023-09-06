using System;
using System.Collections.Generic;

namespace Beamable.Common.Runtime
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
