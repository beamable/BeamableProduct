using System.Collections.Generic;
using System;
using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class DependentServicesWindow : EditorWindow
    {
        public static DependentServicesWindow ShowWindow()
        {
            var wnd = CreateInstance<DependentServicesWindow>();
            
            wnd.titleContent = new GUIContent(Constants.DEPENDENT_SERVICES_WINDOW_TITLE);
            wnd.ShowUtility();
            wnd.minSize = new Vector2(620, 400);
            wnd.maxSize = wnd.minSize;
            wnd.position = new Rect((Screen.width + wnd.minSize.x) * 0.5f, Screen.width * 0.5f, wnd.minSize.x, wnd.minSize.y);
            wnd.Refresh();

            return wnd;
        }
        private void Refresh()
        {
            var container = this.GetRootVisualContainer();
            container.Clear();

            var dependentServicesPopup = new DependentServicesPopup();

            dependentServicesPopup.OnConfirm += () =>
            {
                dependentServicesPopup.SetServiceDependencies();
                Close();
            };
            dependentServicesPopup.OnClose += () =>
            {
                if (!dependentServicesPopup.IsAnyRelationChanged)
                {
                    Close();
                    return;
                }

                var choice = EditorUtility.DisplayDialogComplex( 
                    title: "Dependencies Have Been Modified",
                    message: "Do you want to apply the changes you made before quitting?",
                    ok: "Apply",
                    cancel: "Cancel",
                    alt: "Don't Apply");
                
                switch (choice)
                {
                    case 0:
                        dependentServicesPopup.SetServiceDependencies();
                        Close();
                        break;
                    case 1:
                        break;
                    case 2:
                        Close();
                        break;
                }
            };
            
            container.Add(dependentServicesPopup);
            dependentServicesPopup.PrepareParent();
            dependentServicesPopup.Refresh();
            Repaint();
        }
    }
}