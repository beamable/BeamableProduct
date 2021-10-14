using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Beamable.Common.Api.Auth;
using Beamable.Editor.Content.Components;
using Beamable.Editor.Content.Models;
using Beamable.Editor;
using Beamable.Editor.Login.UI;
using UnityEditor;
using Beamable.Editor.NoUser;
using Beamable.Editor.Realms;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Platform.SDK;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Content
{
   public class ContentManagerWindow : EditorWindow, ISerializationCallbackReceiver
    {
      [MenuItem(
      BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
      BeamableConstants.OPEN + " " +
      BeamableConstants.CONTENT_MANAGER,
      priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_2
      )]
      public static async Task Init()
      {
         await LoginWindow.CheckLogin(typeof(ContentManagerWindow), typeof(SceneView));

         // Create Beamable ContentManagerWindow and dock it next to Unity Hierarchy Window
         ContentManagerWindow.Instance.Show();
      }
      
      private static ContentManagerWindow _instance;

      public static ContentManagerWindow Instance
      {
        get
        {
            if (_instance == null)
            {
                _instance = GetWindow<ContentManagerWindow>(BeamableConstants.CONTENT_MANAGER, true, typeof(ContentManagerWindow), typeof(SceneView));
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
                var oldModel = _instance?._contentManager;
                _instance = value;
                _instance._contentManager = oldModel;
            }
        }
      }
      
      private ContentManager _contentManager;
      private VisualElement _windowRoot;
      private VisualElement _explorerContainer, _statusBarContainer;

      private ActionBarVisualElement _actionBarVisualElement;
      private ExplorerVisualElement _explorerElement;
      private StatusBarVisualElement _statusBarElement;
      private BeamablePopupWindow _currentWindow;

      private List<string> _cachedItemsToDownload;
      private bool _cachedCreateNewManifestFlag;

      private void OnEnable()
      {
         // Refresh if/when the user logs-in or logs-out while this window is open
         EditorAPI.Instance.Then(de =>
         {
            de.OnUserChange += HandleUserChange;
            de.OnRealmChange += HandleRealmChange;
            ContentIO.OnManifestChanged += OnManifestChanged;
         });
         minSize = new Vector2(560, 300);

         // Force refresh to build the initial window
         Refresh();
      }

      private void OnDisable()
      {
         EditorAPI.Instance.Then(de =>
         {
            de.OnUserChange -= HandleUserChange;
            de.OnRealmChange -= HandleRealmChange;
            ContentIO.OnManifestChanged -= OnManifestChanged;
         });
      }

      private void HandleRealmChange(RealmView realm)
      {
         Refresh();
      }

      private void HandleUserChange(User user)
      {
         Refresh();
      }

      private void OnManifestChanged(string manifestId)
      {
         SoftReset();
      }

      public void Refresh()
      {
         EditorAPI.Instance.Then(beamable =>
         {
            var isLoggedIn = beamable.HasToken;
            if (!isLoggedIn)
            {
               Debug.LogWarning("You are accessing the Beamable Content Manager, but you are not logged in. You may see out of sync data.");
            }
         });
         SetForContent();
      }

      public void SetCurrentWindow(BeamablePopupWindow window)
      {
         _currentWindow = window;
      }

      public void CloseCurrentWindow()
      {
         if (_currentWindow != null)
         {
            _currentWindow.Close();
            _currentWindow = null;
         }
      }

      void SetForLogin()
      {
         var root = this.GetRootVisualContainer();
         root.Clear();
         var noUserVisualElement = new NoUserVisualElement();
         root.Add(noUserVisualElement);
      }
      
      public void SoftReset()
      {
         _contentManager.Model.TriggerSoftReset();
      }

      void SetForContent()
      {
         ContentManager oldManager = null;
         if (Instance != null)
         {
            oldManager = Instance._contentManager;
            oldManager?.Destroy();
         }

         _contentManager?.Destroy();
         _contentManager = new ContentManager();
         _contentManager.Initialize();
         Instance = this;

         var root = this.GetRootVisualContainer();

         root.Clear();
         var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{ContentManagerConstants.BASE_PATH}/ContentManagerWindow.uxml");
         _windowRoot = uiAsset.CloneTree();
         _windowRoot.AddStyleSheet($"{ContentManagerConstants.BASE_PATH}/ContentManagerWindow.uss");
         _windowRoot.name = nameof(_windowRoot);

         root.Add(_windowRoot);

         _actionBarVisualElement = root.Q<ActionBarVisualElement>("actionBarVisualElement");
         _actionBarVisualElement.Model = _contentManager.Model;
         _actionBarVisualElement.Refresh();

         // Handlers for Buttons (Left To Right in UX)
         _actionBarVisualElement.OnAddItemButtonClicked += (typeDescriptor) =>
         {
            _contentManager.AddItem(typeDescriptor);
         };

         _actionBarVisualElement.OnValidateButtonClicked += () =>
         {

            if (_currentWindow != null)
            {
               _currentWindow.Close();
            }
            
            _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.ValidateContent, GetValidateContentVisualElement(), this, 
            ContentManagerConstants.WindowSizeMinimum, () =>
            {
                // trigger after Unity domain reload
                Instance._currentWindow?.SwapContent(Instance.GetValidateContentVisualElement());
            });

            _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
         };

         _actionBarVisualElement.OnPublishButtonClicked += (createNew) =>
         {
             if (_currentWindow != null)
             {
                 _currentWindow.Close();
             }

             // validate and create publish set.

             _cachedCreateNewManifestFlag = createNew;

             _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.ValidateContent, GetValidateContentVisualElementWithPublish(), this,
             ContentManagerConstants.WindowSizeMinimum, () =>
             {
                 // trigger after Unity domain reload
                 Instance._currentWindow?.SwapContent(Instance.GetValidateContentVisualElementWithPublish());
             });

             _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;

             if (_cachedCreateNewManifestFlag)
             {
                 _currentWindow.minSize = new Vector2(_currentWindow.minSize.x, _currentWindow.minSize.y + 100);
             }
         };

         _actionBarVisualElement.OnDownloadButtonClicked += () =>
         {
             if (_currentWindow != null)
             {
                 _currentWindow.Close();
             }

             _cachedItemsToDownload = null;
             _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.DownloadContent, GetDownloadContentVisualElement(), this,
             ContentManagerConstants.WindowSizeMinimum, () =>
             {
                 // trigger after Unity domain reload
                 Instance._currentWindow?.SwapContent(Instance.GetDownloadContentVisualElement());
             });
             _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
         };

         _actionBarVisualElement.OnRefreshButtonClicked += () =>
         {
            _contentManager.RefreshWindow(true);
         };

         _actionBarVisualElement.OnDocsButtonClicked += () =>
         {
            _contentManager.ShowDocs();
         };

         _explorerContainer = root.Q<VisualElement>("explorer-container");
         _statusBarContainer = root.Q<VisualElement>("status-bar-container");

         _explorerElement = new ExplorerVisualElement();
         _explorerContainer.Add(_explorerElement);
         _explorerElement.OnAddItemButtonClicked += ExplorerElement_OnAddItemButtonClicked;
         _explorerElement.OnAddItemRequested += ExplorerElement_OnAddItem;
         _explorerElement.OnItemDownloadRequested += ExplorerElement_OnDownloadItem;
         _explorerElement.OnRenameItemRequested += ExplorerElement_OnItemRename;

         _explorerElement.Model = _contentManager.Model;
         _explorerElement.Refresh();

         _statusBarElement = new StatusBarVisualElement();
         _statusBarElement.Model = _contentManager.Model;
         _statusBarContainer.Add(_statusBarElement);
         _statusBarElement.Refresh();
      }

      private void ExplorerElement_OnAddItemButtonClicked()
      {
         var newContent = _contentManager.AddItem();
         EditorApplication.delayCall += () =>
         {
            if (_contentManager.Model.GetDescriptorForId(newContent.Id, out var item))
            {
               item.ForceRename();
            }
         };
      }

      private void ExplorerElement_OnItemRename(ContentItemDescriptor contentItemDescriptor)
      {
            EditorApplication.delayCall += () =>
            {
                if (_contentManager.Model.GetDescriptorForId(contentItemDescriptor.Id, out var item))
                {
                    item.ForceRename();
                }
            };
      }

      private void ExplorerElement_OnAddItem(ContentTypeDescriptor type)
      {
         var newContent = _contentManager.AddItem(type);
         EditorApplication.delayCall += () =>
         {
            if (_contentManager.Model.GetDescriptorForId(newContent.Id, out var item))
            {
               item.ForceRename();
            }
         };
      }

      private void ExplorerElement_OnDownloadItem(List<ContentItemDescriptor> items)
      {
         if (_currentWindow != null)
         {
            _currentWindow.Close();
         }

         _cachedItemsToDownload = items.Select(x => x.Id).ToList();
         _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.DownloadContent, GetDownloadContentVisualElement(), this,
         ContentManagerConstants.WindowSizeMinimum, () =>
         {
             // trigger after Unity domain reload
             Instance._currentWindow?.SwapContent(Instance.GetDownloadContentVisualElement());
             Instance._currentWindow?.FitToContent();
         }).FitToContent();

         _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
      }

      private void Update() {
         _actionBarVisualElement.RefreshPublishDropdownVisibility();
      }

      DownloadContentVisualElement GetDownloadContentVisualElement()
      {
            var downloadPopup = new DownloadContentVisualElement();

            if (_cachedItemsToDownload != null && _cachedItemsToDownload.Count > 0)
            {
                downloadPopup.Model = _contentManager.PrepareDownloadSummary(_cachedItemsToDownload.ToArray());
            }
            else
            {
                downloadPopup.Model = _contentManager.PrepareDownloadSummary();
            }

            downloadPopup.OnRefreshContentManager += () => _contentManager.RefreshWindow(true);
            downloadPopup.OnClosed += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            downloadPopup.OnCancelled += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            downloadPopup.OnDownloadStarted += (summary, prog, finished) =>
            {
                Instance._contentManager?.DownloadContent(summary, prog, finished).Then(_ => SoftReset());
            };

            return downloadPopup;
      }

      ResetContentVisualElement GetResetContentVisualElement()
      {
            var clearPopup = new ResetContentVisualElement();
            clearPopup.Model = Instance._contentManager.PrepareDownloadSummary();
            clearPopup.DataModel = Instance._contentManager.Model;

            clearPopup.OnRefreshContentManager += () => Instance._contentManager.RefreshWindow(true);
            clearPopup.OnClosed += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            clearPopup.OnCancelled += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            clearPopup.OnDownloadStarted += (summary, prog, finished) =>
            {
                Instance._contentManager?.DownloadContent(summary, prog, finished).Then(_ =>
                {
                    Instance._contentManager?.Model.TriggerSoftReset();
                });
            };

            return clearPopup;
      }

      ValidateContentVisualElement GetValidateContentVisualElement()
      {
            var validatePopup = new ValidateContentVisualElement();
            validatePopup.DataModel = _contentManager.Model;

            validatePopup.OnCancelled += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            validatePopup.OnClosed += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            EditorApplication.delayCall += () =>
            {
                Instance._contentManager?.ValidateContent(validatePopup.SetProgress, validatePopup.HandleValidationErrors)
               .Then(_ => validatePopup.HandleFinished());
            };

            return validatePopup;
      }

      ValidateContentVisualElement GetValidateContentVisualElementWithPublish()
      {
            var validatePopup = new ValidateContentVisualElement();
            validatePopup.DataModel = _contentManager.Model;

            validatePopup.OnCancelled += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            validatePopup.OnClosed += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            EditorApplication.delayCall += () =>
            {
                _contentManager.ValidateContent(validatePopup.SetProgress, validatePopup.HandleValidationErrors)
                    .Then(errors =>
                    {
                        validatePopup.HandleFinished();

                        if (errors.Count != 0) return;

                        _currentWindow.SwapContent(GetPublishContentVisualElement(), () =>
                        {
                            // trigger after domain reload
                            Instance._currentWindow?.SwapContent(Instance.GetPublishContentVisualElement());
                        });

                        _currentWindow.titleContent = new GUIContent(ContentManagerConstants.PublishContent);
                    });
            };

            return validatePopup;
      }

      PublishContentVisualElement GetPublishContentVisualElement()
      {
            var publishPopup = new PublishContentVisualElement();
            publishPopup.CreateNewManifest = _cachedCreateNewManifestFlag;
            publishPopup.DataModel = _contentManager.Model;
            publishPopup.PublishSet = _contentManager.CreatePublishSet(_cachedCreateNewManifestFlag);

            publishPopup.OnCancelled += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            publishPopup.OnCompleted += () =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            };

            bool createNewManifest = _cachedCreateNewManifestFlag;

            publishPopup.OnPublishRequested += (set, prog, finished) =>
            {
                if (createNewManifest)
                {
                    EditorAPI.Instance.Then(api =>
                    {
                        api.ContentIO.SwitchManifest(publishPopup.ManifestName).Then(_ =>
                        {
                            set.ManifestId = publishPopup.ManifestName;
                            Instance._contentManager?.PublishContent(set, prog, finished).Then(__ => SoftReset());
                        });
                    });
                }
                else
                {
                    Instance._contentManager?.PublishContent(set, prog, finished).Then(_ => SoftReset());
                }
            };

            return publishPopup;
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

      [MenuItem(BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Reset Content")]
      private static async Task ResetContent() {

         if (Instance == null)
         {
            await Init();
         }

         Instance._currentWindow?.Close();
         Instance._currentWindow = null;

         Instance._currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.RemoveLocalContent, Instance.GetResetContentVisualElement(), null ,
         ContentManagerConstants.WindowSizeMinimum, () =>
         {
             // trigger after Unity domain reload
             Instance._currentWindow?.SwapContent(Instance.GetResetContentVisualElement());
             Instance._currentWindow?.FitToContent();
         });

         Instance._currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
      }
   }
}