using Beamable.Common;
using Beamable.Editor.Realms;
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
			RefreshAvailable();

			EditorAPI.Instance.Then(api =>
			{
				api.OnRealmChange -= HandleRealmChanged;
				api.OnRealmChange += HandleRealmChanged;
				Current = api.Realm;
				OnElementChanged?.Invoke(Current);
			});
		}

		public Promise<List<ISearchableElement>> RefreshAvailable()
		{
			return EditorAPI.Instance.FlatMap(api =>
			{
				Current = api.Realm;
				return api.RealmService.GetRealms().Map(realms =>
				{
					return realms.ToList<ISearchableElement>();
				});
			}).Then(realms =>
			{

				Elements = realms.ToList<ISearchableElement>();
				OnAvailableElementsChanged?.Invoke(Elements);
			});
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
