#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif
using Beamable.Common;
using Beamable.Editor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.ContentManager;

namespace Beamable.Editor.UI.Components
{
	public class BeamablePopupWindow : EditorWindow, ISerializationCallbackReceiver
	{
		private static BeamablePopupWindow _currentConfirmationWindow;

		public event Action OnClosing;

		private BeamableVisualElement _contentElement;
		private VisualElement _windowRoot;
		private VisualElement _container;

		private Action<BeamablePopupWindow> _onDomainReload;

		[SerializeField]
		private byte[] serializedDomainReloadAction;
		private bool isSet;

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
		/// This method introduces a delayFrame to let later versions of Unity avoid throwing a warning about an unchecked window.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="sourceRect"></param>
		/// <param name="size"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public static async Promise<BeamablePopupWindow> ShowDropdownAsync(string title, Rect sourceRect, Vector2 size,
			BeamableVisualElement content)
		{
			var wnd = CreateInstance<BeamablePopupWindow>();
			var promise = new Promise();
			EditorApplication.delayCall += () =>
			{
				wnd.titleContent = new GUIContent(title);
				wnd._contentElement = content;
				wnd.ShowAsDropDown(sourceRect, size);
				wnd.GetRootVisualContainer().AddToClassList("fill-popup-window");

				wnd.Refresh();
				promise.CompleteSuccess();
			};
			await promise;
			return wnd;
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

		public static BeamablePopupWindow ShowUtility(string title, BeamableVisualElement content, EditorWindow parent,
			Vector2 defaultSize, Action<BeamablePopupWindow> onDomainReload = null)
		{
			var wnd = CreateInstance<BeamablePopupWindow>();
			wnd.titleContent = new GUIContent(title);
			wnd._contentElement = content;
			wnd.minSize = defaultSize;
			wnd._onDomainReload = onDomainReload;

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

		public static BeamablePopupWindow ShowConfirmationUtility(string title, ConfirmationPopupVisualElement element,
																  EditorWindow parent, Action<BeamablePopupWindow> onDomainReload = null)
		{
			var window = ShowUtility(title, element, parent, ConfirmationPopupSize,
				onDomainReload).FitToContent();

			CloseConfirmationWindow();
			_currentConfirmationWindow = window;

			return window;
		}

		public static void CloseConfirmationWindow()
		{
			if (_currentConfirmationWindow != null)
			{
				_currentConfirmationWindow.Close();
			}

			_currentConfirmationWindow = null;
		}

		public BeamablePopupWindow FitToContent()
		{
			EditorApplication.delayCall += () =>
			{
				var newSize = new Vector2(_contentElement.contentRect.width, _contentElement.contentRect.height);

				if (newSize.SqrMagnitude() > 0)
					minSize = maxSize = newSize;
			};
			return this;
		}

		private void OnEnable()
		{
			VisualElement root = this.GetRootVisualContainer();
			var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				$"{Directories.COMMON_COMPONENTS_PATH}/BeamablePopupWindow/beamablePopupWindow.uxml");
			_windowRoot = uiAsset.CloneTree();
			this.GetRootVisualContainer()
				.AddStyleSheet($"{Directories.COMMON_COMPONENTS_PATH}/BeamablePopupWindow/beamablePopupWindow.uss");
			_windowRoot.name = nameof(_windowRoot);

			root.Add(_windowRoot);

			if (isSet)
			{
				EditorApplication.delayCall += () => _onDomainReload?.Invoke(this);
			}
		}

		public void SwapContent(BeamableVisualElement other, Action<BeamablePopupWindow> onDomainReload = null)
		{
			_contentElement = other;
			Refresh();
			this.GetRootVisualContainer().AddToClassList("fill-popup-window");

			if (onDomainReload != null)
				_onDomainReload = onDomainReload;
		}

		private void OnDestroy()
		{
			OnClosing?.Invoke();
			_onDomainReload = null;
		}

		public void Refresh()
		{
			_container = _windowRoot.Q<VisualElement>("container");
			_container.Clear();
			_container.Add(_contentElement);
			_contentElement.Refresh();
			Repaint();
			isSet = true;
		}

		public void OnBeforeSerialize()
		{
			if (_onDomainReload != null)
			{
				BinaryFormatter formatter = new BinaryFormatter();

				using (MemoryStream stream = new MemoryStream())
				{
					formatter.Serialize(stream, (object)_onDomainReload);
					serializedDomainReloadAction = stream.ToArray();
				}
			}
		}

		public void OnAfterDeserialize()
		{
			if (serializedDomainReloadAction != null && serializedDomainReloadAction.Length > 0)
			{
				BinaryFormatter formatter = new BinaryFormatter();

				using (MemoryStream stream = new MemoryStream(serializedDomainReloadAction))
				{
					_onDomainReload = (Action<BeamablePopupWindow>)formatter.Deserialize(stream);
				}
			}
		}
	}
}
