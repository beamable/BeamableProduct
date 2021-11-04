using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Realms;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common.Models;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
    public class SearchabledDropdownVisualElement : BeamableVisualElement
    {
        public static readonly string ComponentPath = $"{BeamableComponentsConstants.COMP_PATH}/{nameof(SearchabledDropdownVisualElement)}/{nameof(SearchabledDropdownVisualElement)}";

        private string _switchText;

        private VisualElement _root;
        private ISearchableDropDownElement _selectedElement;
        private List<ISearchableDropDownElement> _elementViews;
        private LoadingIndicatorVisualElement _loadingIndicator;
        private VisualElement _mainContent;
        private SearchBarVisualElement _searchBar;
        private Button _refreshButton;
        public ISearchableDropDownModel Model { get; set; }

#pragma warning disable 67
        public event Action<ISearchableDropDownElement> OnRealmSelected;
#pragma warning restore 67

        public SearchabledDropdownVisualElement(string switchText) : base(ComponentPath)
        {
            this._switchText = switchText;
        }

        public override void OnDetach()
        {
            Model.OnAvailableChanged -= OnUpdated;
            Model.OnChanged -= OnActiveChanged;
            base.OnDetach();
        }

        private void OnActiveChanged(ISearchableDropDownElement element)
        {
            _selectedElement = element;
            SetList(_elementViews, _root);
        }

        public override void Refresh()
        {
            base.Refresh();

            _loadingIndicator = Root.Q<LoadingIndicatorVisualElement>();
            _mainContent = Root.Q<VisualElement>("mainBlockedContent");
            _searchBar = Root.Q<SearchBarVisualElement>();
            _refreshButton = Root.Q<Button>("refreshButton");
            _root = Root.Q<VisualElement>("elementsList");

            _loadingIndicator.SetPromise(Model.RefreshAvailable(), _mainContent);
            _refreshButton.clickable.clicked += () =>
                {
                    _loadingIndicator.SetPromise(Model.RefreshAvailable(), _mainContent);
                };

            _selectedElement = Model.Current;
            Model.OnAvailableChanged -= OnUpdated;
            Model.OnAvailableChanged += OnUpdated;
            Model.OnChanged -= OnActiveChanged;
            Model.OnChanged += OnActiveChanged;

            _searchBar.OnSearchChanged += filter =>
            {
                SetList(Model.Elements, _root, filter.ToLower());
            };
            _searchBar.DoFocus();
            OnUpdated(Model.Elements);
        }

        private void OnUpdated(List<ISearchableDropDownElement> elements)
        {
            _elementViews = elements;
            SetList(_elementViews, _root);
        }

        private void SetList(IEnumerable<ISearchableDropDownElement> elements, VisualElement listRoot, string filter = null)
        {
            listRoot.Clear();
            if (elements == null) return;

            elements = elements.Where(r => r.IsAvailable()).OrderBy(r => -r.Depth);
            foreach (var singleElement in elements)
            {
                if (singleElement.IsToSkip(filter))
                    continue;

                var selectButton = new Button();
                selectButton.text = singleElement.DisplayName;
                selectButton.clickable.clicked += () =>
                {
                    _loadingIndicator.SetText(_switchText);
                    _loadingIndicator.SetPromise(new Promise<int>(), _mainContent, _refreshButton);
                    _searchBar.SetEnabled(false);
                    EditorApplication.delayCall += () => OnRealmSelected?.Invoke(singleElement);
                };

                if (singleElement.Equals(_selectedElement))
                {
                    selectButton.AddToClassList("selected");
                    selectButton.SetEnabled(false);
                }

                var classNameToAdd = singleElement.GetClassNameToAdd();

                if (string.IsNullOrEmpty(classNameToAdd))
                {
                    selectButton.AddToClassList(classNameToAdd);
                }

                listRoot.Add(selectButton);
            }
        }
    }
}