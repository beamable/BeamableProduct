using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI;
using Beamable.Server.Editor.UI.Components;
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

		public static PublishStandaloneWindow ShowPublishWindow(EditorWindow parent, BeamEditorContext editorContext)
		{
			if (IsAlreadyOpened)
				return null;

			var wnd = CreateInstance<PublishStandaloneWindow>();

			wnd.name = PUBLISH;
			wnd.titleContent = new GUIContent(PUBLISH);
			wnd.ShowUtility();
			wnd._model = new ManifestModel();

			//TODO this should be changed to a different cache of the services data?
			var servicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			var loadPromise = servicesRegistry.GenerateUploadModel();

			wnd._publishPopup = new PublishStandaloneMicroservicePopup { Model = wnd._model, InitPromise = loadPromise, Registry = servicesRegistry };
			wnd.Refresh();

			wnd.minSize = wnd.maxSize = new Vector2(MIN_SIZE.x, MIN_SIZE.y + Mathf.Clamp(MicroservicesDataModel.Instance.AllUnarchivedServices.Count, 1, MAX_ROW) * ROW_HEIGHT); ;
			wnd.position = BeamablePopupWindow.GetCenterOnMainWin(wnd);

			loadPromise.Then(model =>
			{
				wnd._model = model;
				wnd._publishPopup.Model = model;
				wnd.RefreshElement();
			});

			return wnd;
		}

		private ManifestModel _model;
		private PublishStandaloneMicroservicePopup _publishPopup;

		private void OnEnable()
		{
			Instance = this;
			if (!isSet) return;

			//TODO more access to the MRC here
			var servicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			servicesRegistry.GenerateUploadModel().Then(model =>
			{
				_model = model;
				_publishPopup = new PublishStandaloneMicroservicePopup() { Model = _model, InitPromise = Promise<ManifestModel>.Successful(model), Registry = servicesRegistry };
				Refresh();
				RefreshElement();
			});
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
			_publishPopup.OnSubmit +=  (model, logger) =>
			{
				/*
				 * We need to build each image...
				 * upload each image that is different than whats in the manifest...
				 * upload the manifest file...
				 */
				//WindowStateUtility.DisableAllWindows(new[] { PUBLISH });
				//_publishPopup.PrepareForPublish();
				//var microservicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
				//await microservicesRegistry.Deploy(model, _tokenSource.Token, _publishPopup.HandleServiceDeployed, logger);
				
				//TODO publish here using the CLI or calling CodeSErvice stuff
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
