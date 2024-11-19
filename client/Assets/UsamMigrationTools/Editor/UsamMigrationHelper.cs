using Beamable.Editor;
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
		
		[MenuItem("Usam Migration/Copy Storage With Assembly Ref")]
		public static void CopyStorageWithAssemblyRef()
		{
			CopyScenario("StorageWithAsmRef");
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
		
		[MenuItem("Usam Migration/Copy Local Package")]
		public static void CopyLocalPackage()
		{
			CopyScenario("SimpleLocalPackage/LegacyProjectPackage", "Packages/LegacyProjectPackage");
		}
		
		
		public static void CopyScenario(string folderPath, string destFolder=null)
		{
			var sourceFolder = $"LegacyBeamableServices/{folderPath}";
			destFolder ??= $"Assets/UsamMigrations/{folderPath}";
			
			FileUtils.CopyDirectory(sourceFolder, destFolder, true);
			AssetDatabase.Refresh();
		}
		
	}
}
