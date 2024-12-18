using Beamable.Common;
using Beamable.Editor.Environment;
using Beamable.Editor.UI.Components;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Environment;
namespace Beamable.Editor.UI.Environment
{
	public class EnvironmentWindow : BeamEditorWindow<EnvironmentWindow>
	{
		static EnvironmentWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = Constants.MenuItems.Windows.Names.ENVIRONMENT,
				DockPreferenceTypeName = null,
				FocusOnShow = true,
				RequireLoggedUser = false,
			};
		}

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_ENV,
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_1
		)]
		public static async void Init() => await GetFullyInitializedWindow();
		public static async void Init(BeamEditorWindowInitConfig initParameters) => await GetFullyInitializedWindow(initParameters);



		private VisualElement _windowRoot;
		private EnvironmentData _data;
		private EnvironmentService _service;
		private TextField _apiTextBox;
		private TextField _portalApiTextBox;
		private TextField _mongoExpressTextBox;
		private TextField _dockerRegTextBox;
		private PrimaryButtonVisualElement _applyButton;

		protected override void Build()
		{
			position = new Rect(position.x, position.y, 350, 630);
			minSize = new Vector2(350, 500);
			// Refresh if/when the user logs-in or logs-out while this window is open
			ActiveContext.OnUserChange += _ => BuildWithContext();

			_data = ActiveContext.ServiceScope.GetService<EnvironmentData>().Clone();
			_service = ActiveContext.ServiceScope.GetService<EnvironmentService>();

			var root = this.GetRootVisualContainer();
			root.Clear();
			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BASE_PATH}/EnvironmentWindow.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{BASE_PATH}/EnvironmentWindow.uss");
			_windowRoot.name = nameof(_windowRoot);
			_windowRoot.TryAddScrollViewAsMainElement();

			root.Add(_windowRoot);

			var title = root.Q<Label>("title");
			title.AddTextWrapStyle();

			_apiTextBox = root.Q<TextField>("api");
			_portalApiTextBox = root.Q<TextField>("portalApi");
			_mongoExpressTextBox = root.Q<TextField>("mongoExpress");
			_dockerRegTextBox = root.Q<TextField>("dockerReg");


			string CheckUrl(string url)
			{
				if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				{
					return "invalid url";
				}

				return null;
			}


			var devBtn = root.Q<GenericButtonVisualElement>("dev");
			devBtn.Refresh();
			devBtn.OnClick += OnDevClicked;

			var stagBtn = root.Q<GenericButtonVisualElement>("stage");
			stagBtn.Refresh();
			stagBtn.OnClick += OnStagingClicked;

			var prodBtn = root.Q<GenericButtonVisualElement>("prod");
			prodBtn.Refresh();
			prodBtn.OnClick += OnProdClicked;

			var cancelBtn = root.Q<GenericButtonVisualElement>("cancel");
			cancelBtn.Refresh();
			cancelBtn.OnClick += OnRevertClicked;

			_applyButton = root.Q<PrimaryButtonVisualElement>();
			_applyButton.Refresh();
			_applyButton.AddGateKeeper(
								_apiTextBox.AddErrorLabel("valid api url", CheckUrl),
								_portalApiTextBox.AddErrorLabel("valid portal url", CheckUrl),
								_mongoExpressTextBox.AddErrorLabel("valid mongo express url", CheckUrl),
								_dockerRegTextBox.AddErrorLabel("valid docker registry url", CheckUrl));
			_applyButton.Button.clickable.clicked += OnApplyClicked;

			SetUIFromData();
		}


		private void OnRevertClicked()
		{
			_service.ClearOverrides();
		}

		private void OnApplyClicked()
		{
			var overridesData = new EnvironmentOverridesData(
				_apiTextBox.value,
				_portalApiTextBox.value,
				_mongoExpressTextBox.value,
				_dockerRegTextBox.value
			);
			_service.SetOverrides(overridesData);
		}

		void SetUIFromData()
		{
			_apiTextBox.SetValueWithoutNotify(_data.ApiUrl);
			_portalApiTextBox.SetValueWithoutNotify(_data.PortalUrl);
			_mongoExpressTextBox.SetValueWithoutNotify(_data.BeamMongoExpressUrl);
			_dockerRegTextBox.SetValueWithoutNotify(_data.DockerRegistryUrl);

			_applyButton.CheckGateKeepers();
		}

		private void OnStagingClicked()
		{
			_data = _service.GetStaging(BeamableEnvironment.SdkVersion);
			SetUIFromData();
		}

		private void OnDevClicked()
		{
			_data = _service.GetDev(BeamableEnvironment.SdkVersion);
			SetUIFromData();
		}

		private void OnProdClicked()
		{
			_data = _service.GetProd(BeamableEnvironment.SdkVersion);
			SetUIFromData();
		}
	}
}
