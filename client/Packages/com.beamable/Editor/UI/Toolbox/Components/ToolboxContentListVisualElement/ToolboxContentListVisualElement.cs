using Beamable.Editor.Content.Models;
using Beamable.Editor.Content;
using UnityEngine;
using Beamable.Editor.UI.Buss.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.Content.Components;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Toolbox.Components
{
    public class ToolboxContentListVisualElement : ToolboxComponent
    {
        private VisualElement _gridContainer;
        private IWidgetSource _widgetSource;
        private ScrollView _scrollView;


        public new class UxmlFactory : UxmlFactory<ToolboxContentListVisualElement, UxmlTraits>
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
                var self = ve as ToolboxContentListVisualElement;
            }
        }

        public ToolboxModel Model { get; set; }
        private int ExtraElementCount = 0;
        private int TotalWidgets = 0;
        private List<VisualElement> _extraElements = new List<VisualElement>();

        public ToolboxContentListVisualElement() : base(nameof(ToolboxContentListVisualElement))
        {
        }


        public override void Refresh()
        {
            base.Refresh();

            _gridContainer = Root.Q("gridContainer");

            Model.OnWidgetSourceChanged += Model_OnWidgetSourceAvailable;
            Model.OnQueryChanged += Model_OnQueryChanged;
            RefreshWidgetElements(_gridContainer);
            EditorApplication.update -= CheckForExtraElements;
            EditorApplication.update += CheckForExtraElements;

        }

        public void CheckForExtraElements()
        {
            var totalWidth = this.layout.width;
            var elementSize = 220; // TODO: Find this number programmaticaly
            var totalElements = TotalWidgets;


            var elementsPerRow = (int) (totalWidth / elementSize);
            var completedRows = (int) (totalElements / elementsPerRow);
            var correctElements = completedRows * elementsPerRow;
            var leftOverElements = totalElements - correctElements;
            var extraElements = (elementsPerRow - leftOverElements) % elementsPerRow;

            extraElements = Mathf.Min(extraElements, totalElements); // a sane upper limit, so we don't accidently create thousands of elements on page load.
            if (ExtraElementCount == extraElements) return;
            ExtraElementCount = extraElements;

            SetExtraElements();
        }

        private void Model_OnQueryChanged()
        {
            RefreshWidgetElements(_gridContainer);
        }

        private void Model_OnWidgetSourceAvailable(IWidgetSource source)
        {
            if (_widgetSource == source) return; // nothing to do; no change.

            _widgetSource = source;
            RefreshWidgetElements(_gridContainer);
        }



        private void RefreshWidgetElements(VisualElement gridRoot, string filter = null)
        {
            gridRoot.Clear();

            TotalWidgets = 0;
            foreach (var widget in Model.GetFilteredWidgets())
            {
                var widgetElement = new ToolboxFeatureVisualElement();
                widgetElement.WidgetModel = widget;
                widgetElement.Refresh();
                gridRoot.Add(widgetElement);
                TotalWidgets++;
            }
            SetExtraElements();

        }

        private void SetExtraElements()
        {
            foreach (var element in _extraElements)
            {
                element.parent?.Remove(element);
            }

            _extraElements.Clear();

            for (var i = 0; i < ExtraElementCount; i++)
            {
                var widgetElement = new ToolboxFeatureVisualElement();
                widgetElement.AddToClassList("invisible");
                widgetElement.Refresh();
                _extraElements.Add(widgetElement);
                _gridContainer.Add(widgetElement);
            }
        }
    }
}