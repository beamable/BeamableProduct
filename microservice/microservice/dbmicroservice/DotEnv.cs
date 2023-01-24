namespace Beamable.Server;

using System;
using System.IO;

public static class DotEnv
{
	/// <summary>
	/// Load an env file into the current process.
	/// The .env file should have 1 expression per line, in the format of
	/// NAME=VALUE
	/// </summary>
	/// <param name="filePath"></param>
	public static void Load(string filePath=".env")
	{
		if (!File.Exists(filePath))
			return;

		foreach (var line in File.ReadAllLines(filePath))
		{
			var parts = line.Split(
				'=',
				StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length != 2)
				continue;

			Environment.SetEnvironmentVariable(parts[0], parts[1]);
		}
	}
}
