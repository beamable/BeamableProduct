using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public class PublishPopup : MicroserviceComponent
	{
		public new class UxmlFactory : UxmlFactory<PublishPopup, UxmlTraits> { }
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{
				name = "custom-text",
				defaultValue = "nada"
			};

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get
				{
					yield break;
				}
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as PublishPopup;
			}
		}

		public Action OnCloseRequested;
		public Action<ManifestModel, Action<LogMessage>> OnSubmit;

		public ManifestModel Model { get; set; }
		public Promise<ManifestModel> InitPromise { get; set; }
		public MicroserviceReflectionCache.Registry Registry { get; set; }

		private VisualElement _servicesList;
		private TextField _userDescription;
		private GenericButtonVisualElement _cancelButton;
		private PrimaryButtonVisualElement _primarySubmitButton;
		private ScrollView _scrollContainer;
		private LoadingBarElement _mainLoadingBar;
		private LogVisualElement _logger;
		private Label _infoTitle;

		private Dictionary<string, PublishManifestEntryVisualElement> _publishManifestElements;
		private readonly Dictionary<IBeamableService, Action> _logForwardActions = new Dictionary<IBeamableService, Action>();
		private readonly List<PublishManifestEntryVisualElement> _servicesToPublish = new List<PublishManifestEntryVisualElement>();

		public PublishPopup() : base(nameof(PublishPopup)) { }
		public override void Refresh()
		{
			base.Refresh();
			
			var loadingIndicator = Root.Q<LoadingIndicatorVisualElement>();
			loadingIndicator.SetText("Fetching Beamable Cloud Data");
			Assert.IsNotNull(InitPromise, "The InitPromise must be set before calling Refresh()");
			loadingIndicator.SetPromise(InitPromise, Root.Q("mainVisualElement"));
			
			if (Model?.Services == null)
				return;

			var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();

			serviceRegistry.OnServiceDeployStatusChanged -= HandleServiceDeployStatusChanged;
			serviceRegistry.OnServiceDeployStatusChanged += HandleServiceDeployStatusChanged;
			serviceRegistry.OnServiceDeployProgress -= HandleServiceDeployProgress;
			serviceRegistry.OnServiceDeployProgress += HandleServiceDeployProgress;
			serviceRegistry.OnDeployFailed -= HandleDeployFailed;
			serviceRegistry.OnDeployFailed += HandleDeployFailed;
			serviceRegistry.OnDeploySuccess -= HandleDeploySuccess;
			serviceRegistry.OnDeploySuccess += HandleDeploySuccess;

			_mainLoadingBar = Root.Q<LoadingBarElement>("mainLoadingBar");
			_mainLoadingBar.SmallBar = true;
			_mainLoadingBar.Hidden = true;
			_mainLoadingBar.Refresh();

			_scrollContainer = new ScrollView(ScrollViewMode.Vertical);
			_scrollContainer.horizontalScroller?.RemoveFromHierarchy();
			_scrollContainer.showVertical = true;
			_servicesList = Root.Q<VisualElement>("servicesList");
			_servicesList.Add(_scrollContainer);

			_infoTitle = Root.Q<Label>("infoTitle");
			_infoTitle.text = $"Publish Service to {Context.CurrentRealm.DisplayName} realm";
			
			_publishManifestElements = new Dictionary<string, PublishManifestEntryVisualElement>(Model.Services.Count);

			var entryModels = new List<IEntryModel>(Model.Services.Values);
			entryModels.AddRange(Model.Storages.Values);

			for (int index = 0; index < entryModels.Count; index++)
			{
				var model = entryModels[index];
				var wasPublished = EditorPrefs.GetBool(GetPublishedKey(model.Name), false);
				var isLocal = MicroservicesDataModel.Instance.ContainsModel(model.Name);
				var isRemote = MicroservicesDataModel.Instance.ContainsRemoteOnlyModel(model.Name);
				var newElement = new PublishManifestEntryVisualElement(model, wasPublished, index, isLocal, isRemote);
				newElement.Refresh();
				_publishManifestElements.Add(model.Name, newElement);
				_scrollContainer.Add(newElement);

				if (model.Archived)
					newElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			}

			_userDescription = Root.Q<TextField>("userDescription");
			_userDescription.AddPlaceholder("Description here...");
			_userDescription.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

			_cancelButton = Root.Q<GenericButtonVisualElement>("cancelBtn");
			_cancelButton.OnClick += () => OnCloseRequested?.Invoke();

			_primarySubmitButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
			_primarySubmitButton.Button.clickable.clicked += HandlePrimaryButtonClicked;

			// SortServices();
			AddLogger();
		}
		public void PrepareParent()
		{
			parent.name = "PublishWindowContainer";
			parent.AddStyleSheet(UssPath);
		}
		public void PrepareForPublish()
		{
			_mainLoadingBar.Hidden = false;

			foreach (var kvp in _publishManifestElements)
			{
				var serviceModel = MicroservicesDataModel.Instance.GetModel<ServiceModelBase>(kvp.Key);
				if (serviceModel == null)
				{
					Debug.LogError($"Cannot find model: {kvp.Key}");
					continue;
				}

				if (serviceModel.IsArchived)
					continue;

				kvp.Value.UpdateStatus(ServicePublishState.Unpublished);
				// new DeployMSLogParser(kvp.Value.LoadingBar, serviceModel);
				_servicesToPublish.Add(kvp.Value);
			}
		}
		
		// private void SortServices()
		// {
		// 	int Comparer(VisualElement a, VisualElement b) =>
		// 		a is PublishManifestEntryVisualElement firstManifestElement &&
		// 		b is PublishManifestEntryVisualElement secondManifestElement
		// 			? firstManifestElement.CompareTo(secondManifestElement)
		// 			: 0;
		// 	
		// 	_scrollContainer.Sort(Comparer);
		// }
		private void AddLogger()
		{
			_logger = new LogVisualElement
			{
				Model = new PublishServiceAccumulator(),
				EnableDetatchButton = false,
				EnableMoreButton = false
			};

			foreach (var desc in MicroservicesDataModel.Instance.AllLocalServices)
			{
				void ForwardLog()
				{
					var message = desc.Logs.Messages.LastOrDefault();
					if (message == null)
						return;

					var copiedMessage = new LogMessage
					{
						Level = message.Level,
						IsBoldMessage = message.IsBoldMessage,
						Message = $"{desc.Name} - {message.Message}",
						MessageColor = message.MessageColor,
						Parameters = message.Parameters,
						ParameterText = message.ParameterText,
						PostfixMessageIcon = message.PostfixMessageIcon,
						Timestamp = message.Timestamp
					};
					_logger.Model.Logs.AddMessage(copiedMessage);
				}
				_logForwardActions.Add(desc, ForwardLog);
				desc.Logs.OnMessagesUpdated += ForwardLog;
			}

			Root.Q("logContainer").Add(_logger);
			_logger.Refresh();
		} 
		private float CalculateProgress() => _servicesToPublish.Count == 0 ? 0f : _servicesToPublish.Average(x => x.LoadingBar.Progress);
		private static string GetPublishedKey(string serviceName) => string.Format(MicroserviceReflectionCache.Registry.SERVICE_PUBLISHED_KEY, serviceName);

		private void HandlePrimaryButtonClicked()
		{
			foreach (PublishManifestEntryVisualElement manifestEntryVisualElement in _publishManifestElements.Values)
				manifestEntryVisualElement.HandlePublishStarted();

			// _topMessage.HandleSubmitClicked();
			_primarySubmitButton.SetText("Publishing...");
			_primarySubmitButton.Disable();
			// AddLogger();
			OnSubmit?.Invoke(Model, (message) => _logger.Model.Logs.AddMessage(message));
		}
		private void HandleServiceDeployStatusChanged(IDescriptor descriptor, ServicePublishState state)
		{
			if (!_publishManifestElements.TryGetValue(descriptor.Name, out var element))
				return;

			element?.UpdateStatus(state);
			// SortServices();
			switch (state)
			{
				case ServicePublishState.Failed:
					_primarySubmitButton.Enable();
					_mainLoadingBar.UpdateProgress(0, failed: true);
					foreach (KeyValuePair<string, PublishManifestEntryVisualElement> kvp in _publishManifestElements)
						kvp.Value.LoadingBar.SetUpdater(null);
					break;
			}
		}
		private void HandleServiceDeployProgress(IDescriptor descriptor)
		{
			_mainLoadingBar.Progress = CalculateProgress();
		}
		public void HandleServiceDeployed(IDescriptor descriptor)
		{
			EditorPrefs.SetBool(GetPublishedKey(descriptor.Name), true);
			_servicesToPublish.FirstOrDefault(x => x.Model.Name == descriptor.Name)?.LoadingBar?.UpdateProgress(1);
			HandleServiceDeployProgress(descriptor);
		}
		private void HandleDeployFailed(ManifestModel _, string __) => HandleDeployEnded(false);
		private void HandleDeploySuccess(ManifestModel _, int __) => HandleDeployEnded(true);
		private void HandleDeployEnded(bool success)
		{
			_primarySubmitButton.SetText("Close");
			_primarySubmitButton.Enable();
			_primarySubmitButton.SetAsFailure(!success);
			_primarySubmitButton.Button.clickable.clicked -= HandlePrimaryButtonClicked;
			_primarySubmitButton.Button.clickable.clicked += () => OnCloseRequested?.Invoke();
		}
		
		protected override void OnDestroy()
		{
			foreach (var desc in MicroservicesDataModel.Instance.AllLocalServices)
			{
				if (!_logForwardActions.TryGetValue(desc, out var cb)) 
					continue;
				if (desc.Logs == null) 
					continue;
				desc.Logs.OnMessagesUpdated -= cb;
			}
			_logForwardActions.Clear();
			base.OnDestroy();
		}
	}
}
