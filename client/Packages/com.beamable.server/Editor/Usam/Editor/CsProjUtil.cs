namespace Beamable.Server.Editor.Usam
{
	public class CsProjUtil
	{
		public static bool OnPreGeneratingCSProjectFiles(UsamService usam)
		{
			usam.AssemblyService.Reload();
			CsharpProjectUtil.GenerateAllReferencedAssemblies(usam);
			return false; // if we don't return false, then this methods PREVENTS Unity from generating csproj files what-so-ever.
		}
	}
}
