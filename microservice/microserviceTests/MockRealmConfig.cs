using Beamable.Common;
using Beamable.Server.Api.RealmConfig;
using System.Collections.Generic;

namespace microserviceTests;

public class MockRealmConfig: IRealmConfigService
{
	public Promise<RealmConfig> GetRealmConfigSettings()
	{
		return Promise<RealmConfig>.Successful(new RealmConfig(new Dictionary<string, RealmConfigNamespaceData>()));
	}

	public void UpdateLogLevel()
	{
	}
}
