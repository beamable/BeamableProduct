
using System.Collections.Generic;
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
   public class LoadingSpinnerVisualElement : BeamableVisualElement
   {
      public float Size { get; private set; }
      public float HalfSize => Size * .5f;

      public LoadingSpinnerVisualElement() : base($"{BeamableComponentsConstants.UI_PACKAGE_PATH}/Common/Components/{nameof(LoadingSpinnerVisualElement)}/{nameof(LoadingSpinnerVisualElement)}")
      {
      }

      public new class UxmlFactory : UxmlFactory<LoadingSpinnerVisualElement, UxmlTraits> { }
      public new class UxmlTraits : VisualElement.UxmlTraits
      {
         UxmlFloatAttributeDescription size = new UxmlFloatAttributeDescription() { name = "size", defaultValue = 20 };

         public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
         {
            get { yield break; }
         }
         public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
         {
            base.Init(ve, bag, cc);
            var self = ve as LoadingSpinnerVisualElement;
            self.Size = size.GetValueFromBag(bag, cc);


            self.Refresh();
         }
      }

      public float Angle;
      public float Speed = .7f;

      private double _lastTime;
      private TemplateContainer _spin;

      public override void Refresh()
      {
         base.Refresh();

         _spin = Root.Q<TemplateContainer>();

         _spin.style.width = Size;
         _spin.style.height = Size;
         style.width = Size;
         style.height = Size;

         EditorApplication.update += Update;
      }

      void Update()
      {

         var time = EditorApplication.timeSinceStartup;
         var diff = (float)(time - _lastTime);
         _lastTime = time;

         Quaternion rot = Quaternion.Euler(0, 0, Speed);
         var targetPos = new Vector3(HalfSize, HalfSize, 0);

         transform.position = rot * (transform.position - targetPos) + targetPos;
         transform.rotation = (rot * transform.rotation).normalized;

         #if UNITY_2018
         parent.clippingOptions = ClippingOptions.ClipAndCacheContents;
         #endif
         Angle += diff * Speed;
      }

   }
}