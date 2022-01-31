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

		private bool isSet;
		private CancellationTokenSource _tokenSource;

		public static PublishWindow ShowPublishWindow()
		{
			var wnd = CreateInstance<PublishWindow>();
			wnd.name = Constants.Publish;
			wnd.titleContent = new GUIContent(Constants.Publish);

			wnd.ShowUtility();
			wnd.minSize = MIN_SIZE;
			wnd.position = new Rect(wnd.position.x, wnd.position.y + 40, wnd.minSize.x, wnd.minSize.y);

			var servicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			servicesRegistry.GenerateUploadModel().Then(model =>
			{
				wnd._model = model;
				wnd.minSize = new Vector2(MIN_SIZE.x, MIN_SIZE.y + Mathf.Clamp(model?.Services?.Count ?? 1, 1, MAX_ROW) * DEFAULT_ROW_HEIGHT);
				wnd.Refresh();
			});


			return wnd;
		}

		private ManifestModel _model;

		private void OnEnable()
		{
			VisualElement root = this.GetRootVisualContainer();

			if (isSet)
			{
				Refresh();

				var servicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
				servicesRegistry.GenerateUploadModel().Then(model =>
				{
					_model = model;
					Refresh();
				});
			}
		}

		private void Refresh()
		{
			VisualElement container = this.GetRootVisualContainer();
			container.Clear();
			var e = new PublishPopup { Model = _model };
			_tokenSource = new CancellationTokenSource();
			e.OnCloseRequested += () =>
			{
				_tokenSource?.Cancel();
				WindowStateUtility.EnableAllWindows();
				Close();
			};
			e.OnSubmit += async model =>
			{
				/*
				 * We need to build each image...
				 * upload each image that is different than whats in the manifest...
				 * upload the manifest file...
				 */
				WindowStateUtility.DisableAllWindows(new[] { Constants.Publish });
				e.PrepareForPublish();
				var microservicesRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
				await microservicesRegistry.Deploy(model, this, _tokenSource.Token, e.HandleServiceDeployed);
			};

			container.Add(e);
			e.PrepareParent();
			e.Refresh();
			Repaint();
			isSet = true;
		}

		private void OnDestroy()
		{
			_tokenSource?.Cancel();
			WindowStateUtility.EnableAllWindows();
		}
	}
}
