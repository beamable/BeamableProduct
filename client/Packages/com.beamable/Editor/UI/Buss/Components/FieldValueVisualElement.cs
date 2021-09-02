
using System;
using System.Linq;
using Beamable.Editor.UI.Buss.Model;
using Beamable.UI.Buss;
using TMPro;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
   public class FieldValueModel
   {
      public Type FieldType;
      public Func<object> GetValue;
      public Action<object> Set;
      public RangeAttribute Range;
      public bool Readonly;
   }

   public class FieldValueVisualElement : BeamableVisualElement
   {
      public FieldValueModel Model { get; }
      public const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/fieldValueVisualElement";

      private VisualElement _container;

      public FieldValueVisualElement(FieldValueModel model) : base (COMMON)
      {
         Model = model;
      }

      public override void Refresh()
      {
         base.Refresh();
         _container = Root.Q<VisualElement>("container");


         var propType = Model.FieldType;

         if (typeof(Color).IsAssignableFrom(propType))
         {
            var colorField = new ColorField();
            _container.Add(colorField);
            Handle(colorField);
         }else if (typeof(long).IsAssignableFrom(propType))
         {
            var numberField = new LongField();
            _container.Add(numberField);
            Handle(numberField);
         }
         else if (typeof(Texture2D).IsAssignableFrom(propType))
         {
            var objectField = new ObjectField();
            objectField.allowSceneObjects = false;
            objectField.objectType = typeof(Texture2D);
            _container.Add(objectField);
            Handle(objectField);
         }
         else if (typeof(Sprite).IsAssignableFrom(propType))
         {
            var objectField = new ObjectField();
            objectField.allowSceneObjects = false;
            objectField.objectType = typeof(Sprite);
            _container.Add(objectField);
            Handle(objectField);
         }
         else if (typeof(TMP_FontAsset).IsAssignableFrom(propType))
         {
            var objectField = new ObjectField();
            objectField.allowSceneObjects = false;
            objectField.objectType = typeof(TMP_FontAsset);
            _container.Add(objectField);
            Handle(objectField);
         }
         else if (typeof(Material).IsAssignableFrom(propType))
         {
            var objectField = new ObjectField();
            objectField.allowSceneObjects = false;
            objectField.objectType = typeof(Material);
            _container.Add(objectField);
            Handle(objectField);
         }
         else if (typeof(Vector2).IsAssignableFrom(propType))
         {
            var vec2Field = new Vector2Field();
            _container.Add(vec2Field);
            Handle(vec2Field);
         }
         // REMOVED FOR WINDOWS COMPAT
//         else if (typeof(_HorizontalAlignmentOptions).IsAssignableFrom(propType))
//         {
//            var enumField = new EnumField();
//            enumField.Init((_HorizontalAlignmentOptions)Model.GetValue());
//            _container.Add(enumField);
//            Handle(enumField);
//         }
//         else if (typeof(_VerticalAlignmentOptions).IsAssignableFrom(propType))
//         {
//            var enumField = new EnumField();
//            enumField.Init((_VerticalAlignmentOptions)Model.GetValue());
//            _container.Add(enumField);
//            Handle(enumField);
//         }
         else if (typeof(FontStyles).IsAssignableFrom(propType))
         {
            var enumField = new EnumField();
            enumField.Init((FontStyles)Model.GetValue());
            _container.Add(enumField);
            Handle(enumField); // TODO add support for multi-style. Bold AND italic.
         }
         else if (typeof(float).IsAssignableFrom(propType))
         {
            // is slider?
            var rangeAttribute = Model.Range;
            if (rangeAttribute == null)
            {
               var floatField = new FloatField();
               _container.Add(floatField);
               Handle(floatField);
            }
            else
            {
               var slider = new Slider(rangeAttribute.min, rangeAttribute.max);
               _container.Add(slider);
               Handle(slider);
            }

         }
         else
         {
            _container.Add(new Label("unsupported edit type"));
         }
         _container.SetEnabled(!Model.Readonly);

      }

      public void Handle<TData>(INotifyValueChanged<TData> element)
      {
         element.value = (TData) Model.GetValue();
         element.RegisterValueChangedCallback(evt =>
         {
            Model.Set(evt.newValue);
         });
      }

   }
}