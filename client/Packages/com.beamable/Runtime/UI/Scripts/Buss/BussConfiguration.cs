using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Beamable.UI.Buss // TODO: rename it to Beamable.UI.BUSS - new system's namespace
{
    public class BussConfiguration : ModuleConfigurationObject
    {
        // New system
        public static BussConfiguration Instance => Get<BussConfiguration>();
        [SerializeField] private BussStyleSheet globalStyleSheet = null;

        // TODO: serialized only for debug purposes. Remove before final push
        [SerializeField] private List<BussElement> _rootBussElements = new List<BussElement>();

#if UNITY_EDITOR
	    static BussConfiguration()
	    {
		    // temporary solution to refresh the list of BussElements on scene change
		    EditorSceneManager.sceneOpened += (scene, mode) => Instance.RefreshBussElements();
		    EditorSceneManager.sceneClosed += scene => Instance.RefreshBussElements();
	    }

	    void RefreshBussElements()
	    {
		    _rootBussElements.Clear();
		    foreach (BussElement element in FindObjectsOfType<BussElement>())
		    {
			    element.CheckParent();
		    }
		    EditorUtility.SetDirty(this);
	    }
#endif

        public void RegisterObserver(BussElement bussElement)
        {
            // TODO: serve case when user adds (by Add Component opiton, not by changing hierarchy) BUSSStyleProvider
            // component somewhere "above" currently topmost BUSSStyleProvider(s) causing to change whole hierarchy 
 
            if (!_rootBussElements.Contains(bussElement))
            {
                _rootBussElements.Add(bussElement);
            }
        }

        public void UnregisterObserver(BussElement bussElement)
        {
            _rootBussElements.Remove(bussElement);
        }

        public void UpdateStyleSheet(BussStyleSheet styleSheet) {
            // this should happen only in editor
            if (styleSheet == null) return;
            if (styleSheet == globalStyleSheet) {
                foreach (var bussElement in _rootBussElements) {
                    bussElement.OnStyleChanged();
                }
            }
            else {
                foreach (var bussElement in _rootBussElements) {
                    OnStyleSheetChanged(bussElement, styleSheet);
                }
            }
        }

        private void OnStyleSheetChanged(BussElement element, BussStyleSheet styleSheet)
        {
	        if (element == null) return;
            if (element.StyleSheet == styleSheet) {
                element.OnStyleChanged();
            }
            else {
                foreach (var child in element.Children) {
                    OnStyleSheetChanged(child, styleSheet);
                }
            }
        }

        private void OnDestroy()
        {
            if (globalStyleSheet != null)
            {
                globalStyleSheet.OnChange -= OnGlobalStyleChanged;
            }
        }
        
        private void OnDisable()
        {
            if (globalStyleSheet != null)
            {
                globalStyleSheet.OnChange -= OnGlobalStyleChanged;
            }
        }

        private void OnGlobalStyleChanged()
        {
            foreach (var bussElement in _rootBussElements)
            {
                bussElement.OnStyleChanged();
            }
        }

        // TODO: in future move to some styles repository class which responsibility will be caching styles and recalculate them
        #region Styles parsing

        public void RecalculateStyle(BussElement element) {
            element.Style.Clear();
            element.PseudoStyles.Clear();

            if (globalStyleSheet != null) {
                ApplyStyleSheet(element, globalStyleSheet);
            }

            foreach (var styleSheet in element.AllStyleSheets) {
                if (styleSheet != null) {
                    ApplyStyleSheet(element, styleSheet);
                }
            }
            
            ApplyDescriptor(element, element.InlineStyle);
            
            element.ApplyStyle();
        }

        public static void ApplyStyleSheet(BussElement element, BussStyleSheet sheet) {
            if (element == null || sheet == null) return;
            foreach (var descriptor in sheet.Styles) {
                if (descriptor.Selector?.CheckMatch(element) ?? false) {
                    ApplyDescriptor(element, descriptor);
                }
            }
        }

        public static void ApplyDescriptor(BussElement element, BussStyleDescription descriptor) {
            if (element == null || descriptor == null) return;
            foreach (var property in descriptor.Properties) {
                element.Style[property.Key] = property.GetProperty();
            }
        }

        #endregion
    }
}
