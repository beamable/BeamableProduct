// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using Beamable.Common.Api.Inventory;
using System;
using System.Collections.Generic;

namespace Beamable.Common
{
	public interface IFederatedInventory<in T> : IFederatedLogin<T> where T : IFederationId, new()
	{
		Promise<FederatedInventoryProxyState> GetInventoryState(string id);

		Promise<FederatedInventoryProxyState> StartInventoryTransaction(
			string id,
			string transaction,
			Dictionary<string, long> currencies,
			List<FederatedItemCreateRequest> newItems,
			List<FederatedItemDeleteRequest> deleteItems,
			List<FederatedItemUpdateRequest> updateItems);
	}

	[Serializable]
	public class FederatedInventoryCurrency
	{
		public string name;
		public long value;
	}

	[Serializable]
	public class FederatedInventoryProxyState
	{
		public Dictionary<string, long> currencies;
		public Dictionary<string, List<FederatedItemProxy>> items;
	}

	[Serializable]
	public class FederatedItemProxy
	{
		public string proxyId;
		public List<ItemProperty> properties;
	}
}
