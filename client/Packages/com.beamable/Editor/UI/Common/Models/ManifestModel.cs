using Beamable.Common;
using Beamable.Editor.Content;
using Modules.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
	public class ManifestModel : ISearchableModel
	{
		public event Action<List<ISearchableElement>> OnAvailableElementsChanged;
		public event Action<IEnumerable<AvailableManifestModel>> OnArchivedManifestsFetched;
		public event Action<ISearchableElement> OnElementChanged;

		public ISearchableElement Default
		{
			get;
			set;
		}

		public ISearchableElement Current
		{
			get;
			set;
		}

		public List<ISearchableElement> Elements
		{
			get;
			set;
		}

		public IEnumerable<AvailableManifestModel> ArchivedManifestModels
		{
			get;
			private set;
		} =
			Enumerable.Empty<AvailableManifestModel>();

		public void Initialize()
		{
			Default = new AvailableManifestModel() { id = BeamableConstants.DEFAULT_MANIFEST_ID };
			RefreshAvailable();

			EditorAPI.Instance.Then(api =>
			{
				ContentIO.OnManifestChanged += HandleManifestChanged;
				ContentIO.OnManifestsListFetched += HandleManifestListFetched;
				ContentIO.OnArchivedManifestsFetched += HandleArchivedManifestListFetched;

				Current = new AvailableManifestModel() { id = ContentConfiguration.Instance.EditorManifestID };
				OnElementChanged?.Invoke(Current);

				api.OnRealmChange += _ => RefreshAvailable();
				ContentPublisher.OnContentPublished += () => RefreshAvailable();
			});
		}

		public Promise<List<ISearchableElement>> RefreshAvailable()
		{
			return EditorAPI.Instance.FlatMap(api =>
			{
				Current = new AvailableManifestModel() { id = ContentConfiguration.Instance.EditorManifestID };
				return api.ContentIO.GetAllManifestIDs().Map(manifest =>
				{
					return manifest.manifests.ToList<ISearchableElement>();
				});
			});
		}

		public Promise<AvailableManifests> RefreshAvailableManifests()
		{
			return EditorAPI.Instance.FlatMap(api =>
			{
				Current = new AvailableManifestModel() { id = ContentConfiguration.Instance.EditorManifestID };
				return api.ContentIO.GetAllManifestIDs();
			});
		}

		private void HandleManifestChanged(string manifestId)
		{
			Current = new AvailableManifestModel() { id = ContentConfiguration.Instance.EditorManifestID };
			try
			{
				OnElementChanged?.Invoke(Current);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private void HandleManifestListFetched(AvailableManifests manifests)
		{
			var nextManifestModels = manifests.manifests;

			if (nextManifestModels.AreManifestIdsEquals(Elements?.OfType<AvailableManifestModel>().ToList()))
				return; // short circuit if the manifests are identical.

			try
			{
				Elements = manifests.manifests.ToList<ISearchableElement>();
				OnAvailableElementsChanged?.Invoke(Elements);
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
		}

		private void HandleArchivedManifestListFetched(IEnumerable<AvailableManifestModel> manifests)
		{
			ArchivedManifestModels = manifests;
			try
			{
				OnArchivedManifestsFetched?.Invoke(manifests);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}
