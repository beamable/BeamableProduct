namespace cli;

public class CliEnvironment
{
	public CliEnvironment()
	{
		Api = Environment.GetEnvironmentVariable(Constants.KEY_ENV_API) ?? string.Empty;
		Cid = Environment.GetEnvironmentVariable(Constants.KEY_ENV_CID) ?? string.Empty;
		Pid = Environment.GetEnvironmentVariable(Constants.KEY_ENV_PID) ?? string.Empty;
		AccessToken = Environment.GetEnvironmentVariable(Constants.KEY_ENV_ACCESS_TOKEN) ?? string.Empty;
		RefreshToken = Environment.GetEnvironmentVariable(Constants.KEY_ENV_REFRESH_TOKEN) ?? string.Empty;
		ConfigDir = Environment.GetEnvironmentVariable(Constants.KEY_ENV_CONFIG_DIR) ?? string.Empty;
		LogLevel = Environment.GetEnvironmentVariable(Constants.KEY_ENV_LOG_LEVEL) ?? string.Empty;
	}

	private string Api { get; }
	private string Cid { get; }
	private string Pid { get; }
	private string AccessToken { get; }
	private string RefreshToken { get; }
	private string ConfigDir { get; }
	public string LogLevel { get; }


	private static readonly Type[] SUPPORTED_OPTION_TYPES = new[] { typeof(HostOption), typeof(CidOption), typeof(PidOption), typeof(AccessTokenOption), typeof(RefreshTokenOption), typeof(ConfigDirOption) };

	public bool TryGetFromOption(ConfigurableOption option, out string var)
	{
		if (SUPPORTED_OPTION_TYPES.Contains(option.GetType()))
		{
			var = GetFromOption(option);
			return true;
		}

		var = string.Empty;
		return false;
	}

	private string GetFromOption(ConfigurableOption option)
	{
		return option switch
		{
			HostOption => Api,
			CidOption => Cid,
			PidOption => Pid,
			AccessTokenOption => AccessToken,
			RefreshTokenOption => RefreshToken,
			ConfigDirOption => ConfigDir,
			_ => throw new ArgumentException($"Unsupported Option Type given! Type={option.GetType().Name} does not have an Environment Variable fallback!")
		};
	}
}
