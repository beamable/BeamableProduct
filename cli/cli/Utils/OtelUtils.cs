namespace cli.Utils;

public static class OtelUtils
{
	public static void SetOtelConfig(bool UseOtel, ConfigService configService)
	{
		configService.SetDefaultConfigString(Constants.CONFIG_OTEL, UseOtel.ToString());
		configService.FlushDefaultConfig();
	}
	
	public static bool GetOtelConfig(ConfigService configService)
	{
		if (bool.TryParse(configService.GetDefaultConfigString(Constants.CONFIG_OTEL), out bool result))
		{
			return result;
		}

		return false;
	}

	public static bool HasOtelConfig(ConfigService configService)
	{
		return !string.IsNullOrEmpty(configService.GetDefaultConfigString(Constants.CONFIG_OTEL));
	}
	
	public static void CreateOtelFolders(ConfigService configService)
	{
		var otelFolders = GetOtelFolders(configService);
		
		foreach (var folder in otelFolders)
		{
			if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
		}
	}

	public static bool HasAllOtelFolders(ConfigService configService)
	{
		bool containsOtelFolder = true;

		string[] otelFolders = GetOtelFolders(configService);

		foreach (var otelFolder in otelFolders)
		{
			containsOtelFolder &= Directory.Exists(otelFolder);
		}
		
		return containsOtelFolder;
	}

	public static string[] GetOtelFolders(ConfigService configService)
	{
		return new string[]
		{
			configService.ConfigTempOtelLogsDirectoryPath, configService.ConfigTempOtelMetricsDirectoryPath,
			configService.ConfigTempOtelTracesDirectoryPath
		};
	}
}
