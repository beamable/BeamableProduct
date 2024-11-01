using System;
using System.IO;
using UnityEditor.PackageManager;

namespace Beamable.Editor.BeamCli.Commands
{
	[Serializable]
	[Flags]
	public enum BeamManifestEntryFlags
	{
		IS_SERVICE = 1,
		IS_STORAGE = 2,
		IS_READONLY = 4
	}

	public static class BeamManifestEntryFlagsExtensions
	{
		public static bool HasFlagType(this BeamManifestEntryFlags flag, BeamManifestEntryFlags test)
		{
			return flag.HasFlag(test);
		}
	}
	
	public partial class BeamManifestServiceEntry
	{
		/// <summary>
		/// if the .csproj file is coming from a "readonly" source in Unity, then this bool is true.
		/// Readonlys are
		/// 1. Library/PackageCache
		/// 2. folders outside the root folder. 
		/// </summary>
		public bool IsReadonlyPackage;

		public BeamManifestEntryFlags Flags;
		
		public static bool IsReadonlyProject(string csProjPath)
		{
			var root = System.Environment.CurrentDirectory;
			var full = Path.GetFullPath(csProjPath);

			var isChildOfProject = full.StartsWith(root);
			
			if (!isChildOfProject)
				return true;

			var isPackageCached = csProjPath.StartsWith("Library/PackageCache");
			if (isPackageCached)
				return true;

			return false;
		}
	}
}
