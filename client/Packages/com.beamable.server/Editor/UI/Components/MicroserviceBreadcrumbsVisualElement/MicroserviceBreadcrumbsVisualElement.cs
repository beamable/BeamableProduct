using Beamable.Editor.Content.Models;
using Beamable.Editor.Content;
using UnityEngine;
using Beamable.Editor.UI.Buss.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.Config;
using Beamable.Editor.Content.Components;
using Beamable.Editor.Environment;
using Beamable.Editor.Modules.Theme;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Components;
using UnityEditor;
using Debug = UnityEngine.Debug;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class MicroserviceBreadcrumbsVisualElement : MicroserviceComponent
    {
        public new class UxmlFactory : UxmlFactory<MicroserviceBreadcrumbsVisualElement, UxmlTraits>
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
                var self = ve as MicroserviceBreadcrumbsVisualElement;

            }
        }

        public event Action<bool> OnSelectAllCheckboxChanged;
        
        private RealmButtonVisualElement _realmButton;
        private BeamableCheckboxVisualElement _checkbox;

        public MicroserviceBreadcrumbsVisualElement() : base(nameof(MicroserviceBreadcrumbsVisualElement))
        {
        }

        public override void Refresh()
        {
            base.Refresh();
            _realmButton = Root.Q<RealmButtonVisualElement>("realmButton");
            _realmButton.Refresh();

            _checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            _checkbox.Refresh();
            _checkbox.OnValueChanged += b => OnSelectAllCheckboxChanged?.Invoke(b);
        }

        public void SetSelectAllCheckboxValue(bool value)
        {
            _checkbox.SetWithoutNotify(value);
        }
    }
    
}