using Beamable.BSAT.Core;
using Beamable.BSAT.Core.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Beamable.BSAT.Constants.TestConstants.General;

namespace Beamable.BSAT.UI
{
    public class MainMenuTestController : MonoBehaviour
    {
	    [SerializeField] private Button generateReportButton;
        [SerializeField] private GameObject sceneButton;
        [SerializeField] private Transform scrollViewContent;
        
        public TestConfiguration TestConfiguration
		{
			get
			{
				if (_testConfiguration == null)
				{
					_testConfiguration = Resources.Load<TestConfiguration>(CONFIGURATION_FILE_NAME);
					if (TestConfiguration == null)
					{
						TestableDebug.LogError("Cannot load test configuration file!");
						Debug.Break();
					}
				}
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
	        
	        generateReportButton.onClick.AddListener(() => TestConfiguration.GenerateReport());
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
