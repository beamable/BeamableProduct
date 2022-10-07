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

					public static readonly Vector2 ThemeManagerWindowSize = new Vector2(550, 700);

					public const string DELETE_STYLE_HEADER = "Delete style";
					public const string DELETE_STYLE_MESSAGE = "Are You sure You want to delete this style?";

					public const string CLEAR_ALL_PROPERTIES_HEADER = "Clear all properties";
					public const string CLEAR_ALL_PROPERTIES_MESSAGE = "Are You sure You want to clear all properties and variables?";

					public const string NO_BUSS_STYLE_SHEET_AVAILABLE =
						"There should be created at least one Buss Style Sheet and it should be referenced by Buss Element present at scene";

					public const string ADD_STYLE_BUTTON_LABEL = "Add Style";
					public const string ADD_STYLE_OPTIONS_HEADER = "Select target stylesheet";

					public const string DUPLICATE_STYLESHEET_BUTTON_LABEL = "Duplicate factory stylesheet";
					public const string DUPLICATE_STYLESHEET_OPTIONS_HEADER = "Select source stylesheet";

					public const string TOGGLE_SHOW_ALL = "Show All";
					public const string TOGGLE_HIDE_ALL = "Hide All";
				}

				public static partial class MenuItems
				{
					public const string DUPLICATE = "Duplicate";
					public const string COPY_TO = "Copy to";
					public const string COPY_INTO_NEW_STYLE_SHEET = "Copy into new style sheet";
					public const string REMOVE = "Remove";
					public const string NONE = "None";
				}

				public static partial class Paths
				{
					public const string FACTORY_STYLES_RESOURCES_PATH = "DefaultStyleSheets";
				}
			}
		}
	}
}
