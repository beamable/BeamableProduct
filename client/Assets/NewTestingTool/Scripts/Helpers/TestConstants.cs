namespace Beamable.NewTestingTool.Constants
{
	public static class TestConstants
	{
		public static class General
		{
			public const string DEBUG_PREFIX = "[TESTER]";
			public const string SCENE_TEMPLATE_NAME = "TestTemplate";
			public const string SCRIPT_TEMPLATE_NAME = "TestableTemplate";
			public const string CONFIGURATION_FILE_NAME = "TestConfiguration";
		}
		public static class Paths
		{
			public const string PATH_TO_TESTING_TOOL = "Assets/NewTestingTool";
			public static readonly string PATH_TO_UI_COMPONENTS = $"{PATH_TO_TESTING_TOOL}/Scripts/Editor/UI/Components";
			public static readonly string PATH_TO_TEST_SCENES = $"{PATH_TO_TESTING_TOOL}/Tests";
			public static readonly string PATH_TO_RESOURCES = $"{PATH_TO_TESTING_TOOL}/Resources";
			public static readonly string PATH_TO_TEMPLATES = $"{PATH_TO_TESTING_TOOL}/Templates";
			public static readonly string PATH_TO_SCENE_TEMPLATE = $"{PATH_TO_TEMPLATES}/{General.SCENE_TEMPLATE_NAME}";
			public static readonly string PATH_TO_SCRIPT_TEMPLATE = $"{PATH_TO_TEMPLATES}/{General.SCRIPT_TEMPLATE_NAME}";
		
			public static string GetPathToTestResources(string sceneName) => $"{PATH_TO_RESOURCES}/Tests/{sceneName}";
			public static string GetPathToTest(string sceneName) => $"{PATH_TO_TEST_SCENES}/{sceneName}";
			public static string GetPathToTestScene(string sceneName) => $"{GetPathToTest(sceneName)}/{sceneName}.unity";
			public static string GetPathToTestScripts(string sceneName) => $"{GetPathToTest(sceneName)}/Scripts";
		}
		public static class SessionState
		{
			public const string SHOULD_CREATE_NEW_TEST = "testing_tool_should_create_new_test";
			public const string TEST_NAME = "testing_tool_test_name";
			public const string SCRIPT_NAME = "testing_tool_script_name";
		}
	}
}
