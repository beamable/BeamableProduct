using Beamable;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using Editor.UI2.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.UI2.ContentWindow
{
	public class ContentWindow : BeamEditorWindow<ContentWindow>
	{

		private const int HEADER_BUTTON_WIDTH = 50;
		
		public SearchData contentSearchData;
		
		static ContentWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "New Content Manager",
				DockPreferenceTypeName = typeof(SceneView).AssemblyQualifiedName,
				FocusOnShow = false,
				RequireLoggedUser = true,
				RequirePid = true,
			};
		}
		
		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"New Content Manager",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();

		protected override void Build()
		{
			contentSearchData = new SearchData() {onEndCheck = Repaint};
		}

		protected override void DrawGUI()
		{
			DrawHeader();
			
			RunDelayedActions();
		}

		private void DrawHeader()
		{
			bool clickedCreate = false, clickedValidate = false, clickedPublish = false, clickedDownload = false;
			BeamGUI.DrawHeaderSection(this, ActiveContext, () =>
			{
				clickedCreate = BeamGUI.HeaderButton("Create", BeamGUI.iconPlus, width: HEADER_BUTTON_WIDTH);
				clickedValidate = BeamGUI.HeaderButton("Validate", BeamGUI.iconCheck, width: HEADER_BUTTON_WIDTH);
				clickedPublish = BeamGUI.HeaderButton("Publish", BeamGUI.iconUpload, width: HEADER_BUTTON_WIDTH);
				clickedDownload = BeamGUI.HeaderButton("Download", BeamGUI.iconDownload, width: HEADER_BUTTON_WIDTH);
				EditorGUILayout.Space(5, false);
				this.DrawSearchBar(contentSearchData, true);
				DrawFilterButton(BeamGUI.iconTag, new List<string>() {"tag1", "tag2", "tag3"}, new Dictionary<string, bool>() {{"tag1", false}, {"tag2", false}, {"tag3", false}});
				DrawFilterButton(BeamGUI.iconType, new List<string>() {"type1", "type2", "type3"}, new Dictionary<string, bool>() {{"type1", false}, {"type2", false}, {"type3", false}});
				DrawFilterButton(BeamGUI.iconStatus, new List<string>() {"status1", "status2", "status3"}, new Dictionary<string, bool>() {{"status1", false}, {"status2", false}, {"status3", false}});
				
			}, () =>
			{
				
			}, () =>
			{
				Application.OpenURL("https://docs.beamable.com/docs/content-manager-overview");
			}, Repaint);
			

			if (clickedCreate)
			{
				ShowCreateContentMenu();
			}

			if (clickedValidate)
			{
				
			}

			if (clickedPublish)
			{
				ShowPublishMenu();
			}

			if (clickedDownload)
			{
				ShowDownloadMenu();
			}
			
		}

		private static void ShowDownloadMenu()
		{
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Open Download Window"), false, () => { });
			menu.AddItem(new GUIContent("Reset Content"), false, () => { });
			menu.AddItem(new GUIContent("Download Content (default)"), false, () => { });
			menu.ShowAsContext();
		}

		private static void ShowPublishMenu()
		{
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Open Publish Window"), false, () => { });
			menu.AddItem(new GUIContent("Publish New Content namespace"), false, () => { });
			menu.AddItem(new GUIContent("Archive namespaces"), false, () => { });
			menu.AddItem(new GUIContent("Publish (default)"), false, () => { });
			menu.ShowAsContext();
		}

		private static void ShowCreateContentMenu()
		{
			GenericMenu menu = new GenericMenu();
			var contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
			foreach (ContentTypePair contentTypePair in contentTypeReflectionCache.GetAll().OrderBy(pair => pair.Name))
			{
				string itemName = contentTypePair.Name;
				string createItemName;
					
				string typeName = itemName.Split('.').Last();
				createItemName = $"Create {typeName}";
					
				menu.AddItem(new GUIContent($"{itemName.Replace(".","/")}/{createItemName}"), false, () =>
				{
					Debug.Log(contentTypePair.Type.FullName);	
				});
			}
			menu.ShowAsContext();
		}

		private static void DrawFilterButton(Texture icon, List<string> items, Dictionary<string, bool> states)
		{
			var isClicked = BeamGUI.HeaderButton(null, icon,
			                     width: 30,
			                     padding: 4,
			                     iconPadding: -5,
			                     drawBorder: true);
			var buttonRect = GUILayoutUtility.GetLastRect();
			if(isClicked)
				ToggleListWindow.Show(buttonRect, new Vector2(100, 120), items, states);
		}
	}
}
