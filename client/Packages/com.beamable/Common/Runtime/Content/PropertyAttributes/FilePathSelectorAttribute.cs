// This file generated by a copy-operation from another project. 
// Edits to this file will be overwritten by the build process. 

using UnityEngine;

namespace Beamable.Common.Content
{
	public class FilePathSelectorAttribute : PropertyAttribute
	{
		public string DialogTitle;
		public bool OnlyFiles;
		public string FileExtension;
		public string RootFolder => Application.dataPath;
		public string PathRelativeTo;

		public FilePathSelectorAttribute(bool absolutePath = false)
		{
#if UNITY_EDITOR
			PathRelativeTo = absolutePath ? null : RootFolder;
#endif
		}
	}
}
