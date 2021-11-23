public static class BeamableConstants
{ 
    public const string DATA_DIR = "Assets/Beamable/Editor/content";
   public const string ASSET_DIR = "Assets/Beamable/DefaultAssets";
   public const string DEFAULT_DATA_DIR = "Packages/com.beamable/Editor/Modules/Content/DefaultContent";
   public const string DEFAULT_ASSET_DIR = "Packages/com.beamable/Editor/Modules/Content/DefaultAssets~";
   public const string BEAMABLE_ASSET_GROUP = "Beamable Assets";

   public static string DEFAULT_MANIFEST_ID = "global";

   //Websites
   public const string BEAMABLE_MAIN_WEBSITE = "beamable.com";
   public const string BEAMABLE_DOCS_WEBSITE = "docs.beamable.com";
   public const string BEAMABLE_LEGAL_WEBSITE = "https://app.termly.io/document/terms-of-use-for-website/c44e18e4-675f-4eeb-8fa4-a9a5267ec2c5";

   //Window Names
   public const string BEAMABLE = "Beamable";
   public const string CONTENT_MANAGER = "Content Manager";
   public const string CONFIG_MANAGER = "Configuration Manager";
   public const string THEME_MANAGER = "Theme Manager";
   public const string MICROSERVICES_MANAGER = "Microservices Manager";
   public const string PORTAL = "Portal";
   public const string TOOLBOX = "Toolbox";
   public const string BUSS = BEAMABLE + " Styles";
   public const string BUSS_SHEET_EDITOR = "Sheet Inspector";
   public const string BUSS_WIZARD = "Theme Wizard";
   public const string LOGIN = "Beamble Login";
   public const string SDF_GENERATOR = "SDF Generator";

   //Help Urls
   public const string URL_BEAMABLE_MAIN_WEBSITE = "http://www.beamable.com";
   public const string URL_BEAMABLE_DOCS_WEBSITE = "http://docs.beamable.com";
   public const string URL_BEAMABLE_BLOG_RELEASES_UNITY_SDK = "https://www.beamable.com/blog/beamable-releases-unity-sdk";

   //
   public const string URL_FEATURE_ACCOUNT_HUD = "https://docs.beamable.com/docs/account-hud";
   public const string URL_FEATURE_ADMIN_FLOW = "https://docs.beamable.com/docs/admin-flow";
   public const string URL_FEATURE_ANNOUNCEMENTS_FLOW = "https://docs.beamable.com/docs/announcements-flow";
   public const string URL_FEATURE_CALENDAR_FLOW = "https://docs.beamable.com/docs/calendar-flow";
   public const string URL_FEATURE_CURRENCY_HUD = "https://docs.beamable.com/docs/currency-hud";
   public const string URL_FEATURE_LEADERBOARD_FLOW = "https://docs.beamable.com/docs/leaderboard-flow";
   public const string URL_FEATURE_LOGIN_FLOW = "https://docs.beamable.com/docs/login-flow";
   public const string URL_FEATURE_INVENTORY_FLOW = "https://docs.beamable.com/docs/inventory-flow";
   public const string URL_FEATURE_STORE_FLOW = "https://docs.beamable.com/docs/store-flow";
   public const string URL_FEATURE_MICROSERVICES = "https://docs.beamable.com/docs/microservices-feature";
   //
   public const string URL_TOOL_WINDOW_CONTENT_MANAGER = "https://docs.beamable.com/docs/content-manager";
   public const string URL_TOOL_WINDOW_CONTENT_NAMESPACES = "https://docs.beamable.com/docs/content-manager#namespaces";
   public const string URL_TOOL_WINDOW_CONFIG_MANAGER = "https://docs.beamable.com/docs/content-manager";
   public const string URL_TOOL_WINDOW_TOOLBOX = "https://docs.beamable.com/docs/toolbox";
   
   //Menu Items: Shared
   public const string OPEN = "Open";

   //Menu Items: Window
   private const string MENU_ITEM_PATH_WINDOW = "Window";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE = MENU_ITEM_PATH_WINDOW + "/Beamable";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_SAMPLES = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Samples";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Help";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP_DIAGNOSTIC_DATA = MENU_ITEM_PATH_WINDOW_BEAMABLE_HELP + "/Generate Diagnostic Info";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Utilities";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER = MENU_ITEM_PATH_WINDOW_BEAMABLE + "/Beamable Developer";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_MICROSERVICES = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Microservices";

   //Menu Items: Window (#ifdef BEAMABLE_DEVELOPER)
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_BEAMABLE_DEVELOPER_SAMPLES = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Samples";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_UNITY = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Unity";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_THEME_MANAGER = MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Theme Manager";

   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_BUSS = "/New BUSS";
   public const string MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_SDF_GENERATOR =
       MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + MENU_ITEM_PATH_WINDOW_BEAMABLE_BUSS + "/Open SDF Generator";
   
   public const int MENU_ITEM_PATH_WINDOW_PRIORITY_1 = 0;
   public const int MENU_ITEM_PATH_WINDOW_PRIORITY_2 = 20;
   public const int MENU_ITEM_PATH_WINDOW_PRIORITY_3 = 40;
   public const int MENU_ITEM_PATH_WINDOW_PRIORITY_4 = 60;

   //Menu Items: Assets
   public const string MENU_ITEM_PATH_ASSETS_BEAMABLE = "Beamable";
   public const string MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS = MENU_ITEM_PATH_ASSETS_BEAMABLE + "/Configurations";
   public const string MENU_ITEM_PATH_ASSETS_BEAMABLE_SAMPLES = MENU_ITEM_PATH_ASSETS_BEAMABLE + "/Samples";
   public const int MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1 = 0;

   //Editor Pref Keys
   public const string REALM_PREFERENCE = "BeamableSelectedRealms";
}
