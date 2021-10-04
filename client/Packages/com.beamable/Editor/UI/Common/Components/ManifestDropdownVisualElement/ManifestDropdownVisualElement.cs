using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.Content;
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
    public class ManifestDropdownVisualElement : BeamableVisualElement
    {
        private VisualElement _listRoot;
        private string _currentManifestId;
        private List<AvailableManifestModel> _availableManifestModels;
        private Button _editButton;
        public ManifestModel Model { get; set; }

        private bool _editModeOn;

        public event Action<string> OnManifestSelected;

        public ManifestDropdownVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(ManifestDropdownVisualElement)}/{nameof(ManifestDropdownVisualElement)}")
        { }

        public override void OnDetach()
        {
            Model.OnAvailableManifestsChanged -= OnManifestsUpdated;
            Model.OnManifestChanged -= OnActiveManifestChanged;
            base.OnDetach();
        }

        private void OnActiveManifestChanged(string manifestId)
        {
            _currentManifestId = manifestId;
            SetManifestList(_availableManifestModels, _listRoot);
        }

        public override void Refresh()
        {
            base.Refresh();

            var loadingIndicator = Root.Q<LoadingIndicatorVisualElement>();
            var mainContent = Root.Q<VisualElement>("mainBlockedContent");
            var searchBar = Root.Q<SearchBarVisualElement>();
            var refreshButton = Root.Q<Button>("refreshButton");
            _editButton = Root.Q<Button>("editButton");
            _listRoot = Root.Q<VisualElement>("manifestList");

            _currentManifestId = Model.CurrentManifestId;
            Model.OnAvailableManifestsChanged -= OnManifestsUpdated;
            Model.OnAvailableManifestsChanged += OnManifestsUpdated;
            Model.OnManifestChanged -= OnActiveManifestChanged;
            Model.OnManifestChanged += OnActiveManifestChanged;

            loadingIndicator.SetPromise(Model.RefreshAvailableManifests(), mainContent);
            refreshButton.clickable.clicked += () =>
            {
                loadingIndicator.SetPromise(Model.RefreshAvailableManifests(), mainContent);
            };
            
            _editButton.clickable.clicked -= ToggleEditMode;
            _editButton.clickable.clicked += ToggleEditMode;

            searchBar.OnSearchChanged += filter =>
            {
                SetManifestList(Model.ManifestModels, _listRoot, filter.ToLower());
            };
            searchBar.DoFocus();
            OnManifestsUpdated(Model.ManifestModels);
        }
        
        private void ToggleEditMode() {
            _editModeOn = !_editModeOn;
            Root.ToggleInClassList("editMode");
        }

        private void OnManifestsUpdated(List<AvailableManifestModel> manifestModels)
        {
            _availableManifestModels = manifestModels;
            SetManifestList(_availableManifestModels, _listRoot);
        }

        private void SetManifestList(IEnumerable<AvailableManifestModel> allManifestModels, VisualElement listRoot, string filter = null)
        {
            listRoot.Clear();
            foreach (var manifestModel in allManifestModels.OrderBy(x => x.id))
            {
                var id = manifestModel.id;
                var manifestSelectButton = new Button();
                manifestSelectButton.text = id;
                manifestSelectButton.clickable.clicked += () =>
                {
                    OnManifestSelected?.Invoke(id);
                };

                if (manifestModel.id.Equals(_currentManifestId))
                {
                    manifestSelectButton.AddToClassList("selected");
                    manifestSelectButton.SetEnabled(false);
                }
                else if (!manifestModel.id.Equals(BeamableConstants.DEFAULT_MANIFEST_ID))
                {
                    var deleteManifestButton = new Button();
                    manifestSelectButton.Add(deleteManifestButton);
                    deleteManifestButton.AddToClassList("deleteManifestButton");
                    deleteManifestButton.clickable.clicked += () =>
                    {
                        var ifDelete = EditorUtility.DisplayDialog(
                            "Deleting manifest version",
                            $"Are you sure you want to archive manifest named '{id}'\n" +
                            $"This operation will archive it permanently for all users!",
                            "Yes", "No");

                        if (ifDelete)
                        {
                            EditorAPI.Instance.Then(api => {
                                api.ContentIO.ArchiveManifests(id);
                            });
                        }
                    };
                }
                
                listRoot.Add(manifestSelectButton);

            }
        }

    }
}