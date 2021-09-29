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
        public event Action OnClosing;

        private BeamableVisualElement _contentElement;
        private VisualElement _windowRoot;
        private VisualElement _container;

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
            newWindowPosition.x -= newWindowSize.x / 2;

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
        public static BeamablePopupWindow ShowDropdown(string title, Rect sourceRect, Vector2 size,
            BeamableVisualElement content)
        {
            var wnd = CreateInstance<BeamablePopupWindow>();
            wnd.titleContent = new GUIContent(title);
            wnd._contentElement = content;
            wnd.ShowAsDropDown(sourceRect, size);
            wnd.GetRootVisualContainer().AddToClassList("fill-popup-window");

            wnd.Refresh();

            return wnd;
        }

        public static BeamablePopupWindow ShowUtility(string title, BeamableVisualElement content, EditorWindow parent, Vector2 defaultSize)
        {
            var wnd = CreateInstance<BeamablePopupWindow>();
            wnd.titleContent = new GUIContent(title);
            wnd._contentElement = content;

            wnd.ShowUtility();
            if (parent != null)
            {
                wnd.position = GetCenteredScreenRectForWindow(parent, defaultSize);
            }

            // TODO: Somehow position the utility based on the parent view.
            wnd.Refresh();
            wnd.GetRootVisualContainer().AddToClassList("fill-popup-window");
            return wnd;
        }


        private void OnEnable()
        {
            VisualElement root = this.GetRootVisualContainer();
            var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{BeamableComponentsConstants.COMP_PATH}/BeamablePopupWindow/beamablePopupWindow.uxml");
            _windowRoot = uiAsset.CloneTree();
            this.GetRootVisualContainer()
                .AddStyleSheet($"{BeamableComponentsConstants.COMP_PATH}/BeamablePopupWindow/beamablePopupWindow.uss");
            _windowRoot.name = nameof(_windowRoot);

            root.Add(_windowRoot);
        }

        public void SwapContent(BeamableVisualElement other)
        {
            _contentElement = other;
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
            _container.Add(_contentElement);
            _contentElement.Refresh();
            Repaint();
        }
    }
}