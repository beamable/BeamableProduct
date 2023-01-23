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

		public async void Initialize()
		{
			await RefreshAvailable();

			var api = BeamEditorContext.Default;
			api.OnRealmChange -= HandleRealmChanged;
			api.OnRealmChange += HandleRealmChanged;
			Current = api.CurrentRealm;
			OnElementChanged?.Invoke(Current);
		}

		public async Promise<List<ISearchableElement>> RefreshAvailable()
		{
			var api = BeamEditorContext.Default;
			Current = api.CurrentRealm;

			try
			{
				 return await api.ServiceScope.GetService<RealmsService>().GetRealms()
				         .Map(realms => realms.ToList<ISearchableElement>())
				         .Then(realms =>
				         {
					         Elements = realms.ToList<ISearchableElement>();
					         OnAvailableElementsChanged?.Invoke(Elements);
				         });
			}
			catch (RequesterException ex)
			{
				if (ex.Status == 400) // realm is archived
				{
					await api.Relogin();
					var realms = await RefreshAvailable();
					OnElementChanged?.Invoke(Current);
					return realms;
				}

				throw;
			}
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
