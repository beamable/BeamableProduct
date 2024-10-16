using Beamable.Common;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.Microservice.UI2.Models;
using Beamable.Editor.UI.Model;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI
{
	[System.Serializable]
	public class ServiceLogWindow : EditorWindow
	{
		public static void ShowService(IMicroserviceVisualsModel service)
		{
			var existing = Resources.FindObjectsOfTypeAll<ServiceLogWindow>().ToList().FirstOrDefault(w => w._serviceName.Equals(service.Name));
			if (existing != null)
			{
				existing.Show(true);
				return;
			}
			var window = CreateInstance<ServiceLogWindow>();
			window.titleContent = new GUIContent($"{service.Name} Logs");
			window.minSize = new Vector2(450, 200);
			window.SetModel(service);
			window.Init();
			window.Show(true);
		}

		private VisualElement _windowRoot;

		[NonSerialized] // we need this to get repulled every time, so that the event registration lines up.
		private IMicroserviceVisualsModel _model;

		[SerializeField]
		private string _serviceName;

		[NonSerialized]
		private bool _registeredEvents;

		[NonSerialized]
		private bool _isClosing;

		private LogVisualElement _logElement;

		private void Init()
		{
			BeamEditorContext.Default.Dispatcher.Schedule(() =>
			{
				if (_model == null)
				{
					_model = MicroservicesDataModel.Instance.GetModel<ServiceModelBase>(_serviceName);
				}
				if (_model == null)
				{
					_model = BeamEditorContext.Default.ServiceScope.GetService<UsamDataModel>().GetModel(_serviceName);
				}
				_model.DetachLogs(); // take note of the fact that logs are detached...
				RegisterEvents();
				Refresh();
			});
		}


		private void RegisterEvents()
		{
			if (_registeredEvents) return;
			_registeredEvents = true;
			_model.OnLogsAttached -= OnLogsAttached;
			_model.OnLogsAttached += OnLogsAttached;
		}

		void OnLogsAttached()
		{
			if (!_isClosing)
			{
				Close();
			}
		}

		private void SetModel(IMicroserviceVisualsModel model)
		{
			_model = model;
			_serviceName = model.Name;
		}

		private void OnBecameVisible()
		{
			Init();
		}


		private void OnDestroy()
		{
			_isClosing = true;
			_logElement?.Destroy();
			_model?.AttachLogs();
		}


		void Refresh()
		{
			var root = this.GetRootVisualContainer();
			root.Clear();
			var uiAsset =
			   AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR_UI}/ServiceLogWindow.uxml");
			_windowRoot = uiAsset.CloneTree();
			_windowRoot.AddStyleSheet($"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR_UI}/ServiceLogWindow.uss");
			_windowRoot.name = nameof(_windowRoot);

			root.Add(_windowRoot);

			var logContainerElement = _windowRoot.Q<VisualElement>("logContainer");
			_logElement = new LogVisualElement();
			_logElement.Model = _model;
			_logElement.Refresh();

			logContainerElement.Add(_logElement);
		}

	}
}
