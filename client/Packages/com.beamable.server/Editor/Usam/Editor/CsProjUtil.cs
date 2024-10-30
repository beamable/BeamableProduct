namespace Beamable.Server.Editor.Usam
{
	public class CsProjUtil
	{
		public static bool OnPreGeneratingCSProjectFiles(UsamService usam)
		{
			
			AssemblyUtil.Reload(usam);
			CsharpProjectUtil.GenerateAllReferencedAssemblies(usam.Cli);
			return false; // if we don't return false, then this methods PREVENTS Unity from generating csproj files what-so-ever.
		}
	}
}
