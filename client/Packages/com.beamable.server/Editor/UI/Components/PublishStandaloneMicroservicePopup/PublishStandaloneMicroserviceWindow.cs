using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.Usam;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public class PublishStandaloneWindow : EditorWindow
	{
		[SerializeField] private bool isSet;
		private CancellationTokenSource _tokenSource;
		private static PublishStandaloneWindow Instance { get; set; }
		private static bool IsAlreadyOpened => Instance != null;

		public BeamEditorContext editorContext;

		public static PublishStandaloneWindow ShowPublishWindow(EditorWindow parent, BeamEditorContext editorContext)
		{
			if (IsAlreadyOpened)
				return null;

			var wnd = CreateInstance<PublishStandaloneWindow>();

			wnd.editorContext = editorContext;

			wnd.name = PUBLISH;
			wnd.titleContent = new GUIContent(PUBLISH);
			wnd.ShowUtility();

			wnd._publishPopup = new PublishStandaloneMicroservicePopup();
			wnd.Refresh();

			wnd.minSize = wnd.maxSize = new Vector2(MIN_SIZE.x, MIN_SIZE.y + Mathf.Clamp(MicroservicesDataModel.Instance.AllUnarchivedServices.Count, 1, MAX_ROW) * ROW_HEIGHT); ;
			wnd.position = BeamablePopupWindow.GetCenterOnMainWin(wnd);

			return wnd;
		}

		private PublishStandaloneMicroservicePopup _publishPopup;

		private void OnEnable()
		{
			Instance = this;
			if (!isSet) return;
		}

		private void RefreshElement()
		{
			_publishPopup.Refresh();
			Repaint();
		}

		private void Refresh()
		{
			VisualElement container = this.GetRootVisualContainer();
			container.Clear();
			_tokenSource = new CancellationTokenSource();
			_publishPopup.OnCloseRequested += () =>
			{
				_tokenSource?.Cancel();
				WindowStateUtility.EnableAllWindows();
				Close();
			};
			_publishPopup.OnSubmit += async (logger) =>
		   {
			   WindowStateUtility.DisableAllWindows(new[] { PUBLISH });
			   await _publishPopup.PrepareForPublish();

			   var publishService = editorContext.ServiceScope.GetService<PublishService>();
			   await publishService.PublishServices();
			   publishService.Init();
		   };

			container.Add(_publishPopup);
			_publishPopup.PrepareParent();
			_publishPopup.Refresh();
			Repaint();
			isSet = true;
		}

		private void OnDestroy()
		{
			Instance = null;
			_tokenSource?.Cancel();
			WindowStateUtility.EnableAllWindows();
		}
	}
}
