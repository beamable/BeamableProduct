namespace cli.Utils;

public static class OtelUtils
{
	public static void SetAllowOtelConfig(bool AllowOtel, ConfigService configService)
	{
		var otelConfig = configService.LoadOtelConfigFromFile();

		otelConfig.BeamCliAllowTelemetry = AllowOtel;
		
		configService.SaveOtelConfigToFile(otelConfig);
	}
	
	public static bool GetAllowOtelConfig(ConfigService configService)
	{
		return configService.LoadOtelConfigFromFile().BeamCliAllowTelemetry;
	}

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
