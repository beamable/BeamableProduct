// using ActionBarVisualElement = Beamable.Editor.Microservice.UI.Components.ActionBarVisualElement;
// using MicroserviceBreadcrumbsVisualElement = Beamable.Editor.Microservice.UI.Components.MicroserviceBreadcrumbsVisualElement;
using Beamable.Editor.Login.UI;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.Microservice.UI
{
	public class MicroserviceWindow : CommandRunnerWindow, ISerializationCallbackReceiver
	{
		[MenuItem(
			BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			BeamableConstants.OPEN + " " +
			BeamableConstants.MICROSERVICES_MANAGER,
			priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init()
		{
			var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			await LoginWindow.CheckLogin(inspector);

			await new CheckDockerCommand().Start(null).Then(installed =>
			{
				var instanceWnd = Instance;
				instanceWnd.Focus();
			});

		}

		private readonly Vector2 MIN_SIZE = new Vector2(450, 200);

		private VisualElement _windowRoot;
		private ActionBarVisualElement _actionBarVisualElement;
		private MicroserviceBreadcrumbsVisualElement _microserviceBreadcrumbsVisualElement;
		private MicroserviceContentVisualElement _microserviceContentVisualElement;
		private LoadingBarElement _loadingBar;

		[SerializeField]
		public MicroservicesDataModel Model;

		private static MicroserviceWindow _instance;

		public static MicroserviceWindow Instance
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
			private set
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


#if UNITY_2019_3_OR_NEWER
		public static bool IsInstantiated => _instance != null || HasOpenInstances<MicroserviceWindow>();
#else
		public static bool IsInstantiated => _instance != null;
#endif

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

		void SetMinSize()
		{
			minSize = MIN_SIZE;
		}

		void SetForContent()
		{
			var root = this.GetRootVisualContainer();
			root.Clear();

			if (_windowRoot == null)
			{
				var uiAsset =
					AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Constants.SERVER_UI}/MicroserviceWindow.uxml");
				_windowRoot = uiAsset.CloneTree();
				_windowRoot.AddStyleSheet($"{Constants.SERVER_UI}/MicroserviceWindow.uss");
				_windowRoot.name = nameof(_windowRoot);

				root.Add(_windowRoot);
			}

			bool localServicesAvailable = Model?.AllLocalServices != null;
			int localServicesAmount = localServicesAvailable ? Model.AllLocalServices.Count : 0;
			int selectedServicesAmount = localServicesAvailable
				? Model.AllLocalServices.Count(beamService => beamService.IsSelected)
				: 0;

			_actionBarVisualElement = root.Q<ActionBarVisualElement>("actionBarVisualElement");
			_actionBarVisualElement.Refresh();
			_actionBarVisualElement.UpdateButtonsState(selectedServicesAmount, localServicesAmount);

			_microserviceBreadcrumbsVisualElement = root.Q<MicroserviceBreadcrumbsVisualElement>("microserviceBreadcrumbsVisualElement");
			_microserviceBreadcrumbsVisualElement.Refresh();
			_microserviceBreadcrumbsVisualElement.UpdateSelectAllCheckboxValue(selectedServicesAmount, localServicesAmount);

			_loadingBar = root.Q<LoadingBarElement>("loadingBar");
			_loadingBar.Hidden = true;
			_loadingBar.Refresh();
			var btn = _loadingBar.Q<Button>("button");
			btn.clickable.clicked -= HideAllLoadingBars;
			btn.clickable.clicked += HideAllLoadingBars;

			_microserviceContentVisualElement = root.Q<MicroserviceContentVisualElement>("microserviceContentVisualElement");
			_microserviceContentVisualElement.Model = Model;

			_microserviceContentVisualElement.Refresh();

			if (Model != null)
			{
				Model.OnServerManifestUpdated += (manifest) =>
				{
					_microserviceContentVisualElement?.Refresh();
				};
			}

			_microserviceBreadcrumbsVisualElement.OnSelectAllCheckboxChanged +=
				_microserviceContentVisualElement.SetAllMicroserviceSelectedStatus;
			_microserviceBreadcrumbsVisualElement.OnNewServicesDisplayFilterSelected += HandleDisplayFilterSelected;

			_microserviceContentVisualElement.OnServiceSelectionAmountChange +=
				_microserviceBreadcrumbsVisualElement.UpdateSelectAllCheckboxValue;
			_microserviceContentVisualElement.OnServiceSelectionAmountChange += _actionBarVisualElement.UpdateButtonsState;

			_actionBarVisualElement.OnInfoButtonClicked += () =>
			{
				Application.OpenURL(BeamableConstants.URL_FEATURE_MICROSERVICES);
			};

			_actionBarVisualElement.OnCreateNewClicked += _microserviceContentVisualElement
				.DisplayCreatingNewService;

			_actionBarVisualElement.OnPublishClicked += () => PublishWindow.ShowPublishWindow(this);

			_actionBarVisualElement.OnRefreshButtonClicked += () =>
			{
				RefreshWindow(true);
			};

			_actionBarVisualElement.OnStartAllClicked += () =>
				_microserviceContentVisualElement.BuildAndStartAllMicroservices(_loadingBar);
			_actionBarVisualElement.OnBuildAllClicked += () =>
				_microserviceContentVisualElement.BuildAllMicroservices(_loadingBar);

			var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			if (serviceRegistry != null)
			{
				serviceRegistry.OnDeploySuccess -= HandleDeploySuccess;
				serviceRegistry.OnDeploySuccess += HandleDeploySuccess;
				serviceRegistry.OnDeployFailed -= HandleDeployFailed;
				serviceRegistry.OnDeployFailed += HandleDeployFailed;
			}
		}

		private void HandleDisplayFilterSelected(ServicesDisplayFilter filter)
		{
			Model.Filter = filter;
			Refresh();
		}

		private void HideAllLoadingBars()
		{
			foreach (var microserviceVisualElement in _windowRoot.Q<MicroserviceContentVisualElement>().ServiceVisualElements)
			{
				microserviceVisualElement.Q<LoadingBarElement>().Hidden = true;
			}
		}

		public void RefreshWindow(bool isHardRefresh)
		{
			if (isHardRefresh)
			{
				Instance.Refresh();
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
			new CheckDockerCommand().Start(null).Then(_ =>
			{
				_microserviceBreadcrumbsVisualElement?.Refresh();
				_actionBarVisualElement?.Refresh();
				_microserviceContentVisualElement?.Refresh();
			});
		}

		private void OnEnable()
		{
			// if BeamEditor is not initialized, schedule a delay call to try again.
			if (!BeamEditor.IsInitialized || !MicroserviceEditor.IsInitialized)
			{
				EditorApplication.delayCall += () =>
				{
					EditorAPI.Instance.Then(api =>
					{
						SetMinSize();
						CreateModel();
						SetForContent();
					});
				};
				return;
			}

			EditorAPI.Instance.Then(api =>
			{
				SetMinSize();
				CreateModel();
				SetForContent();
			});
		}

		private void HandleDeploySuccess(ManifestModel _model, int _totalSteps)
		{
			RefreshWindow(true);
		}

		private void HandleDeployFailed(ManifestModel _model, string reason)
		{
			Debug.LogError(reason);
			_microserviceContentVisualElement?.Refresh();
		}

		private void OnDestroy()
		{
			if (_instance)
			{
				_instance = null;
			}
		}

		public void OnBeforeSerialize()
		{

		}

		public void OnAfterDeserialize()
		{
			_instance = this;
		}
	}
}
