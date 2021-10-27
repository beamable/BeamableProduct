using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private string _id;
#pragma warning restore CS0649

        public string Id => _id;

        private BUSSStyleProvider _styleProvider;

        public void NotifyOnStyleChanged(BUSSStyle newStyle)
        {
            if (TryGetComponent<SDFImage>(out var sdfImage))
            {
                sdfImage.Style = newStyle;
            }
        }

        private void OnBeforeTransformParentChanged()
        {
            Unregister();
        }

        private void OnTransformParentChanged()
        {
            LookForStyleProvider();
            Register();
        }

        private void LookForStyleProvider()
        {
            Transform currentTransform = gameObject.transform;

            while (_styleProvider == null)
            {
                if (currentTransform.parent == null)
                {
                    Debug.LogWarning("Haven't found any SDFStyleProvider");
                    break;
                }

                currentTransform = currentTransform.parent;

                BUSSStyleProvider styleProvider = currentTransform.GetComponent<BUSSStyleProvider>();
                _styleProvider = styleProvider;
            }
        }

        private void OnValidate()
        {
            Register();
            
            // TODO: change this, get style only for current gameobject, not invoke change on everyone
            _styleProvider.NotifyOnStyleChanged();
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            // TODO: do we need this? When element will be enabled again, it will be validated and updated with new/another style
            if (TryGetComponent<SDFImage>(out var sdfImage))
            {
                sdfImage.Style = null;
            }

            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Register()
        {
            if (_styleProvider == null)
            {
                LookForStyleProvider();
            }
            
            if (_styleProvider != null)
            {
                _styleProvider.Register(this);
            }
        }

        private void Unregister()
        {
            if (_styleProvider != null)
            {
                _styleProvider.Unregister(this);
                _styleProvider = null;
            }
        }
    }
}