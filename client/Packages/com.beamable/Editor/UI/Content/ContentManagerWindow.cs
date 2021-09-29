using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
using System.Reflection;
using Beamable.Editor.UI.Buss;
using System.Linq;
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
         RebuildContentPopupsAfterEditorReload();
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
         _contentManager?.Destroy();
         _contentManager = new ContentManager();
         _contentManager.Initialize();

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
            
            _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.ValidateContent, GetValidateContentVisualElement(), this);
            _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
         };

         _actionBarVisualElement.OnPublishButtonClicked += (createNew) =>
         {
            if (_currentWindow != null)
            {
               _currentWindow.Close();
            }
            
            // validate and create publish set.
            var validatePopup = new ValidateContentVisualElement();
            validatePopup.DataModel = _contentManager.Model;

            _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.ValidateContent, validatePopup, this);
            _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
            
            if (createNew)
            {
               _currentWindow.minSize = new Vector2(_currentWindow.minSize.x, _currentWindow.minSize.y + 100);
            }

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

            _contentManager.ValidateContent(validatePopup.SetProgress, validatePopup.HandleValidationErrors)
               .Then(errors =>
               {
                  validatePopup.HandleFinished();

                  if (errors.Count != 0) return;

                   _cachedCreateNewManifestFlag = createNew;
                  _currentWindow.SwapContent(GetPublishContentVisualElement());
                  _currentWindow.titleContent = new GUIContent("Publish Content");
               });
         };

         _actionBarVisualElement.OnDownloadButtonClicked += () =>
         {
            if (_currentWindow != null)
            {
               _currentWindow.Close();
            }

            _cachedItemsToDownload = null;
            _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.DownloadContent, GetDownloadContentVisualElement(), this);
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
         _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.DownloadContent, GetDownloadContentVisualElement(), this);
         _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
      }

      private void Update() {
         _actionBarVisualElement.RefreshPublishDropdownVisibility();
      }

      private void RebuildContentPopupsAfterEditorReload()
      {
            EditorApplication.delayCall += () =>
            {
                if (_currentWindow != null)
                {
                    string titleText = _currentWindow.titleContent.text;

                    Type thisType = this.GetType();
                    MethodInfo theMethod = thisType.GetMethod($"Get{_currentWindow.ContentType}", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (theMethod != null)
                    {
                        var returnValue = (BeamableVisualElement)theMethod.Invoke(this, null);

                        if (returnValue != null)
                        {
                            _currentWindow?.Close();
                            _currentWindow = null;

                            _currentWindow = BeamablePopupWindow.ShowUtility(titleText, returnValue, null);
                            _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
                        }
                    }
                }
            };
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
                _contentManager.DownloadContent(summary, prog, finished).Then(_ => SoftReset());
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
                _contentManager?.ValidateContent(validatePopup.SetProgress, validatePopup.HandleValidationErrors)
               .Then(_ => validatePopup.HandleFinished());
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
                            _contentManager.PublishContent(set, prog, finished).Then(__ => SoftReset());
                        });
                    });
                }
                else
                {
                    _contentManager.PublishContent(set, prog, finished).Then(_ => SoftReset());
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
      private static async Task ResetContent()
      {
         if (Instance == null)
         {
            await Init();
         }

         Instance._currentWindow?.Close();
         Instance._currentWindow = null;

         Instance._currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.RemoveLocalContent, Instance.GetResetContentVisualElement(), null);
         Instance._currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
      }
   }
}