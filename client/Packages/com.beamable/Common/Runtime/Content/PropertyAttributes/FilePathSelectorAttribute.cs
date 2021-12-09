using UnityEngine;

namespace Beamable.Common.Content
{
	public class FilePathSelectorAttribute : PropertyAttribute
	{
		public string DialogTitle;
		public bool OnlyFiles;
		public string FileExtension;
		public string RootFolder;
		public string PathRelativeTo;

		public FilePathSelectorAttribute(bool absolutePath = false)
		{
			RootFolder = Application.dataPath;
			PathRelativeTo = absolutePath ? null : RootFolder;
		}
	}
}
