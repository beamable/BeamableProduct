using UnityEngine;

namespace Modules.Inventory.Prototype
{
    public abstract class CollectionPresenter<T> : MonoBehaviour where T : class
    {
        protected T Collection { get; set; }
    }
}