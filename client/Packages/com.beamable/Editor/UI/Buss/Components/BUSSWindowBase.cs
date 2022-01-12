using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
	public abstract class BUSSWindowBase<TWindow, TVisualElement> : EditorWindow
		where TWindow : BUSSWindowBase<TWindow, TVisualElement>
		where TVisualElement : BeamableVisualElement
	{
		private static BUSSWindowBase<TWindow, TVisualElement> Instance { get; set; }

		private static bool IsAlreadyOpened => Instance != null;

		public static TWindow ShowWindow()
		{
			if (IsAlreadyOpened)
				return null;
			
			var wnd = CreateInstance<TWindow>();
			wnd.ShowUtility();
			return wnd;
		}

		public static void CloseWindow()
		{
			if (!IsAlreadyOpened)
				return;
			
			Instance.Close();
		}

		private void OnEnable() => Instance = this;
		private void OnDisable() => Instance = null;
		protected void Refresh()
		{
			var rootContainer = this.GetRootVisualContainer();
			rootContainer.Clear();

			var visualElement = GetVisualElement();
			rootContainer.Add(visualElement);
			visualElement.Refresh();

			Repaint();
		}
		
		protected abstract TVisualElement GetVisualElement();
	}
}
