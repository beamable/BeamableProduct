using Beamable.Content;
using Beamable.Editor.Content.Models;
using Beamable.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.ContentManager.ContentList;
using static Beamable.Common.Constants;

namespace Beamable.Editor.Content.Components
{
	// TODO: TD213896
	public class ActionBarVisualElement : ContentManagerComponent
	{
		public new class UxmlFactory : UxmlFactory<ActionBarVisualElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{ name = "custom-text", defaultValue = "nada" };

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as ActionBarVisualElement;
			}
		}

		public event Action<ContentTypeDescriptor> OnAddItemButtonClicked;
		public event Action OnValidateButtonClicked;
		public event Action<bool> OnPublishButtonClicked;
		public event Action OnDownloadButtonClicked;
		public event Action OnRefreshButtonClicked;
		public event Action OnDocsButtonClicked;

		public ContentDataModel Model { get; internal set; }

		private SearchBarVisualElement _searchBar;
		private Button _createNewButton, _validateButton, _publishButton, _publishDropdownButton, _downloadButton;
		private Button _tagButton, _typeButton, _statusButton, _refreshButton, _docsButton;
		private bool _mouseOverPublishDropdown;

		public ActionBarVisualElement() : base(nameof(ActionBarVisualElement))
		{
		}

		public override void Refresh()
		{
			base.Refresh();

			//Buttons (Left To Right in UX)

			_createNewButton = Root.Q<Button>("createNewButton");
			var manipulator = new ContextualMenuManipulator(CreateNewButton_CreateMenu);
			manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_createNewButton.clickable.activators.Clear();
			_createNewButton.AddManipulator(manipulator);

			// _createNewButton.clickable.clicked += () => { CreateNewButton_OnClicked(_createNewButton.worldBound); };

			_validateButton = Root.Q<Button>("validateButton");
			_validateButton.clickable.clicked += () => { OnValidateButtonClicked?.Invoke(); };

			_publishButton = Root.Q<Button>("publishButton");
			_publishButton.SetEnabled(Model.UserCanPublish);

			_publishDropdownButton = Root.Q<Button>("publishDropdownButton");
			_publishDropdownButton.RegisterCallback<MouseEnterEvent>(evt => _mouseOverPublishDropdown = true);
			_publishDropdownButton.RegisterCallback<MouseLeaveEvent>(evt => _mouseOverPublishDropdown = false);
			_publishDropdownButton.clickable.activators.Clear();

			var publishButtonManipulator = new ContextualMenuManipulator(HandlePublishButtonClick);
			publishButtonManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_publishButton.clickable.activators.Clear();
			_publishButton.AddManipulator(publishButtonManipulator);

			Model.OnUserCanPublishChanged += _publishButton.SetEnabled;

			_downloadButton = Root.Q<Button>("downloadButton");
			_downloadButton.clickable.clicked += () => { OnDownloadButtonClicked?.Invoke(); };

			_tagButton = Root.Q<Button>("tagButton");
			_tagButton.clickable.clicked += () => { TagButton_OnClicked(_tagButton.worldBound); };
			_tagButton.tooltip = Tooltips.ContentManager.TAG;

			_typeButton = Root.Q<Button>("typeButton");
			_typeButton.clickable.clicked += () => { TypeButton_OnClicked(_typeButton.worldBound); };
			_typeButton.tooltip = Tooltips.ContentManager.TYPE;

			_statusButton = Root.Q<Button>("statusButton");
			_statusButton.clickable.clicked += () => { StatusButton_OnClicked(_statusButton.worldBound); };
			_statusButton.tooltip = Tooltips.ContentManager.STATUS;

			_refreshButton = Root.Q<Button>("refreshButton");
			_refreshButton.clickable.clicked += () => { OnRefreshButtonClicked?.Invoke(); };
			_refreshButton.tooltip = Tooltips.ContentManager.REFRESH;

			_docsButton = Root.Q<Button>("docsButton");
			_docsButton.clickable.clicked += () => { OnDocsButtonClicked?.Invoke(); };
			_docsButton.tooltip = Tooltips.ContentManager.DOCUMENT;

			_searchBar = Root.Q<SearchBarVisualElement>();
			Model.OnQueryUpdated += (query, force) =>
			{
				var existing = force
				? null
				: _searchBar.Value;

				var filterString = query?.ToString(existing) ?? "";
				_searchBar.SetValueWithoutNotify(filterString);
			};
			_searchBar.OnSearchChanged += SearchBar_OnSearchChanged;
		}

		private void SearchBar_OnSearchChanged(string obj)
		{
			var query = EditorContentQuery.Parse(obj);
			Model.SetFilter(query);
		}

		public void RefreshPublishDropdownVisibility()
		{
			if (_publishDropdownButton?.parent == null) return;

			_publishDropdownButton.parent.EnableInClassList("hidden",
														!ContentConfiguration.Instance.EnableMultipleContentNamespaces);
		}

		private void CreateNewButton_OnClicked(Rect visualElementBounds)
		{
			//
			// Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);
			//
			// var content = new CreateNewPopupVisualElement();
			// content.Model = Model;
			// int selectedItemCount = content.GetSelectedItemCount();
			//
			// var wnd = BeamablePopupWindow.ShowDropdown(ContentManagerConstants.CreateNewPopupWindowTitle,
			//    popupWindowRect, new Vector2(160, Math.Min(400, 30*selectedItemCount)), content);
			// // var wnd = BeamablePopupWindow.ShowDropdown(ContentManagerConstants.CreateNewPopupWindowTitle,
			// //    popupWindowRect, ContentManagerConstants.CreateNewPopupWindowSize, content);
			//
			// content.OnAddItemButtonClicked += (typeDescriptor) =>
			// {
			//    OnAddItemButtonClicked?.Invoke(typeDescriptor);
			//
			//    wnd.Close();
			// };

		}

		private void CreateNewButton_CreateMenu(ContextualMenuPopulateEvent evt)
		{
			// Create menu for create button
			List<ContentTypeDescriptor> typeDescriptors = new List<ContentTypeDescriptor>();
			if (Model.SelectedContentTypes == null || Model.SelectedContentTypes.Count == 0)
			{
				// Create all types
				typeDescriptors = Model.GetContentTypes().ToList();
			}
			else
			{
				// Only selected
				var items = Model.SelectedContentTypes.ToList();
				foreach (var selectedViewItem in items)
				{
					ContentTypeTreeViewItem contentTypeTreeViewItem = (ContentTypeTreeViewItem)selectedViewItem;
					typeDescriptors.Add(contentTypeTreeViewItem.TypeDescriptor);
				}
			}
			typeDescriptors.Sort((type1, type2) =>
			{
				return type1.TypeName.CompareTo(type2.TypeName);
			});

			foreach (var typeDescriptor in typeDescriptors)
			{
				String[] splitPath = typeDescriptor.TypeName.Split('.');
				String menuPathName = $"{splitPath[0]}";
				for (int i = 1; i < splitPath.Length; ++i)
					menuPathName += $"/{splitPath[i]}";
				menuPathName += $"/{CONTENT_LIST_CREATE_ITEM} {splitPath[splitPath.Length - 1]}";

				evt.menu.BeamableAppendAction(menuPathName, (Action<Vector2>)((pos) =>
				{
					OnAddItemButtonClicked?.Invoke(typeDescriptor);
				}));
			}
		}

		private void HandlePublishButtonClick(ContextualMenuPopulateEvent evt)
		{
			if (_mouseOverPublishDropdown)
			{
				evt.menu.BeamableAppendAction("Publish new Content namespace", pos => { OnPublishButtonClicked(true); });
				evt.menu.BeamableAppendAction("Archive namespaces", pos => ArchiveManifestsVisualElement.OpenAsUtilityWindow());
				evt.menu.BeamableAppendAction("Publish (default)", pos => { OnPublishButtonClicked(false); });
			}
			else
				OnPublishButtonClicked?.Invoke(false);
		}

		private void TagButton_OnClicked(Rect visualElementBounds)
		{
			Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);
			var content = new TagFilterPopupVisualElement();
			content.Model = Model;
			var longest = "";
			foreach (var name in Model.GetAllTags())
			{
				if (name.Length > longest.Length)
				{
					longest = name;
				}
			}

			var width = Mathf.Max(120, longest.Length * 7 + 30);
			var wnd = BeamablePopupWindow.ShowDropdown("Filter Tag", popupWindowRect, new Vector2(width, 200), content);


			//content.OnSelected += (wrapper, name) =>
			//{
			//    wnd.Close();
			//    EditorUtility.SetDirty(Model.Sheet);
			//    wrapper.Create(name);
			//    _styleObjectElement.Refresh();
			//    VariableAddOrRemoved?.Invoke();
			//};

			content.Refresh();

		}


		private void TypeButton_OnClicked(Rect visualElementBounds)
		{
			Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);

			var content = new TypeFilterPopupVisualElement();
			content.Model = Model;

			var longest = "";
			foreach (var name in Model.GetContentTypes().Select(t => t.TypeName))
			{
				if (name.Length > longest.Length)
				{
					longest = name;
				}
			}

			var width = Mathf.Max(120, longest.Length * 7 + 30);
			var wnd = BeamablePopupWindow.ShowDropdown("Filter Tag", popupWindowRect, new Vector2(width, 200), content);

			//content.OnSelected += (wrapper, name) =>
			//{
			//    wnd.Close();
			//    EditorUtility.SetDirty(Model.Sheet);
			//    wrapper.Create(name);
			//    _styleObjectElement.Refresh();
			//    VariableAddOrRemoved?.Invoke();
			//};

			content.Refresh();
		}

		private void StatusButton_OnClicked(Rect visualElementBounds)
		{
			Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);

			var content = new StatusFilterPopupVisualElement();
			content.Model = Model;
			var wnd = BeamablePopupWindow.ShowDropdown("Filter Tag", popupWindowRect, new Vector2(110, 110), content);

			//content.OnSelected += (wrapper, name) =>
			//{
			//    wnd.Close();
			//    EditorUtility.SetDirty(Model.Sheet);
			//    wrapper.Create(name);
			//    _styleObjectElement.Refresh();
			//    VariableAddOrRemoved?.Invoke();
			//};

			content.Refresh();

		}
	}
}
