namespace cli.Utils;

public static class OtelUtils
{
	/// <summary>
	/// Set the value of the allow otel in the otel config
	/// </summary>
	/// <param name="AllowOtel"></param>
	/// <param name="configService"></param>
	public static void SetAllowOtelConfig(bool AllowOtel, ConfigService configService)
	{
		var otelConfig = configService.LoadOtelConfigFromFile();

		// We just save the otel if the config is different or there's no configuration at all
		bool shouldSaveOtelFile = AllowOtel != otelConfig.BeamCliAllowTelemetry || !HasOtelConfig(configService);
		
		otelConfig.BeamCliAllowTelemetry = AllowOtel;

		if (shouldSaveOtelFile)
		{
			configService.SaveOtelConfigToFile(otelConfig);
		}
	}
	
	/// <summary>
	/// Check the value of the allow otel in the config Defaults: false
	/// </summary>
	/// <param name="configService"></param>
	/// <returns></returns>
	public static bool GetAllowOtelConfig(ConfigService configService)
	{
		return configService.LoadOtelConfigFromFile().BeamCliAllowTelemetry;
	}

	/// <summary>
	/// Check if the user have the otel config in the files
	/// </summary>
	/// <param name="configService"></param>
	/// <returns></returns>
	public static bool HasOtelConfig(ConfigService configService)
	{
		return configService.ExistsOtelConfig();
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
	
	public static void DeleteOtelFolders(ConfigService configService)
	{
		if (Directory.Exists(configService.ConfigTempOtelDirectoryPath))
		{
			DeleteFolder(configService.ConfigTempOtelDirectoryPath);
		}
	}

	private static void DeleteFolder(string path)
	{
		// Delete all files
		foreach (string file in Directory.GetFiles(path))
		{
			try
			{
				File.Delete(file);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to delete file: {file}. Reason: {ex.Message}");
			}
		}

		// Recurse into subdirectories
		foreach (string dir in Directory.GetDirectories(path))
		{
			DeleteFolder(dir);
		}

		// Now delete the empty directory
		try
		{
			Directory.Delete(path);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to delete directory: {path}. Reason: {ex.Message}");
		}
	}

	/// <summary>
	/// Check if all folders for the otel exist in the correct path
	/// </summary>
	/// <param name="configService"></param>
	/// <returns></returns>
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
