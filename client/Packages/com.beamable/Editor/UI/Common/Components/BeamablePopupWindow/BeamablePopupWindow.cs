using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss.Components;
using Beamable.UI.Buss;
using UnityEditor;
using UnityEngine;
using Beamable.Editor.Content.Components;
using Beamable.Editor.Content;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
   public class BeamablePopupWindow : EditorWindow
   {
      public static Vector2 ConfirmationPopupSize = new Vector2(300, 150);

      /// <summary>
      /// Create screen-relative, parent <see cref="VisualElement"/>-relative
      /// <see cref="Rect"/> for new <see cref="BeamablePopupWindow"/>
      /// </summary>
      /// <param name="visualElementBounds"></param>
      public static Rect GetLowerLeftOfBounds(Rect visualElementBounds)
      {
         var newWindowPosition = new Vector2(visualElementBounds.xMin, visualElementBounds.yMax);
         newWindowPosition = GUIUtility.GUIToScreenPoint(newWindowPosition);
         return new Rect(newWindowPosition.x, newWindowPosition.y, 0, 0);
      }
      public static Rect GetLowerRightOfBounds(Rect visualElementBounds)
      {
         var newWindowPosition = new Vector2(visualElementBounds.xMax, visualElementBounds.yMax);
         newWindowPosition = GUIUtility.GUIToScreenPoint(newWindowPosition);
         return new Rect(newWindowPosition.x, newWindowPosition.y, 0, 0);
      }


      /// <summary>
      /// Create CENTERED screen-relative, parent <see cref="VisualElement"/>-relative
      /// <see cref="Rect"/> for new <see cref="ConfirmationPopupVisualElement"/>
      /// </summary>
      /// <param name="visualElementBounds"></param>
      public static Rect GetCenteredScreenRectFromWorldBounds(Rect visualElementBounds, Vector2 newWindowSize)
      {
         //Get relative position
         //TODO: Make this truely sit in the dead center of the window - WIP - srivello
         var newWindowPosition = new Vector2(visualElementBounds.center.x, 0);
         newWindowPosition = GUIUtility.GUIToScreenPoint(newWindowPosition);

         //Adjust by absolute size
         newWindowPosition.x -= newWindowSize.x/2;

         return new Rect(newWindowPosition.x, newWindowPosition.y, newWindowSize.x, newWindowSize.y);
      }

      /// <summary>
      /// Create a Centered screen-relative rectangle, given a parent editor window
      /// </summary>
      /// <param name="window"></param>
      public static Rect GetCenteredScreenRectForWindow(EditorWindow window, Vector2 size)
      {
         var pt = window.position.center;

         var halfSize = size * .5f;
         return new Rect(pt.x - halfSize.x, pt.y - halfSize.y, size.x, size.y);
      }

      /// <summary>
      /// Create new popup with contents of any <see cref="BeamableVisualElement"/>
      /// </summary>
      /// <param name="title"></param>
      /// <param name="sourceRect"></param>
      /// <param name="size"></param>
      /// <param name="content"></param>
      /// <returns></returns>
      public static BeamablePopupWindow ShowDropdown(string title, Rect sourceRect, Vector2 size, BeamableVisualElement content)
      {
         var wnd = CreateInstance<BeamablePopupWindow>();
         wnd.titleContent = new GUIContent(title);
         wnd.ContentElement = content;
         wnd.ShowAsDropDown(sourceRect, size);
         wnd.GetRootVisualContainer().AddToClassList("fill-popup-window");

         wnd.Refresh();

         return wnd;
      }

      /// <summary>
      /// Create new popup with contents of <see cref="ConfirmationPopupVisualElement"/>.
      /// Useful for an "Are you Sure?" user experience.
      /// </summary>
      /// <param name="window">Element that we want to center against</param>
      /// <param name="windowHeader">Window header content</param>
      /// <param name="contentText">Window body content</param>
      /// <param name="onConfirm">Optional: Action to call on confirm button clicked</param>
      /// <param name="onCancel">Optional: Action to call on cancel button clicked</param>
      /// <returns></returns>
      public static void ShowConfirmationPopup<T>(T window, string windowHeader, string contentText,
         Action onConfirm = null, Action onCancel = null) where T : EditorWindow
      {
         var popupWindowRect = GetCenteredScreenRectForWindow(window, ConfirmationPopupSize);
         var confirmationPopupVisualElement = new ConfirmationPopupVisualElement(windowHeader, contentText);

         if(onCancel != null)
         {
            confirmationPopupVisualElement.OnCancelButtonClicked += onCancel.Invoke;
         }

         if(onConfirm != null)
         {
            confirmationPopupVisualElement.OnOKButtonClicked += onConfirm.Invoke;
         }
#if UNITY_2020_1_OR_NEWER
         var wnd = CreateInstance<BeamablePopupWindow>();
         wnd.titleContent = new GUIContent("");
         wnd.ContentElement = confirmationPopupVisualElement;
         wnd.GetRootVisualContainer().AddToClassList("fill-popup-window");
         wnd.Refresh();
         confirmationPopupVisualElement.OnCancelButtonClicked += wnd.Close;
         confirmationPopupVisualElement.OnOKButtonClicked += wnd.Close;

         EditorApplication.delayCall += () =>
         {
            wnd.position = new Rect(popupWindowRect.x, popupWindowRect.y, popupWindowRect.size.x, popupWindowRect.size.y);
            wnd.ShowModalUtility();
         };

#else
         var wnd = BeamablePopupWindow.ShowDropdown(windowHeader, popupWindowRect, popupWindowRect.size, confirmationPopupVisualElement);
         confirmationPopupVisualElement.OnCancelButtonClicked += wnd.Close;
         confirmationPopupVisualElement.OnOKButtonClicked += wnd.Close;
         var newPos = BeamablePopupWindow.GetCenteredScreenRectForWindow(window, popupWindowRect.size);
         wnd.position = newPos;
#endif
      }

      public static BeamablePopupWindow ShowUtility(string title, BeamableVisualElement content, EditorWindow parent)
      {
         var wnd = CreateInstance<BeamablePopupWindow>();
         wnd.titleContent = new GUIContent(title);
         wnd.ContentElement = content;

         wnd.ShowUtility();
         if (parent != null)
         {
            wnd.position = GetCenteredScreenRectForWindow(parent, ContentManagerConstants.WindowSizeMinimum);
         }

         // TODO: Somehow position the utility based on the parent view.
         wnd.Refresh();
         wnd.GetRootVisualContainer().AddToClassList("fill-popup-window");
         return wnd;
      }

      public BeamableVisualElement ContentElement;
      private VisualElement _windowRoot;
      private VisualElement _contentRoot;
      private VisualElement _container;

      public event Action OnClosing;

      private void OnEnable()
      {
         VisualElement root = this.GetRootVisualContainer();
         var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BeamableComponentsConstants.COMP_PATH}/BeamablePopupWindow/beamablePopupWindow.uxml");
         _windowRoot = uiAsset.CloneTree();
         this.GetRootVisualContainer().AddStyleSheet($"{BeamableComponentsConstants.COMP_PATH}/BeamablePopupWindow/beamablePopupWindow.uss");
         _windowRoot.name = nameof(_windowRoot);

         root.Add(_windowRoot);
      }

      public void SwapContent(BeamableVisualElement other)
      {
         ContentElement = other;
         Refresh();

      }

      private void OnDestroy()
      {
         OnClosing?.Invoke();
      }

      public void Refresh()
      {
         _container = _windowRoot.Q<VisualElement>("container");
         _container.Clear();
         _container.Add(ContentElement);
         ContentElement.Refresh();
         Repaint();
      }
   }
}