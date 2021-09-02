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
        public event Action<string> OnManifestChanged;
        public string CurrentManifestId { get; set; }
        public List<AvailableManifestModel> ManifestModels { get; set; }

        public void Initialize()
        {
            RefreshAvailableManifests();

            EditorAPI.Instance.Then(api =>
            {
                ContentIO.OnManifestChanged += HandleManifestChanged;
                CurrentManifestId = ContentConfiguration.Instance.EditorManifestID;
                OnManifestChanged?.Invoke(CurrentManifestId);
                api.OnRealmChange += _ => RefreshAvailableManifests();
                ContentPublisher.OnContentPublished += () => RefreshAvailableManifests();
            });
        }
        
        public Promise<AvailableManifests> RefreshAvailableManifests()
        {
            return EditorAPI.Instance.FlatMap(api =>
            {
                CurrentManifestId = ContentConfiguration.Instance.EditorManifestID;
                return api.ContentIO.GetAllManifestIDs();
            }).Then(manifests =>
            {
                var nextManifestModels = manifests.manifests;
                if (nextManifestModels.AreManifestIdsEquals(ManifestModels)) return; // short circuit if the manifests are identical.

                ManifestModels = manifests.manifests;
                try
                {
                    OnAvailableManifestsChanged?.Invoke(ManifestModels);
                }
                catch (Exception e)
                {
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
            });
        }

        private void HandleManifestChanged(string manifestId)
        {
            CurrentManifestId = manifestId;
            OnManifestChanged?.Invoke(manifestId);
        }
    }
}