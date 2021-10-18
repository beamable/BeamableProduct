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
    public class RealmDropdownVisualElement : BeamableVisualElement
    {
        private VisualElement _listRoot;
        private RealmView _selectedRealm;
        private List<RealmView> _realmViews;
        private LoadingIndicatorVisualElement _loadingIndicator;
        private VisualElement _mainContent;
        private SearchBarVisualElement _searchBar;
        private Button _refreshButton;
        public RealmModel Model { get; set; }

#pragma warning disable 67
        public event Action<RealmView> OnRealmSelected;
#pragma warning restore 67

        public RealmDropdownVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(RealmDropdownVisualElement)}/{nameof(RealmDropdownVisualElement)}")
        {

        }

        public override void OnDetach()
        {
            Model.OnAvailableRealmsChanged -= OnRealmsUpdated;
            Model.OnRealmChanged -= OnActiveRealmChanged;
            base.OnDetach();
        }

        private void OnActiveRealmChanged(RealmView realm)
        {
            _selectedRealm = realm;
            SetRealmList(_realmViews, _listRoot);
        }

        public override void Refresh()
        {
            base.Refresh();

            _loadingIndicator = Root.Q<LoadingIndicatorVisualElement>();
            _mainContent = Root.Q<VisualElement>("mainBlockedContent");
            _searchBar = Root.Q<SearchBarVisualElement>();
            _refreshButton = Root.Q<Button>("refreshButton");
            _listRoot = Root.Q<VisualElement>("realmList");

            _loadingIndicator.SetPromise(Model.RefreshAvailableRealms(), _mainContent);
            _refreshButton.clickable.clicked += () =>
                {
                    _loadingIndicator.SetPromise(Model.RefreshAvailableRealms(), _mainContent);
                };

            _selectedRealm = Model.CurrentRealm;
            Model.OnAvailableRealmsChanged -= OnRealmsUpdated;
            Model.OnAvailableRealmsChanged += OnRealmsUpdated;
            Model.OnRealmChanged -= OnActiveRealmChanged;
            Model.OnRealmChanged += OnActiveRealmChanged;

            _searchBar.OnSearchChanged += filter =>
            {
                SetRealmList(Model.Realms, _listRoot, filter.ToLower());
            };
            _searchBar.DoFocus();
            OnRealmsUpdated(Model.Realms);
        }

        private void OnRealmsUpdated(List<RealmView> realms)
        {
            _realmViews = realms;
            SetRealmList(_realmViews, _listRoot);
        }

        private void SetRealmList(IEnumerable<RealmView> allRealms, VisualElement listRoot, string filter = null)
        {
            listRoot.Clear();
            if (allRealms == null) return;

            allRealms = allRealms.Where(r => !r.Archived).OrderBy(r => -r.Depth);
            foreach (var realm in allRealms)
            {
                if (!string.IsNullOrEmpty(filter) && !realm.ProjectName.ToLower().Contains(filter)) continue;

                var realmSelectButton = new Button();
                realmSelectButton.text = realm.DisplayName;
                realmSelectButton.clickable.clicked += () =>
                {
                    _loadingIndicator.SetText("Switching Realm");
                    _loadingIndicator.SetPromise(new Promise<int>(), _mainContent, _refreshButton);
                    _searchBar.SetEnabled(false);
                    EditorApplication.delayCall += () => OnRealmSelected?.Invoke(realm);
                };

                if (realm.Equals(_selectedRealm))
                {
                    realmSelectButton.AddToClassList("selected");
                    realmSelectButton.SetEnabled(false);
                }
                if (realm.IsProduction)
                {
                    realmSelectButton.AddToClassList("production");
                } else if (realm.IsStaging)
                {
                    realmSelectButton.AddToClassList("staging");
                }

                listRoot.Add(realmSelectButton);

            }
        }

    }
}