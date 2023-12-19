using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants.Features.Services;
using static Beamable.Common.Constants.Features.Services.PublishWindow;
using LogMessage = Beamable.Editor.UI.Model.LogMessage;

namespace Beamable.Editor.Microservice.UI.Components
{
	internal class StandaloneServicePublishStateAnimator
	{
		public int SelectionIndex { get; private set; }

		private readonly VisualElement _animatedElement;
		private readonly int _offset = -300;
		private readonly int _animDuration;

		public StandaloneServicePublishStateAnimator(VisualElement animatedElement, int animDuration = 500)
		{
			_animatedElement = animatedElement;
			_animDuration = animDuration;
		}

		public void Animate(ServicePublishState state)
		{
			SelectionIndex = -1;
			switch (state)
			{
				case ServicePublishState.Verifying:
					SelectionIndex = 1;
					break;
				case ServicePublishState.InProgress:
					SelectionIndex = 2;
					break;
				case ServicePublishState.Published:
				case ServicePublishState.Failed:
					SelectionIndex = 3;
					break;
			}

			if (SelectionIndex != -1)
				Animate(new Vector3(_offset * SelectionIndex, 0, 0));
		}

		private void Animate(Vector3 to) => Animate(to, _animDuration);

		private void Animate(Vector3 to, int duration) =>
			_animatedElement.experimental.animation.Position(to, duration);

		public void Next()
		{
			if (SelectionIndex + 1 == _animatedElement.childCount)
				return;
			SelectionIndex++;
			Animate(new Vector3(_offset * SelectionIndex, 0, 0));
		}

		public void Previous()
		{
			if (SelectionIndex - 1 < 0)
				return;
			SelectionIndex--;
			Animate(new Vector3(_offset * SelectionIndex, 0, 0));
		}
	}

	public class PublishStandaloneMicroservicePopup : MicroserviceComponent
	{
		public new class UxmlFactory : UxmlFactory<PublishStandaloneMicroservicePopup, UxmlTraits> { }

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
				var self = ve as PublishStandaloneMicroservicePopup;
			}
		}

		public Action OnCloseRequested;
		public Action<ManifestModel, Action<LogMessage>> OnSubmit;

		public ManifestModel Model { get; set; }
		public Promise<ManifestModel> InitPromise { get; set; }
		public MicroserviceReflectionCache.Registry Registry { get; set; }

		private VisualElement _servicesList;
		private TextField _userDescription;
		private PrimaryButtonVisualElement _cancelButton;
		private PrimaryButtonVisualElement _primarySubmitButton;
		private ScrollView _scrollContainer;
		private LoadingBarElement _mainLoadingBar;
		private LogVisualElement _logger;
		private Label _infoTitle;
		private Label _infoDescription;
		private VisualElement _arrowLeft;
		private VisualElement _arrowRight;
		private VisualElement _docReference;

		private Dictionary<string, PublishManifestEntryVisualElement> _publishManifestElements;
		private readonly Dictionary<IBeamableService, Action> _logForwardActions = new Dictionary<IBeamableService, Action>();
		private readonly List<PublishManifestEntryVisualElement> _servicesToPublish = new List<PublishManifestEntryVisualElement>();

		private readonly Dictionary<StorageEntryModel, List<ManifestEntryModel>> _storageDependsOnServiceRepresentation =
			new Dictionary<StorageEntryModel, List<ManifestEntryModel>>();

		private List<IEntryModel> _allUnarchivedServices;

		private readonly Color _infoColor = new Color(0f, 146f / 255, 255f / 255);
		private readonly Color _errorColor = new Color(217f / 255, 55f / 255, 55f / 255);
		private ServicePublishStateAnimator _serviceServicePublishStateAnimator;

		public PublishStandaloneMicroservicePopup() : base(nameof(PublishStandaloneMicroservicePopup)) { }

		public override void Refresh()
		{
			base.Refresh();

			var loadingIndicator = Root.Q<LoadingIndicatorVisualElement>();
			loadingIndicator.SetText("Fetching Beamable Cloud Data");
			Assert.IsNotNull(InitPromise, "The InitPromise must be set before calling Refresh()");
			loadingIndicator.SetPromise(InitPromise, Root.Q("mainVisualElement"));
			
			CodeService codeService = ((IServiceProvider)Context.ServiceScope).GetService<CodeService>();

			if (codeService.ServiceDefinitions.Count == 0)
				return;

			_allUnarchivedServices = new List<IEntryModel>();
			
			foreach (IBeamoServiceDefinition serviceDefinition in codeService.ServiceDefinitions)
			{
				if (serviceDefinition.ServiceType == ServiceType.MicroService)
				{
					var allDependencies = new List<ServiceDependency>();
					foreach (string dependency in serviceDefinition.ServiceInfo.dependencies)
					{
						allDependencies.Add(new ServiceDependency { id = dependency, storageType = "storage" });
					}
					
					var entryModel = new ManifestEntryModel()
					{
						Comment = "",
						Name = serviceDefinition.BeamoId,
						Archived = false,
						TemplateId = "small",
						Dependencies = allDependencies
					};
					_allUnarchivedServices.Add(entryModel);
				}

				if (serviceDefinition.ServiceType == ServiceType.StorageObject)
				{
					var storageModel = new StorageEntryModel()
					{
						Comment = "",
						Type = "mongov1", // TODO need to know the type of the storage
						Name = serviceDefinition.BeamoId,
						Archived = false,
						TemplateId = "small"
					};
					_allUnarchivedServices.Add(storageModel);
				}
				
			}

			//var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();

			_serviceServicePublishStateAnimator = new ServicePublishStateAnimator(Root.Q("infoCards"));

			//TODO ask about these guys, should we implement new ones in the CodeService? Should we change the behaviour of them in this registry class?
			//TODO Create a publisher service for USAM that CodeService have control of using DI
			// serviceRegistry.OnServiceDeployStatusChanged -= HandleServiceDeployStatusChanged;
			// serviceRegistry.OnServiceDeployStatusChanged += HandleServiceDeployStatusChanged;
			// serviceRegistry.OnServiceDeployProgress -= HandleServiceDeployProgress;
			// serviceRegistry.OnServiceDeployProgress += HandleServiceDeployProgress;
			// serviceRegistry.OnDeployFailed -= HandleDeployFailed;
			// serviceRegistry.OnDeployFailed += HandleDeployFailed;
			// serviceRegistry.OnDeploySuccess -= HandleDeploySuccess;
			// serviceRegistry.OnDeploySuccess += HandleDeploySuccess;
			// serviceRegistry.OnProgressInfoUpdated -= HandleProgressInfoUpdated;
			// serviceRegistry.OnProgressInfoUpdated += HandleProgressInfoUpdated;

			_mainLoadingBar = Root.Q<LoadingBarElement>("mainLoadingBar");
			_mainLoadingBar.SmallBar = true;
			_mainLoadingBar.Hidden = true;
			_mainLoadingBar.Refresh();

			_scrollContainer = new ScrollView(ScrollViewMode.Vertical);
			_scrollContainer.horizontalScroller?.RemoveFromHierarchy();

#if UNITY_2021_1_OR_NEWER
			_scrollContainer.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
#else
			_scrollContainer.showVertical = true;
#endif
			_servicesList = Root.Q<VisualElement>("servicesList");
			_servicesList.Add(_scrollContainer);

			_infoTitle = Root.Q<Label>("infoTitle");
			_infoTitle.Add(new Label("Publish services to "));
			_infoTitle.Add(new Label($"{Context.CurrentRealm.DisplayName} ") { style = { color = new StyleColor(_infoColor) } });
			_infoTitle.Add(new Label("realm"));

			_infoDescription = Root.Q<Label>("infoDescription");
			_infoDescription.style.color = new StyleColor(_infoColor);
			_infoDescription.text = "Select services to publish";

			_docReference = Root.Q("docReference");
			_docReference.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(Constants.URLs.Documentations.URL_DOC_MICROSERVICES_PUBLISHING));
			_docReference.tooltip = "Document";

			_publishManifestElements = new Dictionary<string, PublishManifestEntryVisualElement>(_allUnarchivedServices.Count);
			_servicesList.style.height = Mathf.Clamp(_allUnarchivedServices.Count, 1, MAX_ROW) * ROW_HEIGHT;

			var elements = new List<PublishManifestEntryVisualElement>();

			for (int index = 0; index < _allUnarchivedServices.Count; index++)
			{
				var model = _allUnarchivedServices[index];
				var isLocal = codeService.GetServiceIsLocal(model.Name);
				var isRemote = MicroservicesDataModel.Instance.ContainsRemoteOnlyModel(model.Name); //TODO figure it out how to know this outside this way
				var newElement = new PublishManifestEntryVisualElement(model, index, isLocal, isRemote);
				newElement.Refresh();
				newElement.OnEnableStateChanged += HandleEnableStateChanged;
				_publishManifestElements.Add(model.Name, newElement);
				elements.Add(newElement);
			}

			var orderedElements = elements.OrderBy(x => x.Model.Type)
															.ThenByDescending(x => x.IsLocal && !x.IsRemote)
															.ToList();

			
			orderedElements.ForEach(x =>
			{
				//TODO this depends on the dependencies list to be working
				var dependencies = GetAllDependencies(x.Model);
				if (x.Model is ManifestEntryModel service)
				{
					// if the number of dependencies is different than those created as PublishManifestEntryVisualElement
					// means that at least one dependent SO for given MS is archived. In that case turn off service.
					if (service.Dependencies.Count != dependencies.Count)
					{
						x.EnableState.SetEnabled(false);
						x.UpdateEnableState(false, true, CHECKBOX_TOOLTIP_ARCHIVED_STORAGE);
						return;
					}
				}

				//TODO we don't have storages working yet in here I guess
				if (x.Model is StorageEntryModel storageModel)
				{
					x.EnableState.SetCheckboxClickable(false);
					if (!_storageDependsOnServiceRepresentation.ContainsKey(storageModel))
					{
						x.UpdateEnableState(false, false, CHECKBOX_TOOLTIP_NO_DEP_ENABLED);
					}
				}

				//TODO modify this once we have the dependencies working
				foreach (var dependency in dependencies)
				{
					if (!(dependency.Model is StorageEntryModel storageEntryModel) ||
						!(x.Model is ManifestEntryModel serviceModel))
						continue;

					if (_storageDependsOnServiceRepresentation.ContainsKey(storageEntryModel))
					{
						_storageDependsOnServiceRepresentation[storageEntryModel].Add(serviceModel);
						continue;
					}
					_storageDependsOnServiceRepresentation.Add(storageEntryModel, new List<ManifestEntryModel> { serviceModel });
				}
			});

			orderedElements.ForEach(x =>
			{
				if (x.Model.Enabled)
					HandleEnableStateChanged(x.Model);
			});
			_scrollContainer.AddRange(orderedElements);

			Root.Q("enableC").tooltip = ON_OFF_HEADER_TOOLTIP;
			Root.Q("nameC").tooltip = NAME_HEADER_TOOLTIP;
			Root.Q("knownLocationC").tooltip = KNOWN_LOCATION_HEADER_TOOLTIP;
			Root.Q("dependenciesC").tooltip = DEPENDENCIES_HEADER_TOOLTIP;
			Root.Q("commentsC").tooltip = COMMENTS_HEADER_TOOLTIP;
			Root.Q("statusC").tooltip = STATUS_HEADER_TOOLTIP;

			_userDescription = Root.Q<TextField>("userDescription");
			_userDescription.AddPlaceholder("Description here...");
			_userDescription.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

			_cancelButton = Root.Q<PrimaryButtonVisualElement>("cancelBtn");
			_cancelButton.Button.clickable.clicked += () => OnCloseRequested?.Invoke();

			_primarySubmitButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
			_primarySubmitButton.Button.clickable.clicked += HandlePrimaryButtonClicked;

			_arrowLeft = Root.Q("arrowLeft");
			_arrowLeft.RegisterCallback<MouseDownEvent>(_ => _serviceServicePublishStateAnimator.Previous());
			_arrowRight = Root.Q("arrowRight");
			_arrowRight.RegisterCallback<MouseDownEvent>(_ => _serviceServicePublishStateAnimator.Next());

			AddLogger();
		}

		private List<PublishManifestEntryVisualElement> GetAllDependencies(IEntryModel model)
		{
			//TODO this might not need to change at all depending if we use these data models
			var dependenciesToUpdate = new List<PublishManifestEntryVisualElement>();
			if (model is ManifestEntryModel serviceModel)
			{
				dependenciesToUpdate.AddRange(from PublishManifestEntryVisualElement entry in _publishManifestElements.Values
											  from dependency in serviceModel.Dependencies
											  where entry.Model.Name == dependency.id
											  select entry);
			}
			return dependenciesToUpdate;
		}

		private void HandleEnableStateChanged(IEntryModel model)
		{
			GetAllDependencies(model)?.ForEach(x =>
			{
				if (!(x.Model is StorageEntryModel storageEntryModel))
					return;

				var isAnyDependentServiceEnabled = _storageDependsOnServiceRepresentation[storageEntryModel].Any(y => y.Enabled);
				var tooltipText = isAnyDependentServiceEnabled
					? CHECKBOX_TOOLTIP_DEPENDENCY_ON_SERVICE
					: CHECKBOX_TOOLTIP_NO_DEP_ENABLED;
				x.UpdateEnableState(isAnyDependentServiceEnabled, false, tooltipText);

			});
		}

		public void PrepareParent()
		{
			parent.name = "PublishWindowContainer";
			parent.AddStyleSheet(UssPath);
		}

		public void PrepareForPublish()
		{
			_arrowLeft.SetEnabled(false);
			_arrowRight.SetEnabled(false);
			_mainLoadingBar.Hidden = false;

			foreach (var kvp in _publishManifestElements)
			{
				//TODO this needs to be changed to use the right data model and make sure
				// that all the data we need is in that model
				var serviceModel = MicroservicesDataModel.Instance.GetModel<ServiceModelBase>(kvp.Key);
				if (serviceModel == null)
				{
					Debug.LogError($"Cannot find model: {kvp.Key}");
					continue;
				}

				if (serviceModel.IsArchived)
					continue;

				if (serviceModel is MongoStorageModel || !kvp.Value.Model.Enabled || (kvp.Value.IsRemote && !kvp.Value.IsLocal))
				{
					kvp.Value.UpdateStatus(ServicePublishState.Published);
					continue;
				}

				kvp.Value.UpdateStatus(ServicePublishState.Unpublished);
				new DeployMSLogParser(kvp.Value.LoadingBar, serviceModel); //TODO this would likely change as well because the model will change
				_servicesToPublish.Add(kvp.Value);
			}
		}

		private void AddLogger()
		{
			_logger = new LogVisualElement
			{
				Model = new PublishServiceAccumulator(),
				EnableDetatchButton = false,
				EnableMoreButton = false
			};

			//TODO change this to not use MicroservicesDataModel and use CodeService instead
			//TODO this whole implementation is going to change!
			foreach (var desc in MicroservicesDataModel.Instance.AllLocalServices.Where(x => !x.IsArchived))
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

		private void HandleProgressInfoUpdated(string message, ServicePublishState state)
		{
			_infoDescription.text = message;
			if (state == ServicePublishState.Failed)
				_infoDescription.style.color = new StyleColor(_errorColor);
			else
				_serviceServicePublishStateAnimator.Animate(state);
		}
		private void HandlePrimaryButtonClicked()
		{
			if (Context.CurrentRealm.IsProduction)
				if (!EditorUtility.DisplayDialog("Warning", "You are trying to publish services to the PROD realm. Continue?", "Publish", "Cancel"))
					return;

			foreach (PublishManifestEntryVisualElement manifestEntryVisualElement in _publishManifestElements.Values)
				manifestEntryVisualElement.HandlePublishStarted();

			_primarySubmitButton.SetText("Publishing...");
			_primarySubmitButton.Disable();
			OnSubmit?.Invoke(Model, (message) => _logger.Model.Logs.AddMessage(message));
		}

		private void HandleServiceDeployStatusChanged(IDescriptor descriptor, ServicePublishState state)
		{
			if (!_publishManifestElements.TryGetValue(descriptor.Name, out var element))
				return;

			element?.UpdateStatus(state);
			switch (state)
			{
				case ServicePublishState.Failed:
					Root.Q<Image>($"infoCardStep{_serviceServicePublishStateAnimator.SelectionIndex + 1}").AddToClassList("card-error");
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
			if (success)
				_serviceServicePublishStateAnimator.Animate(ServicePublishState.Published);

			_primarySubmitButton.SetText("Close");
			_primarySubmitButton.Enable();
			_primarySubmitButton.SetAsFailure(!success);
			_primarySubmitButton.Button.clickable.clicked -= HandlePrimaryButtonClicked;
			_primarySubmitButton.Button.clickable.clicked += () => OnCloseRequested?.Invoke();
			_arrowLeft.SetEnabled(true);
			_arrowRight.SetEnabled(true);
		}

		protected override void OnDestroy()
		{
			//TODO change this to match the log implementation we end up using for this
			foreach (var desc in MicroservicesDataModel.Instance.AllLocalServices.Where(x => !x.IsArchived))
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
