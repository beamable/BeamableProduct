// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

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
			[typeof(IFederatedInventory<>)] = "IFederatedInventory",
			[typeof(IFederatedGameServer<>)] = "IFederatedGameServer"
		};
	}
}
