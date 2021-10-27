using System;
using Modules.Inventory.Prototype;

namespace Modules.Content
{
    public class AnnouncementsPresenter : CollectionPresenter<AnnouncementsCollection>
    {
        private void OnEnable()
        {
            Collection = new AnnouncementsCollection(OnCollectionUpdated);
        }

        private void OnDisable()
        {
            Collection?.Unsubscribe();
            Collection = null;
        }

        private void OnCollectionUpdated()
        {
            
        }
    }
}
