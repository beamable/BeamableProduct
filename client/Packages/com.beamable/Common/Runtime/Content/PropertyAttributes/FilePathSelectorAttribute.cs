// this file was copied from nuget package Beamable.Common@5.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/5.1.0-PREVIEW.RC1

﻿using UnityEngine;

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
