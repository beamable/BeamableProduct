using Beamable.Api;
using Beamable.Common;
using Beamable.Config;

namespace Beamable
{

	public class ConfigPlatformHostResolver : IPlatformRequesterHostResolver
	{
		private readonly ConfigDatabaseProvider _config;
		private EnvironmentData _env;
		public string Host => _config.HostUrl;
		public PackageVersion PackageVersion => _env.SdkVersion;

		public ConfigPlatformHostResolver(ConfigDatabaseProvider config, EnvironmentData env)
		{
			_env = env;
			_config = config;
		}
	}
}
