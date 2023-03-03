using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using Beamable.Common.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
	public class RealmModel : ISearchableModel
	{
		public ISearchableElement Default { get; set; }
		public ISearchableElement Current { get; set; }
		public List<ISearchableElement> Elements { get; set; }

		public event Action<List<ISearchableElement>> OnAvailableElementsChanged;
		public event Action<ISearchableElement> OnElementChanged;

		public void Initialize()
		{
			var api = BeamEditorContext.Default;
			api.OnRealmChange -= HandleRealmChanged;
			api.OnRealmChange += HandleRealmChanged;
			Current = api.CurrentRealm;
			OnElementChanged?.Invoke(Current);

			Elements = api?.EditorAccount?.RealmsInCurrentGame?.ToList<ISearchableElement>() ?? new List<ISearchableElement>();
		}

		public bool RefreshOnStart => false;

		public async Promise<List<ISearchableElement>> RefreshAvailable()
		{
			var api = BeamEditorContext.Default;
			Current = api.CurrentRealm;

			await api.EditorAccount.UpdateRealms(api.Requester);
			Elements = api.EditorAccount.RealmsInCurrentGame.ToList<ISearchableElement>();
			OnAvailableElementsChanged?.Invoke(Elements);
			return Elements;
		}

		private void HandleRealmChanged(RealmView realm)
		{
			Current = realm;
			try
			{
				OnElementChanged?.Invoke(realm);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}
