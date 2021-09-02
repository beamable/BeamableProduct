using System;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Editor.Realms;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
    public class RealmModel
    {
        public event Action<List<RealmView>> OnAvailableRealmsChanged;
        public event Action<RealmView> OnRealmChanged;
        public RealmView CurrentRealm { get; set; }
        public List<RealmView> Realms { get; set; }

        public void Initialize()
        {
            RefreshAvailableRealms();

            EditorAPI.Instance.Then(api =>
            {
                api.OnRealmChange += HandleRealmChanged;
                CurrentRealm = api.Realm;
                OnRealmChanged?.Invoke(CurrentRealm);
            });
        }
        
        public Promise<List<RealmView>> RefreshAvailableRealms()
        {
            return EditorAPI.Instance.FlatMap(api =>
            {
                CurrentRealm = api.Realm;
                return api.RealmService.GetRealms();
            }).Then(realms =>
            {
                
                Realms = realms;
                OnAvailableRealmsChanged?.Invoke(realms);
            });
        }

        private void HandleRealmChanged(RealmView realm)
        {
            CurrentRealm = realm;
            try
            {
                OnRealmChanged?.Invoke(realm);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}