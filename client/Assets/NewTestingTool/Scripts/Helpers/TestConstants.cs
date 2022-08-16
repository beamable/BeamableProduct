namespace NewTestingTool.Constants
{
	public static class TestConstants
	{
		public const string DEBUG_PREFIX = "[TESTER]";
		public const string CONFIGURATION_FILE_NAME = "TestConfiguration";
		public const string PATH_TO_TEST_SCENES = "Assets/NewTestingTool/Tests";
		public const string PATH_TO_RESOURCES = "Assets/NewTestingTool/Resources";
		public static string PATH_TO_RESOURCES_TESTS(string sceneName) => $"{PATH_TO_RESOURCES}/Tests/{sceneName}";
		public static string GetPathToTestScene(string sceneName) => $"{PATH_TO_TEST_SCENES}/{sceneName}/{sceneName}.unity";
	}
}
