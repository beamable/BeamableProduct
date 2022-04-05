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

				public const string GLOBAL_STYLE_SHEET_NAME = "BeamableGlobalStyleSheet";

				public const string DEFAULT_GLOBAL_STYLE_SHEET_PATH =
					Directories.BEAMABLE_PACKAGE_EDITOR_CONFIG + GLOBAL_STYLE_SHEET_NAME + ".asset";
				
				public const string STYLE_SHEETS_PATH = Directories.BEAMABLE_ASSETS + "/BussStyleSheets";
				public const string BEAMABLE_GLOBAL_STYLE_SHEET_PATH = STYLE_SHEETS_PATH + GLOBAL_STYLE_SHEET_NAME + ".asset";
			}
		}
	}
}
