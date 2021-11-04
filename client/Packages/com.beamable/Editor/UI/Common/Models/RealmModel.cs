using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Realms;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
    public class RealmModel : ISearchableDropDownModel
    {
        public ISearchableDropDownElement Current { get; set; }
        public List<ISearchableDropDownElement> Elements { get; set; }

        public event Action<List<ISearchableDropDownElement>> OnAvailableChanged;
        public event Action<ISearchableDropDownElement> OnChanged;

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

        public Promise<List<ISearchableDropDownElement>> RefreshAvailable()
        {
            return EditorAPI.Instance.FlatMap(api =>
            {
                Current = api.Realm;
                return api.RealmService.GetRealms().Map(realms =>
                {
                    return realms.ToList<ISearchableDropDownElement>();
                });
            }).Then(realms =>
            {

                Elements = realms.ToList<ISearchableDropDownElement>();
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