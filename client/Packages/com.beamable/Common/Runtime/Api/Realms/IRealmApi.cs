// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System.Collections.Generic;

namespace Beamable.Common.Api.Realms
{
	public interface IRealmsApi
	{
		Promise<CustomerView> GetCustomerData();
		Promise<List<RealmView>> GetGames();
		Promise<RealmView> GetRealm();
		Promise<List<RealmView>> GetRealms(RealmView game = null);
		Promise<List<RealmView>> GetRealms(string pid);
		Promise<RealmConfigData> GetRealmConfig();
		Promise<bool> UpdateRealmConfig(Dictionary<string, string> request);
	}
}
