using Beamable.NewTestingTool.Core;
using Beamable.NewTestingTool.Core.Models;
using Beamable.NewTestingTool.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using static Beamable.NewTestingTool.Constants.TestConstants.General;
using static Beamable.NewTestingTool.Constants.TestConstants.Paths;

namespace Beamable.NewTestingTool.UI
{
    public class MainMenuTestController : MonoBehaviour
    {
        [SerializeField] private GameObject sceneButton;
        [SerializeField] private Transform scrollViewContent;
        
        private TestConfiguration TestConfiguration
        {
	        get
	        {
		        if (_testConfiguration == null)
			        _testConfiguration = TestExtensions.LoadScriptableObject<TestConfiguration>(CONFIGURATION_FILE_NAME, string.Empty, PATH_TO_RESOURCES);
		        return _testConfiguration;
	        }
        }
        private TestConfiguration _testConfiguration;
        private readonly List<MainMenuTestSceneButton> _testSceneButtons = new List<MainMenuTestSceneButton>();

        private void Awake() => Init();
        private void Init()
        {
	        foreach (var testScene in TestConfiguration.RegisteredTestScenes)
		        _testSceneButtons.Add(CreateTestSceneButton(testScene));
        }

        private MainMenuTestSceneButton CreateTestSceneButton(RegisteredTestScene registeredTestScene)
        {
	        var button = Instantiate(sceneButton, scrollViewContent).GetComponent<MainMenuTestSceneButton>();
            button.Init(registeredTestScene, LoadTestScene);
            return button;
        }
        private void LoadTestScene(RegisteredTestScene registeredTestScene) 
	        => SceneManager.LoadScene(registeredTestScene.SceneName);
    }
}
