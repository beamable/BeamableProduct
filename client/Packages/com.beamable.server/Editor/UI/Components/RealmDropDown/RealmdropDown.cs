using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.Realms;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class RealmDropdown : MicroserviceComponent
    {
        // private VisualElement _listRoot;
        // private RealmView _selectedRealm;
        // private List<RealmView> _realmViews;
        // public ToolboxModel Model { get; set; }

       // public event Action<RealmView> OnRealmSelected;

        public RealmDropdown() : base(nameof(RealmDropdown))
        {

        }

        // public override void OnDetach()
        // {
        //     Model.OnAvailableRealmsChanged -= OnRealmsUpdated;
        //     Model.OnRealmChanged -= OnActiveRealmChanged;
        //     base.OnDetach();
        // }
        //
        // private void OnActiveRealmChanged(RealmView realm)
        // {
        //     _selectedRealm = realm;
        //     SetRealmList(_realmViews, _listRoot);
        // }
        //
        // public override void Refresh()
        // {
        //     base.Refresh();
        //
        //     var loadingIndicator = Root.Q<LoadingIndicatorVisualElement>();
        //     var mainContent = Root.Q<VisualElement>("mainBlockedContent");
        //     var searchBar = Root.Q<SearchBarVisualElement>();
        //     var refreshButton = Root.Q<Button>("refreshButton");
        //     _listRoot = Root.Q<VisualElement>("realmList");
        //
        //     loadingIndicator.SetPromise(Model.RefreshAvailableRealms(), mainContent);
        //     refreshButton.clickable.clicked += () =>
        //         {
        //             loadingIndicator.SetPromise(Model.RefreshAvailableRealms(), mainContent);
        //         };
        //
        //     _selectedRealm = Model.CurrentRealm;
        //     Model.OnAvailableRealmsChanged -= OnRealmsUpdated;
        //     Model.OnAvailableRealmsChanged += OnRealmsUpdated;
        //     Model.OnRealmChanged -= OnActiveRealmChanged;
        //     Model.OnRealmChanged += OnActiveRealmChanged;
        //
        //     searchBar.OnSearchChanged += filter =>
        //     {
        //         SetRealmList(Model.Realms, _listRoot, filter.ToLower());
        //     };
        //     searchBar.DoFocus();
        //     OnRealmsUpdated(Model.Realms);
        // }
        //
        // private void OnRealmsUpdated(List<RealmView> realms)
        // {
        //     _realmViews = realms;
        //     SetRealmList(_realmViews, _listRoot);
        // }
        //
        // private void SetRealmList(IEnumerable<RealmView> allRealms, VisualElement listRoot, string filter = null)
        // {
        //     listRoot.Clear();
        //     allRealms = allRealms.Where(r => !r.Archived).OrderBy(r => -r.Depth);
        //     foreach (var realm in allRealms)
        //     {
        //         if (!string.IsNullOrEmpty(filter) && !realm.ProjectName.ToLower().Contains(filter)) continue;
        //
        //         var realmSelectButton = new Button();
        //         realmSelectButton.text = realm.DisplayName;
        //         realmSelectButton.clickable.clicked += () =>
        //         {
        //             OnRealmSelected?.Invoke(realm);
        //         };
        //
        //         if (realm.Equals(_selectedRealm))
        //         {
        //             realmSelectButton.AddToClassList("selected");
        //             realmSelectButton.SetEnabled(false);
        //         }
        //         if (realm.IsProduction)
        //         {
        //             realmSelectButton.AddToClassList("production");
        //         } else if (realm.IsStaging)
        //         {
        //             realmSelectButton.AddToClassList("staging");
        //         }
        //
        //         listRoot.Add(realmSelectButton);
        //
        //     }
        // }

    }
}