using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor;
using Beamable.Editor.Login.UI;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using UnityEditor;
using Beamable.Server.Editor.UI.Components;
using UnityEngine;
// using ActionBarVisualElement = Beamable.Editor.Microservice.UI.Components.ActionBarVisualElement;
// using MicroserviceBreadcrumbsVisualElement = Beamable.Editor.Microservice.UI.Components.MicroserviceBreadcrumbsVisualElement;
using Debug = UnityEngine.Debug;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.Microservice.UI
{
    public class MicroserviceWindow : CommandRunnerWindow
    {
#if BEAMABLE_NEWMS
        [MenuItem(
            BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
            BeamableConstants.OPEN + " " +
            BeamableConstants.MICROSERVICES_MANAGER,
            priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_2
        )]
#endif
        public static async void Init()
        {
            var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            await LoginWindow.CheckLogin(inspector);
            
            MicroserviceWindow wnd = GetWindow<MicroserviceWindow>("Microservices Manager", true, inspector);
            var checkCommand = new CheckDockerCommand();
            await checkCommand.Start(wnd).Then(installed =>
            {
                if (IsInstantiated)
                {
                    if ( _instance &&
                         EditorWindow.FindObjectOfType(typeof( MicroserviceWindow)) != null)
                    {
                        _instance.Close();
                        _instance = null;
                    }

                    DestroyImmediate(_instance);
                }

                var _ = Instance;
            });
            
        }

        private VisualElement _windowRoot;
        private ActionBarVisualElement _actionBarVisualElement;
        private MicroserviceBreadcrumbsVisualElement _microserviceBreadcrumbsVisualElement;
        private MicroserviceContentVisualElement _microserviceContentVisualElement;
        private LoadingBarElement _loadingBar;

        public MicroservicesDataModel Model;

        private static MicroserviceWindow _instance;

        private static MicroserviceWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
                    _instance = GetWindow<MicroserviceWindow>(BeamableConstants.MICROSERVICES_MANAGER, false, inspector);
                    _instance.Show(true);
                }
                return _instance;
            }
            set
            {
                if (value == null)
                {
                    _instance = null;
                }
                else
                {
                    var oldModel = _instance?.Model;
                    _instance = value;
                    _instance.Model = oldModel;
                }
            }
        }


        public static bool IsInstantiated => _instance != null;

        void CreateModel()
        {
            if (Model == null)
            {
                Model = MicroservicesDataModel.Instance;
            }
            else
            {
                MicroservicesDataModel.Instance = Model;
            }
        }

        void SetForContent()
        {
            var root = this.GetRootVisualContainer();
            root.Clear();

            var uiAsset =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Constants.SERVER_UI}/MicroserviceWindow.uxml");
            _windowRoot = uiAsset.CloneTree();
            _windowRoot.AddStyleSheet($"{Constants.SERVER_UI}/MicroserviceWindow.uss");
            _windowRoot.name = nameof(_windowRoot);

            root.Add(_windowRoot);

            _actionBarVisualElement = root.Q<ActionBarVisualElement>("actionBarVisualElement");
            _actionBarVisualElement.Refresh();

            _microserviceBreadcrumbsVisualElement = root.Q<MicroserviceBreadcrumbsVisualElement>("microserviceBreadcrumbsVisualElement");
            _microserviceBreadcrumbsVisualElement.Refresh();
            _microserviceBreadcrumbsVisualElement.SetSelectAllCheckboxValue(Model.Services.All(model => model.IsSelected));

            if (Model?.Services?.Count == 0)
            {
                _microserviceBreadcrumbsVisualElement.DisableSelectAllCheckbox();
                _actionBarVisualElement.HandleNoMicroservicesScenario();
            }
            
            
            _loadingBar = root.Q<LoadingBarElement>("loadingBar");
            _loadingBar.Hidden = true;
            _loadingBar.Refresh();
            var btn = _loadingBar.Q<Button>("button");
            btn.clickable.clicked -= HideAllLoadingBars;
            btn.clickable.clicked += HideAllLoadingBars;

            _microserviceContentVisualElement = root.Q<MicroserviceContentVisualElement>("microserviceContentVisualElement");
            _microserviceContentVisualElement.Model = Model;
            _microserviceContentVisualElement.Refresh();

            _microserviceBreadcrumbsVisualElement.OnSelectAllCheckboxChanged +=
                _microserviceContentVisualElement.SetAllMicroserviceSelectedStatus;

            _microserviceContentVisualElement.OnAllServiceSelectedStatusChanged +=
                _microserviceBreadcrumbsVisualElement.SetSelectAllCheckboxValue;

            _microserviceBreadcrumbsVisualElement.OnSelectAllCheckboxChanged +=
                _actionBarVisualElement.UpdateTextButtonTexts;
            _microserviceContentVisualElement.OnAllServiceSelectedStatusChanged+=
                _actionBarVisualElement.UpdateTextButtonTexts;

            _actionBarVisualElement.OnInfoButtonClicked += () =>
            {
                Application.OpenURL(BeamableConstants.URL_BEAMABLE_DOCS_WEBSITE);
            };

            _actionBarVisualElement.OnCreateNewClicked += _microserviceContentVisualElement
                .DisplayCreatingNewService;

            _actionBarVisualElement.OnPublishClicked += () => PublishWindow.ShowPublishWindow();

            _actionBarVisualElement.OnRefreshButtonClicked += () =>
            {
                RefreshWindow(true);
            };

            _actionBarVisualElement.OnStartAllClicked += () =>
                _microserviceContentVisualElement.BuildAndStartAllMicroservices(_loadingBar);
            _actionBarVisualElement.OnBuildAllClicked += () =>
                _microserviceContentVisualElement.BuildAllMicroservices(_loadingBar);

            Microservices.onBeforeDeploy -= OnBeforeDeploy;
            Microservices.onBeforeDeploy += OnBeforeDeploy;
        }

        private void HideAllLoadingBars() {
            foreach (var microserviceVisualElement in _windowRoot.Q<MicroserviceContentVisualElement>().MicroserviceVisualElements) {
                microserviceVisualElement.Q<LoadingBarElement>().Hidden = true;
            }
        }

        public void RefreshWindow(bool isHardRefresh)
        {
            if (isHardRefresh)
            {
                MicroserviceWindow.Instance.Refresh();
            }
            else
            {
                RefreshServer();
            }
        }

        private void RefreshServer()
        {
            throw new NotImplementedException();
        }

        private void Refresh()
        {
            _microserviceContentVisualElement.Refresh();
        }

        private void OnEnable()
        {
            CreateModel();
            SetForContent();
        }

        private void OnBeforeDeploy(ManifestModel manifestModel) {
            new DeployLogParser(_loadingBar, manifestModel);
        }
    }

}