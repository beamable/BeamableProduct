using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli.Commands;
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
		public Action<Action<LogMessage>> OnSubmit;

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
		private readonly List<PublishManifestEntryVisualElement> _servicesToPublish = new List<PublishManifestEntryVisualElement>();

		private readonly Dictionary<StorageEntryModel, List<ManifestEntryModel>> _storageDependsOnServiceRepresentation =
			new Dictionary<StorageEntryModel, List<ManifestEntryModel>>();

		private List<IEntryModel> _allUnarchivedServices;

		private readonly Color _infoColor = new Color(0f, 146f / 255, 255f / 255);
		private readonly Color _errorColor = new Color(217f / 255, 55f / 255, 55f / 255);
		private StandaloneServicePublishStateAnimator _serviceServicePublishStateAnimator;

		public PublishStandaloneMicroservicePopup() : base(nameof(PublishStandaloneMicroservicePopup)) { }

		public override void Refresh()
		{
			base.Refresh();

			CodeService codeService = ((IServiceProvider)Context.ServiceScope).GetService<CodeService>();

			var loadingIndicator = Root.Q<LoadingIndicatorVisualElement>();
			loadingIndicator.SetText("Fetching Beamable Cloud Data");
			Assert.IsNotNull(codeService.OnReady, "The InitPromise must be set before calling Refresh()");
			loadingIndicator.SetPromise(codeService.OnReady, Root.Q("mainVisualElement"));

			if (codeService.ServiceDefinitions.Count == 0)
				return;

			_allUnarchivedServices = new List<IEntryModel>();

			foreach (IBeamoServiceDefinition serviceDefinition in codeService.ServiceDefinitions)
			{
				if (serviceDefinition.ServiceType == ServiceType.MicroService)
				{
					// TODO Make it work
					var allDependencies = new List<ServiceDependency>();

					var entryModel = new ManifestEntryModel()
					{
						Comment = "",
						Name = serviceDefinition.BeamoId,
						Archived = false,
						TemplateId = "small",
						Dependencies = allDependencies,
						Enabled = serviceDefinition.ShouldBeEnabledOnRemote
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
						TemplateId = "small",
						Enabled = serviceDefinition.ShouldBeEnabledOnRemote
					};
					_allUnarchivedServices.Add(storageModel);
				}

			}

			var publishService = Context.ServiceScope.GetService<PublishService>();

			_serviceServicePublishStateAnimator = new StandaloneServicePublishStateAnimator(Root.Q("infoCards"));

			publishService.OnDeployStateProgress -= HandleServiceDeployStatusChanged;
			publishService.OnDeployStateProgress += HandleServiceDeployStatusChanged;
			publishService.OnDeployFailed -= HandleDeployFailed;
			publishService.OnDeployFailed += HandleDeployFailed;
			publishService.OnDeploySuccess -= HandleDeploySuccess;
			publishService.OnDeploySuccess += HandleDeploySuccess;
			publishService.OnProgressInfoUpdated -= HandleProgressInfoUpdated;
			publishService.OnProgressInfoUpdated += HandleProgressInfoUpdated;

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
			//_userDescription.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

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

		public async Promise PrepareForPublish()
		{
			_arrowLeft.SetEnabled(false);
			_arrowRight.SetEnabled(false);
			_mainLoadingBar.Hidden = false;

			var codeService = Context.ServiceScope.GetService<CodeService>();
			var publisher = Context.ServiceScope.GetService<PublishService>();
			var cli = Context.ServiceScope.GetService<BeamCommands>();

			codeService.UpdateServicesEnableState(_allUnarchivedServices);
			await CodeService.SetManifest(cli, codeService.ServiceDefinitions);

			foreach (var kvp in _publishManifestElements)
			{
				var serviceModel = _allUnarchivedServices.Find((x) => x.Name == kvp.Key);
				if (serviceModel == null)
				{
					Debug.LogError($"Cannot find model: {kvp.Key}");
					continue;
				}

				if (serviceModel is StorageEntryModel || !kvp.Value.Model.Enabled || (kvp.Value.IsRemote && !kvp.Value.IsLocal))
				{
					kvp.Value.UpdateStatus(ServicePublishState.Published);
					continue;
				}

				kvp.Value.UpdateStatus(ServicePublishState.Unpublished);

				new DeployStandaloneMSLogParser(kvp.Value.LoadingBar, kvp.Key, publisher);
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

			var publishService = Context.ServiceScope.GetService<PublishService>();
			publishService.OnDeployLogMessage -= HandleLogMessage;
			publishService.OnDeployLogMessage += HandleLogMessage;

			Root.Q("logContainer").Add(_logger);
			_logger.Refresh();
		}

		private void HandleLogMessage(string level, string message, string timeStamp)
		{
			var parsed = Enum.TryParse(level, out LogLevel enumLevel);

			if (!parsed)
			{
				Debug.LogError($"Could not parse log level of value: {level}");
				return;
			}

			var copiedMessage = new LogMessage
			{
				Level = enumLevel,
				IsBoldMessage = false,
				Message = message,
				MessageColor = Color.white,
				Parameters = new Dictionary<string, object>(),
				ParameterText = string.Empty,
				PostfixMessageIcon = null,
				Timestamp = timeStamp
			};
			_logger.Model.Logs.AddMessage(copiedMessage);
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
			OnSubmit?.Invoke((message) => _logger.Model.Logs.AddMessage(message));
		}

		private void HandleServiceDeployStatusChanged(string beamoId, ServicePublishState state)
		{
			if (!_publishManifestElements.TryGetValue(beamoId, out var element))
				return;

			if (element.PublishState == ServicePublishState.Published)
				return;

			element.UpdateStatus(state);
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

		private void HandleDeployFailed(string __) => HandleDeployEnded(false);
		private void HandleDeploySuccess() => HandleDeployEnded(true);

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
			var publishService = Context.ServiceScope.GetService<PublishService>();
			publishService.OnDeployLogMessage -= HandleLogMessage;
			base.OnDestroy();
		}
	}
}
