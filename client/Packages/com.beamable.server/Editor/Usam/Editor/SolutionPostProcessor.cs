using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class SolutionPostProcessor : AssetPostprocessor
	{
		public static bool OnPreGeneratingCSProjectFiles()
		{
			AssemblyUtil.Reload();
			CsharpProjectUtil.GenerateAllReferencedAssemblies();
			return false; // if we don't return false, then this methods PREVENTS Unity from generating csproj files what-so-ever.
		}
	}
}
