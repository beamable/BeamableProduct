using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.Environment;
using Beamable.Editor.UI.Components;
using Beamable.Editor.Login.UI;
using UnityEditor;
using Beamable.Editor.NoUser;
using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Buss.Components;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using Debug = UnityEngine.Debug;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Toolbox.UI
{
    public class ToolboxWindow : EditorWindow
    {
        [MenuItem(
            BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
            BeamableConstants.OPEN + " " +
            BeamableConstants.TOOLBOX,
            priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_1
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
            var contentManagerWindow = GetWindow<ToolboxWindow>(BeamableConstants.TOOLBOX, true, typeof(SceneView));

            contentManagerWindow.Show(true);
        }

        public static ToolboxWindow Instance { get; private set; }

        public static bool IsInstantiated
        {
            get { return Instance != null; }
        }

        private ToolboxComponent _toolboxComponent;
        private VisualElement _windowRoot;

        private ToolboxActionBarVisualElement _actionBarVisualElement;
        private ToolboxBreadcrumbsVisualElement _breadcrumbsVisualElement;

        private ToolboxContentListVisualElement _contentListVisualElement;

        // private ToolboxSelectionListVisualElement _selectionListVisualElement;
        private SearchBarVisualElement _searchBarVisualElement;

        private ToolboxModel _model;
        private ToolboxAnnouncementListVisualElement _announcementListVisualElement;

        private void OnEnable()
        {
            Instance = this;
            minSize = new Vector2(560, 300);

            // Refresh if/when the user logs-in or logs-out while this window is open
            EditorAPI.Instance.Then(de => { de.OnUserChange += _ => Refresh(); });

            BeamablePackages.BeamablePackageUpdateMeta.OnPackageUpdated += ShowAnnouncementAfterPackageUpdate;
            BeamablePackages.IsPackageUpdated().Then(isUpdated =>
            {
                if (isUpdated && BeamablePackages.BeamablePackageUpdateMeta.IsBlogSiteAvailable &&
                    !BeamablePackages.BeamablePackageUpdateMeta.IsBlogVisited &&
                    !EditorPrefs.GetBool(BeamableEditorPrefsConstants.IS_PACKAGE_WHATSNEW_ANNOUNCEMENT_IGNORED, true))
                {
                    ShowAnnouncementAfterPackageUpdate();
                }
            });
            
            // Force refresh to build the initial window
            Refresh();
            CheckForUpdate();
        }

        private void OnDisable()
        {
            BeamablePackages.BeamablePackageUpdateMeta.OnPackageUpdated -= ShowAnnouncementAfterPackageUpdate;
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
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{ToolboxConstants.BASE_PATH}/ToolboxWindow.uxml");
            _windowRoot = uiAsset.CloneTree();
            _windowRoot.AddStyleSheet($"{ToolboxConstants.BASE_PATH}/ToolboxWindow.uss");
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
                Debug.Log("Show info");
                Application.OpenURL(BeamableConstants.URL_TOOL_WINDOW_TOOLBOX);
            };

            CheckForDeps();
        }

        private void AnnouncementList_OnHeightChanged(float height)
        {
            // TODO: animate the height...
            _contentListVisualElement?.style.SetTop(65 + height);
        }

        private void CheckForDeps()
        {
            EditorAPI.Instance.Then(b =>
            {
                if (b.HasDependencies()) return;

                var descElement = new VisualElement();
                descElement.Add(new Label("Welcome to Beamable! This package includes official Unity assets")
                    .AddTextWrapStyle());
                var tmpProButton = new Button(() =>
                    Application.OpenURL("https://docs.unity3d.com/Manual/com.unity.textmeshpro.html"))
                {
                    text = "TextMeshPro"
                };
                tmpProButton.AddToClassList("noBackground");
                tmpProButton.AddToClassList("announcementButton");

                descElement.Add(tmpProButton);

                descElement.Add(new Label("and").AddTextWrapStyle());
                var addressablesButton = new Button(() =>
                    Application.OpenURL("https://docs.unity3d.com/Manual/com.unity.addressables.html"))
                {
                    text = "Addressables"
                };
                addressablesButton.AddToClassList("noBackground");
                addressablesButton.AddToClassList("announcementButton");

                descElement.Add(addressablesButton);
                descElement.Add(
                    new Label(
                            "in order to provide UI prefabs you can easily drag & drop into your game. To complete the installation, we must add them to your project now.")
                        .AddTextWrapStyle());

                var depAnnouncement = new AnnouncementModel
                {
                    CustomIcon =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(
                            "Packages/com.beamable/Editor/UI/Common/Icons/welcome.png"),
                    TitleElement = new Label("Beamable + TextMeshPro + Addressables = ♥"),
                    Status = ToolboxAnnouncementStatus.INFO,
                    ActionText = "Import Assets",
                    DescriptionElement = descElement
                };
                depAnnouncement.Action = () =>
                {
                    b.CreateDependencies().Then(_ => { _model.RemoveAnnouncement(depAnnouncement); });
                };
                _model.AddAnnouncement(depAnnouncement);
            });
        }

        private void CheckForUpdate()
        {
            BeamablePackages.IsPackageUpdated().Then(isUpdated =>
            {
                if (isUpdated || BeamablePackages.BeamablePackageUpdateMeta.IsInstallationIgnored)
                {
                    return;
                }

                if (EditorPrefs.GetBool(BeamableEditorPrefsConstants.IS_PACKAGE_UPDATE_IGNORED, false))
                {
                    BeamablePackages.BeamablePackageUpdateMeta.IsInstallationIgnored = true;
                    return;
                }

                var updateAvailableAnnouncement = new UpdateAvailableAnnouncementModel
                {
                    TitleElement = new Label("New package update is available!").AddTextWrapStyle(),
                    DescriptionElement = new VisualElement(),
                    Status = ToolboxAnnouncementStatus.INFO,
                };

                if (BeamablePackages.BeamablePackageUpdateMeta.IsBlogSiteAvailable)
                {
                    updateAvailableAnnouncement.OnWhatsNew = () =>
                    {
                        Application.OpenURL(BeamableWebRequester.BlogSpotUrl);
                        BeamablePackages.BeamablePackageUpdateMeta.IsBlogVisited = true;
                    };
                }

                updateAvailableAnnouncement.OnIgnore = () =>
                {
                    BeamablePackages.BeamablePackageUpdateMeta.IsInstallationIgnored = true;
                    EditorPrefs.SetBool(BeamableEditorPrefsConstants.IS_PACKAGE_UPDATE_IGNORED, true);
                    _model.RemoveAnnouncement(updateAvailableAnnouncement);
                };

                updateAvailableAnnouncement.OnInstall = () => BeamablePackages.UpdatePackage().Then(_ =>
                {
                    _model.RemoveAnnouncement(updateAvailableAnnouncement);
                    if (!BeamablePackages.BeamablePackageUpdateMeta.IsBlogVisited &&
                        BeamablePackages.BeamablePackageUpdateMeta.IsBlogSiteAvailable)
                    {
                        ShowAnnouncementAfterPackageUpdate();
                    }
                });

                _model.AddAnnouncement(updateAvailableAnnouncement);
            });
        }

        private void ShowAnnouncementAfterPackageUpdate()
        {
            if (_model.Announcements.Any(x => x is UpdateAvailableAnnouncementModel))
            {
                return;
            }
            
            var whatsNewAnnouncement = new WhatsNewAnnouncementModel
            {
                TitleElement = new Label("View the related blog post!").AddTextWrapStyle(),
                DescriptionElement = new VisualElement(),
                Status = ToolboxAnnouncementStatus.INFO,
            };

            whatsNewAnnouncement.OnWhatsNew = () =>
            {
                Application.OpenURL(BeamableWebRequester.BlogSpotUrl);
                BeamablePackages.BeamablePackageUpdateMeta.IsBlogVisited = true;
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