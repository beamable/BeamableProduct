using Beamable.Editor.Environment;
using Beamable.Editor.Login.UI;
using Beamable.Editor.NoUser;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.BeamableConstants.Features.Toolbox;

namespace Beamable.Editor.Toolbox.UI
{
	public class ToolboxWindow : EditorWindow
	{
		[MenuItem(
			BeamableConstantsOLD.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			BeamableConstantsOLD.OPEN + " " +
			BeamableConstantsOLD.TOOLBOX,
			priority = BeamableConstantsOLD.MENU_ITEM_PATH_WINDOW_PRIORITY_1
		)]
		public static async void Init()
		{
			await LoginWindow.CheckLogin(typeof(SceneView));

			// Ensure at most one Beamable ContentManagerWindow exists
			// If exists, rebuild it from scratch (easy refresh mechanism)
			if (ToolboxWindow.IsInstantiated)
			{
				if (ToolboxWindow.Instance != null && Instance &&
					EditorWindow.FindObjectOfType(typeof(ToolboxWindow)) != null)
				{
					ToolboxWindow.Instance.Close();
				}

				DestroyImmediate(ToolboxWindow.Instance);
			}

			// Create Beamable ContentManagerWindow and dock it next to Unity Hierarchy Window
			var contentManagerWindow = GetWindow<ToolboxWindow>(BeamableConstantsOLD.TOOLBOX, true, typeof(SceneView));

			contentManagerWindow.Show(true);
		}

		public static ToolboxWindow Instance { get; private set; }

		public static bool IsInstantiated
		{
			get { return Instance != null; }
		}

		private VisualElement _windowRoot;

		private ToolboxActionBarVisualElement _actionBarVisualElement;
		private ToolboxBreadcrumbsVisualElement _breadcrumbsVisualElement;

		private ToolboxContentListVisualElement _contentListVisualElement;

		private ToolboxModel _model;
		private ToolboxAnnouncementListVisualElement _announcementListVisualElement;

		private void OnEnable()
		{
			Instance = this;
			minSize = new Vector2(560, 300);

			// Refresh if/when the user logs-in or logs-out while this window is open
			EditorAPI.Instance.Then(de => { de.OnUserChange += _ => Refresh(); });
			// Force refresh to build the initial window
			Refresh();

			CheckAnnouncements();
			CheckForDeps();
			CheckForUpdate();
		}
		private void OnDisable()
		{
			BeamablePackageUpdateMeta.OnPackageUpdated -= ShowWhatsNewAnnouncement;
		}
		private void CheckAnnouncements()
		{
			BeamablePackageUpdateMeta.OnPackageUpdated += ShowWhatsNewAnnouncement;
			BeamablePackages.IsPackageUpdated().Then(isUpdated =>
			{
				if (isUpdated && BeamablePackageUpdateMeta.IsBlogSiteAvailable &&
					!BeamablePackageUpdateMeta.IsBlogVisited &&
					!EditorPrefs.GetBool(BeamableEditorPrefsConstants.IS_PACKAGE_WHATSNEW_ANNOUNCEMENT_IGNORED, true))
				{
					ShowWhatsNewAnnouncement();
				}
			});
		}
		private async void Refresh()
		{
			if (Instance != null)
			{
				Instance._model?.Destroy();
			}

			Instance._model = new ToolboxModel();
			Instance._model.UseDefaultWidgetSource();
			Instance._model.Initialize();
			var de = await EditorAPI.Instance;
			var isLoggedIn = de.User != null;
			if (isLoggedIn)
			{
				SetForContent();
			}
			else
			{
				SetForLogin();
			}
		}
		private void SetForLogin()
		{
			var root = this.GetRootVisualContainer();
			root.Clear();
			var noUserVisualElement = new NoUserVisualElement();
			root.Add(noUserVisualElement);
		}
		private void SetForContent()
		{
			var root = this.GetRootVisualContainer();
			root.Clear();
			var uiAsset =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BASE_PATH}/ToolboxWindow.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{BASE_PATH}/ToolboxWindow.uss");
			_windowRoot.name = nameof(_windowRoot);

			root.Add(_windowRoot);

			_actionBarVisualElement = root.Q<ToolboxActionBarVisualElement>("actionBarVisualElement");
			_actionBarVisualElement.Model = _model;
			_actionBarVisualElement.Refresh();

			_breadcrumbsVisualElement = root.Q<ToolboxBreadcrumbsVisualElement>("breadcrumbsVisualElement");
			_breadcrumbsVisualElement.Refresh();

			_contentListVisualElement = root.Q<ToolboxContentListVisualElement>("contentListVisualElement");
			_contentListVisualElement.Model = _model;
			_contentListVisualElement.Refresh();

			_announcementListVisualElement = root.Q<ToolboxAnnouncementListVisualElement>();
			_announcementListVisualElement.Model = _model;
			_announcementListVisualElement.Refresh();
			_announcementListVisualElement.OnHeightChanged += AnnouncementList_OnHeightChanged;

			_actionBarVisualElement.OnInfoButtonClicked += () =>
			{
				Application.OpenURL(BeamableConstantsOLD.URL_TOOL_WINDOW_TOOLBOX);
			};

			CheckForDeps();
		}
		private void AnnouncementList_OnHeightChanged(float height)
		{
			// TODO: animate the height...
			_contentListVisualElement?.style.SetTop(65 + height);
			_contentListVisualElement?.MarkDirtyRepaint();
		}
		private void CheckForDeps()
		{
			if (_model.IsSpecificAnnouncementCurrentlyDisplaying(typeof(WelcomeAnnouncementModel)))
			{
				return;
			}

			EditorAPI.Instance.Then(api =>
			{
				if (api.HasDependencies() ||
					_model.IsSpecificAnnouncementCurrentlyDisplaying(typeof(WelcomeAnnouncementModel)))
				{
					return;
				}

				var descriptionElement = new VisualElement();
				descriptionElement.AddToClassList("announcement-descriptionSection");

				var label = new Label("Welcome to Beamable! This package includes official Unity assets");
				label.AddToClassList("noMarginNoPaddingNoBorder");
				label.AddToClassList("announcement-text");
				label.AddTextWrapStyle();
				descriptionElement.Add(label);

				var button = new Button(() => Application.OpenURL("https://docs.unity3d.com/Manual/com.unity.textmeshpro.html"));
				button.text = "TextMeshPro";
				button.AddToClassList("noMarginNoPaddingNoBorder");
				button.AddToClassList("announcement-hiddenButton");
				descriptionElement.Add(button);

				label = new Label("and");
				label.AddToClassList("noMarginNoPaddingNoBorder");
				label.AddToClassList("announcement-text");
				label.AddTextWrapStyle();
				descriptionElement.Add(label);

				button = new Button(() => Application.OpenURL("https://docs.unity3d.com/Manual/com.unity.addressables.html"));
				button.text = "Addressables";
				button.AddToClassList("noMarginNoPaddingNoBorder");
				button.AddToClassList("announcement-hiddenButton");
				descriptionElement.Add(button);

				label = new Label("in order to provide UI prefabs you can easily drag & drop into your game. To complete the installation, we must add them to your project now.");
				label.AddToClassList("noMarginNoPaddingNoBorder");
				label.AddToClassList("announcement-text");
				label.AddTextWrapStyle();
				descriptionElement.Add(label);

				var welcomeAnnouncement = new WelcomeAnnouncementModel();
				welcomeAnnouncement.DescriptionElement = descriptionElement;

				welcomeAnnouncement.OnImport = () =>
				{
					api.CreateDependencies().Then(_ => { _model.RemoveAnnouncement(welcomeAnnouncement); });
				};
				_model.AddAnnouncement(welcomeAnnouncement);
			});
		}
		private void CheckForUpdate()
		{
			BeamablePackages.IsPackageUpdated().Then(isUpdated =>
			{
				if (isUpdated || BeamablePackageUpdateMeta.IsInstallationIgnored)
				{
					return;
				}
				if (EditorPrefs.GetBool(BeamableEditorPrefsConstants.IS_PACKAGE_UPDATE_IGNORED))
				{
					BeamablePackageUpdateMeta.IsInstallationIgnored = true;
					return;
				}
				ShowUpdateAvailableAnnouncement();
			});
		}
		private void ShowUpdateAvailableAnnouncement()
		{
			if (_model.IsSpecificAnnouncementCurrentlyDisplaying(typeof(UpdateAvailableAnnouncementModel)))
			{
				return;
			}

			BeamablePackageUpdateMeta.IsBlogSiteAvailable = BeamableWebRequester.IsBlogSpotAvailable(BeamablePackageUpdateMeta.NewestVersionNumber);
			var updateAvailableAnnouncement = new UpdateAvailableAnnouncementModel();
			updateAvailableAnnouncement.SetDescription(BeamablePackageUpdateMeta.NewestVersionNumber, BeamablePackageUpdateMeta.IsBlogSiteAvailable);

			if (BeamablePackageUpdateMeta.IsBlogSiteAvailable)
			{
				updateAvailableAnnouncement.OnWhatsNew = () =>
				{
					Application.OpenURL(BeamableWebRequester.BlogSpotUrl);
					BeamablePackageUpdateMeta.IsBlogVisited = true;
				};
			}
			else

				updateAvailableAnnouncement.OnIgnore = () =>
				{
					BeamablePackageUpdateMeta.IsInstallationIgnored = true;
					EditorPrefs.SetBool(BeamableEditorPrefsConstants.IS_PACKAGE_UPDATE_IGNORED, true);
					_model.RemoveAnnouncement(updateAvailableAnnouncement);
				};

			updateAvailableAnnouncement.OnInstall = () => BeamablePackages.UpdatePackage().Then(_ =>
			{
				_model.RemoveAnnouncement(updateAvailableAnnouncement);
				if (!BeamablePackageUpdateMeta.IsBlogVisited &&
					BeamablePackageUpdateMeta.IsBlogSiteAvailable)
				{
					ShowWhatsNewAnnouncement();
				}
			});

			_model.AddAnnouncement(updateAvailableAnnouncement);
		}
		private void ShowWhatsNewAnnouncement()
		{
			if (_model.IsSpecificAnnouncementCurrentlyDisplaying(typeof(WhatsNewAnnouncementModel)))
			{
				return;
			}

			var whatsNewAnnouncement = new WhatsNewAnnouncementModel();

			whatsNewAnnouncement.OnWhatsNew = () =>
			{
				Application.OpenURL(BeamableWebRequester.BlogSpotUrl);
				BeamablePackageUpdateMeta.IsBlogVisited = true;
				_model.RemoveAnnouncement(whatsNewAnnouncement);
			};
			whatsNewAnnouncement.OnIgnore = () =>
			{
				EditorPrefs.SetBool(BeamableEditorPrefsConstants.IS_PACKAGE_WHATSNEW_ANNOUNCEMENT_IGNORED, true);
				_model.RemoveAnnouncement(whatsNewAnnouncement);
			};
			_model.AddAnnouncement(whatsNewAnnouncement);
		}
	}
}
