using System.IO;
using UnityEngine;

namespace Beamable.Common
{
	public static partial class Constants
	{
		public static partial class Features
		{
			public static partial class Buss
			{
				public const string BASE_PATH = Directories.BEAMABLE_PACKAGE_EDITOR_UI + "/Buss";
				public const string COMPONENTS_PATH = BASE_PATH + "/Components";
				public const string STYLE_SHEETS_PATH = Directories.BEAMABLE_ASSETS + "/StyleSheets";
				public const string BEAMABLE_GLOBAL_STYLE_SHEET_PATH = STYLE_SHEETS_PATH + "/BeamableGlobalStyleSheet.asset";
			}
		}
	}
}
