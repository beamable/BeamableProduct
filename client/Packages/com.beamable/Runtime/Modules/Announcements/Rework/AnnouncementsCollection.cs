using System;
using Beamable.Api;
using Beamable.Api.Announcements;
using Beamable.Common.Api.Announcements;
using Beamable.Modules.Generics;
using UnityEngine;

namespace Beamable.Modules.Content
{
    public class AnnouncementsCollection : DataCollection<AnnouncementView>
    {
        private PlatformSubscription<AnnouncementQueryResponse> _subscription;

        public AnnouncementsCollection(Action onCollectionUpdated) : base(onCollectionUpdated)
        {
        }

        protected sealed override async void Subscribe()
        {
	        var beamable = await API.Instance;
            if (beamable != null)
            {
                AnnouncementsSubscription announcementsSubscription = beamable.AnnouncementService.Subscribable;
                _subscription = announcementsSubscription.Subscribe(HandleSubscription);
            }
        }

        private void HandleSubscription(AnnouncementQueryResponse response)
        {
            foreach (var view in response.announcements)
            {
                Update(view);
            }

            CollectionUpdated?.Invoke();
        }

        private void Update(AnnouncementView view)
        {
            AnnouncementView data = Find(announcement => announcement.id == view.id);

            if (data == null)
            {
                Debug.Log($"Registering new content with id {view.id}");
                Add(view);
            }
        }

        public sealed override void Unsubscribe()
        {
            _subscription?.Unsubscribe();
        }
    }

}
