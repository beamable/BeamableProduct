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

        [SerializeField] private SDFStyleProvider _parent;

        private void OnBeforeTransformParentChanged()
        {
            if (_parent != null)
            {
                _parent.Unregister(this);
                _parent = null;
            }
        }

        private void OnTransformParentChanged()
        {
            Transform currentTransform = gameObject.transform;
            
            while (_parent == null)
            {
                if (currentTransform.parent == null)
                {
                    Debug.LogWarning("Haven't found any SDFStyleProvider");
                    break;
                }
                
                currentTransform = currentTransform.parent;

                SDFStyleProvider styleProvider = currentTransform.GetComponent<SDFStyleProvider>();
                _parent = styleProvider;
            }

            if (_parent != null)
            {
                _parent.Register(this);
            }
        }

        private void OnValidate()
        {
            // TODO: change this, get style only for current gameobject, not invoke change on everyone
            _parent.NotifyOnStyleChanged();
        }

        private void OnDisable()
        {
            if (TryGetComponent<SDFImage>(out var sdfImage)) {
                sdfImage.Style = null;
            }
        }

        public void NotifyOnStyleChanged(SDFStyle newStyle)
        {
            if (TryGetComponent<SDFImage>(out var sdfImage))
            {
                sdfImage.Style = newStyle;
            }
        }
    }
}