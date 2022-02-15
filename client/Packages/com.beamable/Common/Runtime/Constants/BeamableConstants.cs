namespace Beamable.Common.Constants
{
	public static partial class BeamableConstants
	{
		public static class Commons
		{
			public const string OBSOLETE_WILL_BE_REMOVED = "This is no longer supported, and will be removed in the future.";
		}
		public static class Directories
		{
			public const string BEAMABLE_ASSETS = "Assets/Beamable";
			
			public const string BEAMABLE_PACKAGE = "Packages/com.beamable";
			public const string BEAMABLE_PACKAGE_EDITOR = BEAMABLE_PACKAGE + "/Editor";
			public const string BEAMABLE_PACKAGE_EDITOR_UI = BEAMABLE_PACKAGE_EDITOR + "/UI";
			
			public const string BEAMABLE_SERVER_PACKAGE = "Packages/com.beamable.server";
			public const string BEAMABLE_SERVER_PACKAGE_EDITOR = BEAMABLE_SERVER_PACKAGE + "/Editor";
			public const string BEAMABLE_SERVER_PACKAGE_EDITOR_UI = BEAMABLE_SERVER_PACKAGE_EDITOR + "/UI";
			
			public const string ASSET_DIR = BEAMABLE_ASSETS + "/DefaultAssets";
			public const string DATA_DIR = BEAMABLE_ASSETS + "/Editor/content";
			public const string DEFAULT_DATA_DIR = BEAMABLE_PACKAGE_EDITOR + "/Modules/Content/DefaultContent";
			public const string DEFAULT_ASSET_DIR = BEAMABLE_PACKAGE_EDITOR + "/Modules/Content/DefaultAssets~";
		}
		public static class URLs
		{
			public const string BEAMABLE_MAIN_WEBSITE = "https://www.beamable.com";
			public const string BEAMABLE_DOCS_WEBSITE = "https://docs.beamable.com";
			public const string BEAMABLE_BLOG_RELEASES_UNITY_SDK = "https://www.beamable.com/blog/beamable-release-unity-sdk";
			public const string BEAMABLE_LEGAL_WEBSITE = "https://app.termly.io/document/terms-of-use-for-website/c44e18e4-675f-4eeb-8fa4-a9a5267ec2c5";

			public static class Documentations
			{
				public const string ACCOUNT_HUD = BEAMABLE_DOCS_WEBSITE + "/account-hud";
				public const string ADMIN_FLOW = BEAMABLE_DOCS_WEBSITE + "/admin-flow";
				public const string ANNOUNCEMENTS_FLOW = BEAMABLE_DOCS_WEBSITE + "announcements-flow";
				public const string CALENDAR_FLOW = BEAMABLE_DOCS_WEBSITE + "/calendar-flow";
				public const string CURRENCY_HUD = BEAMABLE_DOCS_WEBSITE + "/currency-hud";
				public const string LEADERBOARD_FLOW = BEAMABLE_DOCS_WEBSITE + "/leaderboard-flow";
				public const string LOGIN_FLOW = BEAMABLE_DOCS_WEBSITE + "/login-flow";
				public const string INVENTORY_FLOW = BEAMABLE_DOCS_WEBSITE + "/inventory-flow";
				public const string STORE_FLOW = BEAMABLE_DOCS_WEBSITE + "/store-flow";
				public const string MICROSERVICES = BEAMABLE_DOCS_WEBSITE + "/microservices-feature";
			
				public const string WINDOW_CONTENT_MANAGER = BEAMABLE_DOCS_WEBSITE + "/content-manager";
				public const string WINDOW_CONTENT_NAMESPACES = BEAMABLE_DOCS_WEBSITE + "/content-manager#namespaces";
				public const string WINDOW_CONFIG_MANAGER = BEAMABLE_DOCS_WEBSITE + "/content-manager";
				public const string WINDOW_TOOLBOX = BEAMABLE_DOCS_WEBSITE + "/toolbox";
			}
		}
		public static class MenuItems
		{
			public static class Windows
			{
				public static class Names
				{
					public const string BEAMABLE = "Beamable";
					public const string CONTENT_MANAGER = "Content Manager";
					public const string CONFIG_MANAGER = "Configuration Manager";
					public const string THEME_MANAGER = "Theme Manager";
					public const string MICROSERVICES_MANAGER = "Microservices Manager";
					public const string PORTAL = "Portal";
					public const string TOOLBOX = "Toolbox";
					public const string BEAMABLE_ASSISTANT = BEAMABLE + " Assistant";
					public const string BUSS = BEAMABLE + " Styles";
					public const string BUSS_SHEET_EDITOR = "Sheet Inspector";
					public const string BUSS_WIZARD = "Theme Wizard";
					public const string LOGIN = "Beamble Login";
					public const string SDF_GENERATOR = "SDF Generator";
				}
				public static class Paths
				{
					private const string MENU_ITEM_PATH_WINDOW = "Window";
					
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE = MENU_ITEM_PATH_WINDOW + "/Beamable";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_SAMPLES = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Samples";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Help";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP_DIAGNOSTIC_DATA = MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP + "/Generate Diagnostic Info";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Utilities";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Beamable Developer";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_MICROSERVICES = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Microservices";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_POOLING = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Pooling";

					//Menu Items: Window (#ifdef BEAMABLE_DEVELOPER)
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_BEAMABLE_DEVELOPER_SAMPLES = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Samples";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_UNITY = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Unity";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Theme Manager";

					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_BUSS = "/New BUSS";
					public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_SDF_GENERATOR =
						MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + MENU_ITEM_PATH_WINDOW_BEAMABLE_BUSS + "/Open SDF Generator";

				}
				public static class Orders
				{
					public const int MENU_ITEM_PATH_WINDOW_PRIORITY_1 = 0;
					public const int MENU_ITEM_PATH_WINDOW_PRIORITY_2 = 20;
					public const int MENU_ITEM_PATH_WINDOW_PRIORITY_3 = 40;
					public const int MENU_ITEM_PATH_WINDOW_PRIORITY_4 = 60;
				}
			}
			public static class Assets
			{
				public static class Paths
				{
					public const string MENU_ITEM_PATH_ASSETS_BEAMABLE = "Beamable";
					public const string MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS = MENU_ITEM_PATH_ASSETS_BEAMABLE + "/Configurations";
					public const string MENU_ITEM_PATH_ASSETS_BEAMABLE_SAMPLES = MENU_ITEM_PATH_ASSETS_BEAMABLE + "/Samples";
				}
				public static class Orders
				{
					public const int MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1 = 0;
					public const int MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_2 = 50;
					public const int MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_3 = 100;
					public const int MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_LAST = int.MaxValue;
				}
			}
		}
	}
}
