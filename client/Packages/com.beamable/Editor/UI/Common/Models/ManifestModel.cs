using System;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Editor.Content;
using Modules.Content;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
    public class ManifestModel
    {
        public event Action<List<AvailableManifestModel>> OnAvailableManifestsChanged;
        public event Action<IEnumerable<AvailableManifestModel>> OnArchivedManifestsFetched;
        public event Action<string> OnManifestChanged;
        public string CurrentManifestId { get; private set; }
        public List<AvailableManifestModel> ManifestModels { get; private set; }

        public IEnumerable<AvailableManifestModel> ArchivedManifestModels { get; private set; } =
            Enumerable.Empty<AvailableManifestModel>();

        public void Initialize()
        {
            RefreshAvailableManifests();

            EditorAPI.Instance.Then(api =>
            {
                ContentIO.OnManifestChanged += HandleManifestChanged;
                ContentIO.OnManifestsListFetched += HandleManifestListFetched;
                ContentIO.OnArchivedManifestsFetched += HandleArchivedManifestListFetched;
                CurrentManifestId = ContentConfiguration.Instance.EditorManifestID;
                OnManifestChanged?.Invoke(CurrentManifestId);
                api.OnRealmChange += _ => RefreshAvailableManifests();
                ContentPublisher.OnContentPublished += () => RefreshAvailableManifests();
            });
        }
        
        public Promise<AvailableManifests> RefreshAvailableManifests() {
            return EditorAPI.Instance.FlatMap(api => {
                CurrentManifestId = ContentConfiguration.Instance.EditorManifestID;
                return api.ContentIO.GetAllManifestIDs();
            });
        }

        private void HandleManifestChanged(string manifestId)
        {
            CurrentManifestId = manifestId;
            try {
                OnManifestChanged?.Invoke(manifestId);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void HandleManifestListFetched(AvailableManifests manifests)
        {
            var nextManifestModels = manifests.manifests;
            if (nextManifestModels.AreManifestIdsEquals(ManifestModels)) return; // short circuit if the manifests are identical.
            
            try {
                ManifestModels = manifests.manifests;
                OnAvailableManifestsChanged?.Invoke(manifests.manifests);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
            if (ContentConfiguration.Instance.EditorManifestID != BeamableConstants.DEFAULT_MANIFEST_ID &&
                manifests.manifests.All(m => m.id != ContentConfiguration.Instance.EditorManifestID))
            {
                EditorUtility.DisplayDialog("No manifest id!",
                    $"There is no manifest named '{ContentConfiguration.Instance.EditorManifestID}' in current realm. Switching into 'global' manifest.",
                    "OK");
                EditorAPI.Instance.Then(api => api.ContentIO.SwitchManifest(BeamableConstants.DEFAULT_MANIFEST_ID));
            }
        }

        private void HandleArchivedManifestListFetched(IEnumerable<AvailableManifestModel> manifests) {
            ArchivedManifestModels = manifests;
            try {
                OnArchivedManifestsFetched?.Invoke(manifests);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}