using System;
using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Editor.Content.Models;
using Beamable.Editor.UI.Buss;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
    public class SearchBarVisualElement : BeamableVisualElement
    {
        private TextField _textField;
        private double _lastChangedTime;
        private bool _pendingChange;
        public string Value => _textField.value;
        public event Action<string> OnSearchChanged;
        public new class UxmlFactory : UxmlFactory<SearchBarVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
                {name = "custom-text", defaultValue = "nada"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var self = ve as SearchBarVisualElement;
            }
        }

        public SearchBarVisualElement() :base($"{BeamableComponentsConstants.UI_PACKAGE_PATH}/Common/Components/{nameof(SearchBarVisualElement)}/{nameof(SearchBarVisualElement)}")
        {
            Refresh();
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                EditorApplication.update += OnEditorUpdate;
            });
        }

        public override void OnDetach()
        {
            base.OnDetach();
            EditorApplication.update -= OnEditorUpdate;
        }

        public override void Refresh()
        {
            base.Refresh();
            _textField = Root.Q<TextField>();
            _textField.RegisterValueChangedCallback(Textfield_ValueChanged);

        }

        private void OnEditorUpdate()
        {
            if (_pendingChange && EditorApplication.timeSinceStartup > _lastChangedTime + .25f)
            {
                _pendingChange = false;
                OnSearchChanged?.Invoke(_textField.value);

            }
        }

        private void Textfield_ValueChanged(ChangeEvent<string> evt)
        {
           _pendingChange = true;
           _lastChangedTime = EditorApplication.timeSinceStartup;

        }

        public void SetValueWithoutNotify(string value)
        {
            _textField.SetValueWithoutNotify(value);
        }

        public void DoFocus()
        {
            _textField.BeamableFocus();
        }
    }
}