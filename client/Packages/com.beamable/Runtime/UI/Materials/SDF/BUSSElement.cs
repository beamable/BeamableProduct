using System;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private string _id;
#pragma warning restore CS0649

        private BUSSStyleProvider _styleProvider;

        public string Id => _id;

        public virtual void ApplyStyle(BUSSStyle newStyle)
        {

        }

        private void OnDisable()
        {
            ApplyStyle(null);
        }

        private void OnValidate()
        {
            _styleProvider = GetComponent<BUSSStyleProvider>();
            _styleProvider?.OnStyleChanged();
        }
    }
}