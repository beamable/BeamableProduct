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
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Content
{
   public class ContentManagerWindow : EditorWindow
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
         var contentManagerWindow = GetWindow<ContentManagerWindow>(BeamableConstants.CONTENT_MANAGER, true, typeof(ContentManagerWindow),typeof(SceneView));
         contentManagerWindow.Show(true);
      }
      
      public static ContentManagerWindow Instance { get; private set; }
      public static bool IsInstantiated { get { return Instance != null; } }
      
      private ContentManager _contentManager;
      private VisualElement _windowRoot;
      private VisualElement _explorerContainer, _statusBarContainer;

      private ActionBarVisualElement _actionBarVisualElement;
      private ExplorerVisualElement _explorerElement;
      private StatusBarVisualElement _statusBarElement;
      private BeamablePopupWindow _currentWindow;

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
            
            var validatePopup = new ValidateContentVisualElement();
            validatePopup.DataModel = _contentManager.Model;
            _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.ValidateContent, validatePopup, this);
            _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;

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
               .Then(_ => validatePopup.HandleFinished());
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

                  var publishPopup = new PublishContentVisualElement();
                  publishPopup.CreateNewManifest = createNew;
                  publishPopup.DataModel = _contentManager.Model;
                  publishPopup.PublishSet = _contentManager.CreatePublishSet(createNew);

                  _currentWindow.SwapContent(publishPopup);
                  _currentWindow.titleContent = new GUIContent("Publish Content");

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
                  
                  publishPopup.OnPublishRequested += (set, prog, finished) =>
                  {
                     if (createNew)
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

               });
         };

         _actionBarVisualElement.OnDownloadButtonClicked += () =>
         {
            if (_currentWindow != null)
            {
               _currentWindow.Close();
            }
            
            var downloadPopup = new DownloadContentVisualElement();

            downloadPopup.Model = _contentManager.PrepareDownloadSummary();
            _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.DownloadContent, downloadPopup, this);
            _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;
            
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
         
         var downloadPopup = new DownloadContentVisualElement();

         downloadPopup.Model = _contentManager.PrepareDownloadSummary(items.ToArray());
         _currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.DownloadContent, downloadPopup, this);
         _currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;

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
            _contentManager.DownloadContent(summary, prog, finished).Then(_ => Refresh());
         };
      }

      private void Update() {
         _actionBarVisualElement.RefreshPublishDropdownVisibility();
      }

      [MenuItem(BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Reset Content")]
      private static async Task ResetContent() {

         if (Instance == null)
         {
            await Init();
         }

            var contentManagerWindow = GetWindow<ContentManagerWindow>(BeamableConstants.CONTENT_MANAGER, true, typeof(ContentManagerWindow), typeof(SceneView));

            var clearPopup = new ResetContentVisualElement();
            clearPopup.Model = Instance._contentManager.PrepareDownloadSummary();
            clearPopup.DataModel = Instance._contentManager.Model;

            contentManagerWindow._currentWindow = BeamablePopupWindow.ShowUtility(ContentManagerConstants.RemoveLocalContent, clearPopup, null);
            contentManagerWindow._currentWindow.minSize = ContentManagerConstants.WindowSizeMinimum;

            clearPopup.OnRefreshContentManager += () => Instance._contentManager.RefreshWindow(true);
            clearPopup.OnClosed += () =>
            {
                contentManagerWindow._currentWindow.Close();
                contentManagerWindow._currentWindow = null;
            };

            clearPopup.OnCancelled += () =>
            {
                contentManagerWindow._currentWindow.Close();
                contentManagerWindow._currentWindow = null;
            };

            clearPopup.OnDownloadStarted += (summary, prog, finished) =>
            {
                Instance._contentManager?.DownloadContent(summary, prog, finished).Then(_ =>
                {
                    Instance._contentManager?.Model.TriggerSoftReset();
                });
                ;
            };
        }
   }
}