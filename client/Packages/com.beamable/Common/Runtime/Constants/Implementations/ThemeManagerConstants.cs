using UnityEngine;

namespace Beamable.Common
{
	public static partial class Constants
	{
		public static partial class Features
		{
			public static partial class Buss
			{
				public static partial class ThemeManager
				{
					public const string BUSS_THEME_MANAGER_PATH = BASE_PATH + "/ThemeManager";

					public static readonly Vector2 THEME_MANAGER_WINDOW_SIZE = new Vector2(550, 700);

					public const string DELETE_STYLE_HEADER = "Delete style";
					public const string DELETE_STYLE_MESSAGE = "Are You sure You want to delete this style?";

					public const string CLEAR_ALL_PROPERTIES_HEADER = "Clear all properties";
					public const string CLEAR_ALL_PROPERTIES_MESSAGE = "Are You sure You want to clear all properties?";

					public const string NO_BUSS_STYLE_SHEET_AVAILABLE =
						"There should be created at least one Buss Style Sheet and it should be referenced by Buss Element present at scene";
				}

				public static partial class MenuItems
				{
					public const string DUPLICATE = "Duplicate";
					public const string COPY_TO = "Copy to";
					public const string COPY_INTO_NEW_STYLE_SHEET = "Copy into new style sheet";
					public const string REMOVE = "Remove";
				}

				public static partial class Paths
				{
					public const string FACTORY_STYLES_RESOURCES_PATH = "DefaultStyleSheets";
				}
			}
		}
	}
}
