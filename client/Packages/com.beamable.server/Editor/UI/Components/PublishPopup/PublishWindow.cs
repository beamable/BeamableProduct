using Beamable.Editor.UI.Buss.Components;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.UI;
using Beamable.Server.Editor.UI.Components;
using System.Threading;
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
	public class PublishWindow : CommandRunnerWindow
	{
		protected const float DEFAULT_ROW_HEIGHT = 47.0f;
		protected const int MAX_ROW = 6;
		private static readonly Vector2 MIN_SIZE = new Vector2(860, 330);

		// private bool isSet;
		private CancellationTokenSource _tokenSource;

		public static PublishWindow ShowPublishWindow(EditorWindow parent)
		{
			var wnd = CreateInstance<PublishWindow>();
			wnd.name = Constants.Publish;
			wnd.titleContent = new GUIContent(Constants.Publish);
			wnd.ShowUtility();
			wnd._model = new ManifestModel();

			var servicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			var loadPromise = servicesRegistry.GenerateUploadModel();

			wnd._element = new PublishPopup {Model = wnd._model, InitPromise = loadPromise, Registry = servicesRegistry};
			wnd.Refresh();

			var size = new Vector2(MIN_SIZE.x, MIN_SIZE.y + Mathf.Clamp(servicesRegistry.AllDescriptors.Count, 1, MAX_ROW) * DEFAULT_ROW_HEIGHT);

			wnd.minSize = size;
			wnd.position = BeamablePopupWindow.GetCenteredScreenRectForWindow(parent, size);
			loadPromise.Then(model =>
			{
				wnd._model = model;
				wnd._element.Model = model;
				wnd.RefreshElement();
			});


			return wnd;
		}

		private ManifestModel _model;
		private PublishPopup _element;

		// private void OnEnable()
		// {
		// 	VisualElement root = this.GetRootVisualContainer();
		//
		// 	if (isSet)
		// 	{
		// 		Refresh();
		//
		// 		var servicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
		// 		servicesRegistry.GenerateUploadModel().Then(model =>
		// 		{
		// 			_model = model;
		// 			Refresh();
		// 		});
		// 	}
		// }

		private void RefreshElement()
		{
			_element.Refresh();
			Repaint();
		}

		private void Refresh()
		{
			VisualElement container = this.GetRootVisualContainer();
			container.Clear();
			_tokenSource = new CancellationTokenSource();
			_element.OnCloseRequested += () =>
			{
				_tokenSource?.Cancel();
				WindowStateUtility.EnableAllWindows();
				Close();
			};
			_element.OnSubmit += async model =>
			{
				/*
				 * We need to build each image...
				 * upload each image that is different than whats in the manifest...
				 * upload the manifest file...
				 */
				WindowStateUtility.DisableAllWindows(new[] { Constants.Publish });
				_element.PrepareForPublish();
				var microservicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
				await microservicesRegistry.Deploy(model, this, _tokenSource.Token, _element.HandleServiceDeployed);
			};

			container.Add(_element);
			_element.PrepareParent();
			_element.Refresh();
			Repaint();
			// isSet = true;
		}

		private void OnDestroy()
		{
			_tokenSource?.Cancel();
			WindowStateUtility.EnableAllWindows();
		}
	}
}
