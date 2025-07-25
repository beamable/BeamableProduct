#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

#if UNITY_2022_1_OR_NEWER
using System.Linq;
#endif

namespace Beamable.Editor.ToolbarExtender
{
	public static class BeamableToolbarCallbacks
	{
		public static readonly Type m_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
		static Type m_guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
#if UNITY_2020_1_OR_NEWER
        static Type m_iWindowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");

        static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#else
		static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
		static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

#if UNITY_2021_2_OR_NEWER
		static MethodInfo m_SendEventToIMGUI = typeof(IMGUIContainer).GetMethod("SendEventToIMGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif

		static ScriptableObject m_currentToolbar;

		/// <summary>
		/// Callback for toolbar OnGUI method.
		/// </summary>
		public static Action<IMGUIContainer> OnToolbarGUI;

		private static IMGUIContainer _container;

		static BeamableToolbarCallbacks()
		{
			EditorApplication.update += OnUpdate;
		}

		static void OnUpdate()
		{
			if (!BeamEditor.IsInitialized) return;
			
			// Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
			if (m_currentToolbar == null)
			{
				// Find toolbar
				var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
				m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
				if (m_currentToolbar != null)
				{
#if UNITY_2021_2_OR_NEWER
					var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
					var rawRoot = root.GetValue(m_currentToolbar);
					var mRoot = rawRoot as VisualElement;
					
					// This IMGUI Container is what draws the toolbar extension
					_container = new IMGUIContainer();
					_container.onGUIHandler += OnGUI;
					_container.focusable = false;
					_container.style.flexGrow = 0;
					_container.style.marginRight = 2;
					
					// In Unity 2021.3 LTS, the IMGUIContainer is not capturing events by default when placed inside the toolbar. This is likely to do with how the
					// internals of the Toolbar component have changed from Unity 2020. To solve this, we manually forward the event from any clicks of the toolbar into
					// the IMGUI Container via reflection (method name found via Rider's Disassembly).
					// The last parameter of the reflection call is false as attempting to verify the bounds of the click into the IMGUIContainer fails due to a misconfiguration of the
					// container's capturing PointerIds. Calling the CapturePointer extension method in the IMGUIContainer does NOT work. As such, the solution is to assume that any click on the
					// toolbar visual element is also a click on the IMGUIContainer. Since clicks only matter inside GUI.Button calls and other similar things that do their own bounds checks inside
					// the IMGUIContainer, this is mostly ok.
					mRoot?.RegisterCallback<MouseDownEvent>(evt =>
					{
#if UNITY_2022_1_OR_NEWER
						_ = (bool) m_SendEventToIMGUI.Invoke(_container, new object[]{evt, true, false});
#else
						_container.HandleEvent(evt);
#endif
						
					});
					_container.RegisterCallback<MouseDownEvent>(evt =>
					{
						_ = (bool) m_SendEventToIMGUI.Invoke(_container, new object[]{evt, true, false});
					});

#if UNITY_2020_1_OR_NEWER
                    var windowBackend = m_windowBackend.GetValue(m_currentToolbar);

                    // Get it's visual tree
                    var visualTree = (VisualElement)m_viewVisualTree.GetValue(windowBackend, null);
#else
					// Get it's visual tree
					var visualTree = (VisualElement)m_viewVisualTree.GetValue(m_currentToolbar, null);
#endif

					// I found this ID by using the UIToolkit debugger.
					const string UnityToolbarRightAreaId = "ToolbarZoneRightAlign";
					var rightAlign = visualTree.Q<VisualElement>(UnityToolbarRightAreaId);
				
					// add some classes to inherit the styles of the toolbar.
					_container.AddToClassList("unity-editor-toolbar-element");
					_container.AddToClassList("unity-toolbar-button");
					rightAlign.Add(_container);
					
					var handler = (Action)m_imguiContainerOnGui.GetValue(_container);
					handler -= OnGUI;
					handler += OnGUI;
					m_imguiContainerOnGui.SetValue(_container, handler);
#endif
				}
			}
		}


		static void OnGUI()
		{
			var handler = OnToolbarGUI;
			if (handler != null)
			{
				try
				{
					handler(_container);
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
		}
	}
}
#endif
