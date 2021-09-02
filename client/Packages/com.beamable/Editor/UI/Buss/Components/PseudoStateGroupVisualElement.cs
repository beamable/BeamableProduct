
using System;
using Beamable.UI.Buss;
using Beamable.Editor.UI.Buss.Model;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.UI.Buss.Components
{
   public class PseudoStateGroupVisualElement : BeamableVisualElement
   {
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/pseudoStateGroupVisualElement";

      public Action OnStateChanged;

      private StyleBehaviour _model;

      private string[] _states = new[] {"hover", "active"};

      private VisualElement _container;

      public PseudoStateGroupVisualElement(StyleBehaviour model) : base(COMMON)
      {
         _model = model;
      }

      public override void Refresh()
      {
         base.Refresh();

         _container = Root.Q<VisualElement>("container");

         foreach (var state in _states)
         {
            var wrapper = new VisualElement();
            wrapper.AddToClassList("pseudo-state");

            wrapper.Add(new Label(state));
            var toggle = new Toggle();
            toggle.RegisterValueChangedCallback(evt =>
            {
               _model.SetPseudoState(state, evt.newValue);
               _model.ApplyStyleTree();
               OnStateChanged?.Invoke();
            });
            toggle.SetValueWithoutNotify(_model.HasPseudoState(state));
            wrapper.Add(toggle);

            _container.Add(wrapper);
         }
      }
   }
}