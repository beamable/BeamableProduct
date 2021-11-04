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

        public new class UxmlFactory : UxmlFactory<SearchabledDropdownVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription _changeDesc = new UxmlStringAttributeDescription
            { name = "changeDesc", defaultValue = "" };

            readonly UxmlStringAttributeDescription _selectedClassName = new UxmlStringAttributeDescription
            { name = "selectedClassName", defaultValue = "" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is SearchabledDropdownVisualElement component)
                {
                    component._switchText = _changeDesc.GetValueFromBag(bag, cc);
                    component._selectedClassName = _selectedClassName.GetValueFromBag(bag, cc);
                }
            }
        }

        private string _switchText;
        private string _selectedClassName;

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

        public SearchabledDropdownVisualElement() : base(ComponentPath)
        {

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

            elements = elements.Where(r => !r.Archived).OrderBy(r => -r.Depth);
            foreach (var singleElement in elements)
            {
                if (singleElement.IsElementToSkipInDropdown(filter))
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
                    if (!string.IsNullOrEmpty(_selectedClassName))
                        selectButton.AddToClassList(_selectedClassName);

                    selectButton.SetEnabled(false);
                }

                var classNameToAdd = singleElement.GetClassNameToAddInDropdown();

                if (string.IsNullOrEmpty(classNameToAdd))
                {
                    selectButton.AddToClassList(classNameToAdd);
                }

                listRoot.Add(selectButton);
            }
        }
    }
}