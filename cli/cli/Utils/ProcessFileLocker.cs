using System.Diagnostics;

namespace cli.Utils
{
	public class ProcessFileLocker
	{
		private readonly ConfigService _configService;

		public ProcessFileLocker(ConfigService configService)
		{
			_configService = configService;
		}
		
		public async Task<bool> LockFile(string fileLockName, int processId)
		{
			bool isLocked = false;
			if (string.IsNullOrEmpty(fileLockName))
			{
				throw new Exception("Lock file needs to have a name");
			}

			string lockFile = Path.Combine(_configService.ConfigTempDirectoryPath!, $"lock.{Path.GetFileNameWithoutExtension(fileLockName)}");
			if (File.Exists(lockFile))
			{
				string fileProcessId = await File.ReadAllTextAsync(lockFile);
				if (int.TryParse(fileProcessId, out int fileProcessIdInt) && IsProcessRunning(fileProcessIdInt))
				{
					isLocked = true;
				}
			}

			if (isLocked)
				return false;
			await File.WriteAllTextAsync(lockFile, processId.ToString());
			return true;
		}

		public async Task<bool> UnlockFile(string fileLockName, int processId)
		{
			if (string.IsNullOrEmpty(fileLockName))
			{
				throw new Exception("Lock file needs to have a name");
			}
			
			string lockFile = Path.Combine(_configService.ConfigTempDirectoryPath!, $"lock.{Path.GetFileNameWithoutExtension(fileLockName)}");
			if (!File.Exists(lockFile))
			{
				return true;
			}

			string fileProcessId = await File.ReadAllTextAsync(lockFile);
			
			if (!int.TryParse(fileProcessId, out int fileProcessIdInt) || fileProcessIdInt == processId)
			{
				File.Delete(lockFile);
				return true;
			}

			return false;
		}
		
		public static bool IsProcessRunning(int processId)
		{
			try
			{
				Process process = Process.GetProcessById(processId);
				return !process.HasExited; 
			}
			catch (ArgumentException)
			{
				return false;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
		}
	}
}
