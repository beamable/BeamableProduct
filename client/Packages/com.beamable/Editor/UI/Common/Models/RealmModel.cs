using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Realms;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
    public class RealmModel : ISearchableModel
    {
        public ISearchableElement Current { get; set; }
        public List<ISearchableElement> Elements { get; set; }

        public event Action<List<ISearchableElement>> OnAvailableChanged;
        public event Action<ISearchableElement> OnChanged;

        public void Initialize()
        {
            RefreshAvailable();

            EditorAPI.Instance.Then(api =>
            {
                api.OnRealmChange += HandleRealmChanged;
                Current = api.Realm;
                OnChanged?.Invoke(Current);
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
                OnAvailableChanged?.Invoke(Elements);
            });
        }

        private void HandleRealmChanged(RealmView realm)
        {
            Current = realm;
            try
            {
                OnChanged?.Invoke(realm);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}