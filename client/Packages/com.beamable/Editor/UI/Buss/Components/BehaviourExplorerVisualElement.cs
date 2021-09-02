
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Beamable.UI.Buss;
using Beamable.Editor.UI.Buss.Model;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
   public class BehaviourExplorerVisualElement : BeamableVisualElement
   {
      private const string COMMON = BeamableComponentsConstants.UI_PACKAGE_PATH + "/Buss/Components/behaviourExplorerVisualElement";

      private ScrollView _scrollView;

      private Dictionary<StyleBehaviour, StyleVisualElementData> _styleToData = new Dictionary<StyleBehaviour, StyleVisualElementData>();
      private struct StyleVisualElementData
      {
         public StyleBehaviour StyleBehaviour;
         public VisualElement Header, Footer, Body;

         public void RemoveSelectedClass()
         {
            Header?.RemoveFromClassList("selected");
            Body?.RemoveFromClassList("selected");
            Footer?.RemoveFromClassList("selected");
         }

         public void AddSelectedClass()
         {
            Header?.AddToClassList("selected");
            Body?.AddToClassList("selected");
            Footer?.AddToClassList("selected");
         }
      }

      private StyleBehaviour _lastSelected;

      public BehaviourExplorerVisualElement() : base(COMMON)
      {

      }

      public override void Refresh()
      {
         base.Refresh();

         _styleToData.Clear();

         _scrollView = Root.Q<ScrollView>("container");

         // find all root elements.
         var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            .GetRootGameObjects()
            //.Select(g => g.GetComponent<StyleBehaviour>())
            .Where(c => c != null)
            .ToList();

         foreach (var root in roots)
         {
            var elems = PopulateRoot(root);
            foreach (var elem in elems)
            {
               _scrollView.Add(elem);
            }
         }
      }

      public void SetSelected(StyleBehaviour style)
      {
         if (_lastSelected != null)
         {
            if (_styleToData.TryGetValue(_lastSelected, out var old))
            {
               old.RemoveSelectedClass();
            }
         }

         _lastSelected = style;
         if (_lastSelected != null && _styleToData.TryGetValue(_lastSelected, out var next))
         {
            next.AddSelectedClass();
         }
      }

      List<VisualElement> PopulateRoot(GameObject gob)
      {
         // search over children, looking for StyleObjects.
         var rootNodes = new List<VisualElement>();

         void Search(Transform root)
         {
            for (var i = 0; i < root.childCount; i++)
            {
               var child = root.GetChild(i);
               var childStyle = child.GetComponent<StyleBehaviour>();
               if (childStyle != null)
               {
                  rootNodes.Add(CreateNode(childStyle));
               }
               else
               {
                  Search(child);
               }
            }
         }

         var style = gob.GetComponent<StyleBehaviour>();
         if (style == null)
         {
            Search(gob.transform);
         }
         else
         {
            rootNodes.Add(CreateNode(style));
         }
         return rootNodes;
      }


      VisualElement CreateNode(StyleBehaviour style)
      {

         var elem = new VisualElement();

         var children = style.GetChildren();
         var headerElem = new Label(GetElementOpen(style, children.Count > 0));
         headerElem.AddToClassList("open");

         elem.Add(headerElem);
         var container = new VisualElement();
         container.style.paddingLeft = 20;
         elem.Add(container);

         Label footerElem = null;
         if (children.Count > 0)
         {
            footerElem = new Label(GetElementClose(style));
            footerElem.AddToClassList("close");
            elem.Add(footerElem);
            footerElem.RegisterCallback<MouseDownEvent>(evt => HandleClickOnElement(style, evt));


         }

         PopulateObjects(container, style, children);

         headerElem.RegisterCallback<MouseDownEvent>(evt => HandleClickOnElement(style, evt));

         headerElem.RegisterCallback<MouseOverEvent>(evt => HandleMouseOver(style, evt, headerElem, footerElem));
         headerElem.RegisterCallback<MouseOutEvent>(evt => HandleMouseOut(style, evt, headerElem, footerElem));

         footerElem?.RegisterCallback<MouseOverEvent>(evt => HandleMouseOver(style, evt, headerElem, footerElem));
         footerElem?.RegisterCallback<MouseOutEvent>(evt => HandleMouseOut(style, evt, headerElem, footerElem));

         _styleToData.Add(style, new StyleVisualElementData
         {
            StyleBehaviour = style,
            Footer = footerElem,
            Header = headerElem,
            Body = container
         });
         return elem;
      }

      private void HandleMouseOver(StyleBehaviour style, MouseOverEvent evt, VisualElement header, VisualElement footer)
      {
         header?.AddToClassList("hover");
         footer?.AddToClassList("hover");
      }

      private void HandleMouseOut(StyleBehaviour style, MouseOutEvent evt, VisualElement header, VisualElement footer)
      {
         header?.RemoveFromClassList("hover");
         footer?.RemoveFromClassList("hover");
      }

      private void HandleClickOnElement(StyleBehaviour style, MouseDownEvent evt)
      {
         Selection.SetActiveObjectWithContext(style.gameObject, style);
      }

      void PopulateObjects(VisualElement container, StyleBehaviour root, List<StyleBehaviour> children)
      {
         foreach (var child in children)
         {
            container.Add(CreateNode(child));
         }
      }

      string GetElementOpen(StyleBehaviour style, bool useBody)
      {
         var classStr = style.ClassString.Length > 0 ? $"class=\"{style.ClassString.ToLower()}\"" : "";
         var endTag = useBody ? ">" : "/>";
         return $"<{style.TypeString} id=\"{style.Id}\" {classStr} {endTag}";
      }

      string GetElementClose(StyleBehaviour style)
      {
         return $"</{style.TypeString}>";
      }
   }
}