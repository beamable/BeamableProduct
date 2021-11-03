using System;
using UnityEngine;

namespace Beamable.UI.BUSS
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private BUSSStyleDescription _inlineStyle;

        public BUSSStyle Style { get; } = new BUSSStyle();

        private BUSSStyleProvider _styleProvider;

        public string Id => _id;
        public BUSSStyleDescription InlineStyle => _inlineStyle;

        public virtual void ApplyStyle()
        {
            // TODO: common style implementation for BUSS Elements, so: applying all properties that affect RectTransform
        }

        private void OnDisable()
        {
            ApplyStyle();
        }

        private void OnValidate()
        {
            _styleProvider = GetComponent<BUSSStyleProvider>();
            _styleProvider?.OnStyleChanged();
        }
    }
}