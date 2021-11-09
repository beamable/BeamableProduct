using UnityEngine;

namespace Beamable.Modules.Generics
{
    public abstract class ModelPresenter<T> : MonoBehaviour where T : Model, new()
    {
        protected T Model;

        protected virtual void Awake()
        {
            Model = new T();
            Model.OnInitialized = Initialized;
            Model.OnChange = Refresh;
        }

        protected void OnDestroy()
        {
            Model.OnInitialized = null;
            Model.OnChange = null;
        }

        protected abstract void Initialized();
        protected abstract void Refresh();
    }
}