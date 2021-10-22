using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.SDF
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class BUSSElement : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private SDFStyleScriptableObject _styleObject;
        [SerializeField] private string _id;
#pragma warning restore CS0649

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                Refresh();
            }
        }

        #region Classes

        [SerializeField] private List<string> _classes = new List<string>();

        private IReadOnlyList<string> _readOnlyClasses;

        public IEnumerable<string> Classes => _readOnlyClasses ?? (_readOnlyClasses = _classes.AsReadOnly());

        public bool HasClass(string className)
        {
            return _classes.Contains(className);
        }

        public void AddClass(string className)
        {
            if (!_classes.Contains(className))
            {
                _classes.Add(className);
                Refresh();
            }
        }

        public void RemoveClass(string className)
        {
            _classes.Remove(className);
            Refresh();
        }

        #endregion

        [SerializeField, HideInInspector] private BUSSElement _parent;
        [SerializeField, HideInInspector] private List<BUSSElement> _children = new List<BUSSElement>();

        private void OnEnable()
        {
            if (_styleObject != null)
            {
                _styleObject.OnUpdate = Refresh;
            }
        }

        private void Refresh()
        {
            if (TryGetComponent<SDFImage>(out var sdfImage))
            {
                if (_styleObject != null)
                {
                    sdfImage.Style = _styleObject.GetStyle();
                }
            }
        }
    }
}