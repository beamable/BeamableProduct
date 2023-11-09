using System;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class BeamServiceSignpost
	{
		public string name;
		public string assetRelativePath;
		public string relativeDockerFile;
		public string relativeProjectFile;

		public string[] assemblyReferences;
	}
}
