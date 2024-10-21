using System.IO;
using UnityEditor;

namespace Beamable.User.UsamMigrationHelper
{
	public class UsamMigrationHelper
	{
		[MenuItem("Usam Migration/Copy Simple")]
		public static void CopySimple()
		{
			CopyScenario("SimpleTest");
		}
		
		[MenuItem("Usam Migration/Copy Simple (with file)")]
		public static void CopySimpleWithCode()
		{
			CopyScenario("SimpleTestWithFile");
		}

		[MenuItem("Usam Migration/Copy Service and Storage")]
		public static void CopyServiceAndStorage()
		{
			CopyScenario("ServiceAndStorage");
		}
		
		
		public static void CopyScenario(string folderPath)
		{
			var sourceFolder = $"LegacyBeamableServices/{folderPath}";
			var destFolder = $"Assets/UsamMigrations/{folderPath}";
			
			CopyDirectory(sourceFolder, destFolder, true);
			AssetDatabase.Refresh();
		}
		
		// taken from: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
		static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			// Get information about the source directory
			var dir = new DirectoryInfo(sourceDir);

			// Check if the source directory exists
			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			// Cache directories before we start copying
			DirectoryInfo[] dirs = dir.GetDirectories();

			// Create the destination directory
			Directory.CreateDirectory(destinationDir);

			// Get the files in the source directory and copy to the destination directory
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath);
			}

			// If recursive and copying subdirectories, recursively call this method
			if (recursive)
			{
				foreach (DirectoryInfo subDir in dirs)
				{
					string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
					CopyDirectory(subDir.FullName, newDestinationDir, true);
				}
			}
		}
	}
}
