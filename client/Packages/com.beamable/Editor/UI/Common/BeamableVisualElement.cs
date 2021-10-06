using System;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss
{
   public class BeamableVisualElement : VisualElement
   {
      protected VisualTreeAsset TreeAsset { get; private set; }
      protected VisualElement Root { get; private set; }

      protected string UXMLPath { get; private set; }

      protected string USSPath { get; private set; }

      public BeamableVisualElement(string commonPath) : this(commonPath + ".uxml", commonPath + ".uss") {}

      public BeamableVisualElement(string uxmlPath, string ussPath)
      {
         UXMLPath = uxmlPath;
         USSPath = ussPath;
         TreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);


         RegisterCallback<DetachFromPanelEvent>(evt =>
         {
            OnDetach();
         });

      }


      public virtual void OnDetach()
      {
         // Do any sort of cleanup
      }

      public void Destroy()
      {
         // call OnDestroy on all child elements.
         foreach (var child in Children())
         {
            if (child is BeamableVisualElement beamableChild)
            {
               beamableChild.Destroy();
            }
         }
         OnDestroy();
      }

      protected virtual void OnDestroy()
      {
         // Unregister any events...
      }

      public virtual void Refresh()
      {
         Destroy();
         Clear();

         Root = TreeAsset.CloneTree();

         this.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
         this.AddStyleSheet(USSPath);
#if UNITY_2018
         var additionalUSS = USSPath.Replace(".uss", ".2018.uss");
         if (UnityEngine.Windows.File.Exists(additionalUSS))
         {
            this.AddStyleSheetPath(additionalUSS);
         }
#endif
         Add(Root);

         Root?.Query<VisualElement>(className: "--image-scale-to-fit").ForEach(elem =>
         {
            elem?.SetBackgroundScaleModeToFit();
         });
      }
   }
}