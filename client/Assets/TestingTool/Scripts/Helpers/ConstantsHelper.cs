namespace TestingTool.Scripts.Helpers
{
    public static class ConstantsHelper
    {
        public static readonly string TEST_CREATOR_FILENAME = "TestScenariosCreator";
        public static readonly string TEST_CONFIG_FILENAME = "TestingToolConfig";
        public static readonly string TEST_SCENARIOS_RUNTIME_FILENAME = "TestScenariosRuntime";
        public static readonly string MAIN_TESTING_SCENE_NAME = "MainMenu";
        public static readonly string NEW_SCENE_NAME = "NewTestScene_RenameMe";

        public static readonly string TEST_TOOL_DIRECTORY = "Assets/TestingTool";
        public static readonly string TEST_SCENARIOS_CREATOR_PATH = $"{TEST_TOOL_DIRECTORY}/{TEST_CREATOR_FILENAME}.asset";
        public static readonly string TEST_SCENARIOS_RUNTIME_PATH = $"{TEST_TOOL_DIRECTORY}/Resources/{TEST_SCENARIOS_RUNTIME_FILENAME}.asset";
        public static readonly string TEST_SCENE_TEMPLATE_PATH = $"{TEST_TOOL_DIRECTORY}/Scenes/SceneTemplate/TestSceneTemplate.unity";
        public static readonly string TEST_SCENES_PATH = $"{TEST_TOOL_DIRECTORY}/Scenes/";
        public static readonly string MAIN_MENU_PATH = $"{TEST_TOOL_DIRECTORY}/{MAIN_TESTING_SCENE_NAME}.unity";

        public static string TEST_SCENE_DATA_PATH(string sceneName) => $"{TEST_SCENES_PATH}{sceneName}.unity";
    }
}