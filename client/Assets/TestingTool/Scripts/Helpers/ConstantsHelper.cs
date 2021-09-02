namespace TestingTool.Scripts.Helpers
{
    public static class ConstantsHelper
    {
        public static readonly string TEST_CREATOR_FILENAME = "TestScenariosCreator";
        public static readonly string TEST_SCENARIOS_RUNTIME_FILENAME = "TestScenariosRuntime";
        public static readonly string MAIN_TESTING_SCENE_NAME = "MainMenu";
        public static readonly string NEW_SCENE_NAME = "NewTestScene_RenameMe";
        
        public static readonly string TEST_SCENARIOS_CREATOR_PATH = $"Assets/TestingTool/{TEST_CREATOR_FILENAME}.asset";
        public static readonly string TEST_SCENARIOS_RUNTIME_PATH = $"Assets/TestingTool/Resources/{TEST_SCENARIOS_RUNTIME_FILENAME}.asset";
        public static readonly string TEST_SCENE_TEMPLATE_PATH = "Assets/TestingTool/Scenes/SceneTemplate/TestSceneTemplate.unity";
        public static readonly string TEST_SCENES_PATH = "Assets/TestingTool/Scenes/";
        public static readonly string MAIN_MENU_PATH = $"Assets/TestingTool/{MAIN_TESTING_SCENE_NAME}.unity";

        public static string TEST_SCENE_DATA_PATH(string sceneName) => $"{TEST_SCENES_PATH}{sceneName}.unity";
    }
}