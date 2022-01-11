using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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
				name = "custom-text", defaultValue = "nada"
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

		private const int MILISECOND_PER_UPDATE = 250;

		public Action OnCloseRequested;
		public Action<ManifestModel> OnSubmit;

		public ManifestModel Model
		{
			get;
			set;
		}

		private TextField _generalComments;
		private GenericButtonVisualElement  _cancelButton;
		private PrimaryButtonVisualElement _continueButton;
		private ScrollView _scrollContainer;
		private Dictionary<string, PublishManifestEntryVisualElement> _publishManifestElements;
		private LoadingBarElement _mainLoadingBar;
		private Label _topMessage;
		int _topMessageCounter = 0;
		private DateTime _lastUpdateTime;
		private bool _topMessageUpdating = false;

		private readonly string[] topMessageUpdateTexts =
			{"Deploying   ", "Deploying.  ", "Deploying.. ", "Deploying..."};

		public PublishPopup() : base(nameof(PublishPopup)) { }

		public void PrepareParent()
		{
			parent.name = "PublishWindowContainer";
			parent.AddStyleSheet(USSPath);
		}

		public override void Refresh()
		{
			base.Refresh();

			if (Model?.Services == null)
			{
				return;
			}

			Microservices.OnServiceDeployStatusChanged -= HandleServiceServiceDeployStatusChange;
			Microservices.OnServiceDeployStatusChanged += HandleServiceServiceDeployStatusChange;
			Microservices.OnServiceDeployProgress -= HandleServiceDeployProgress;
			Microservices.OnServiceDeployProgress += HandleServiceDeployProgress;
			Microservices.OnDeploySuccess -= HandleDeploySuccess;
			Microservices.OnDeploySuccess += HandleDeploySuccess;

			_mainLoadingBar = Root.Q<LoadingBarElement>("mainLoadingBar");
			_mainLoadingBar.SmallBar = true;
			_mainLoadingBar.Hidden = true;
			_mainLoadingBar.Refresh();

			_scrollContainer = Root.Q<ScrollView>("manifestsContainer");
			_publishManifestElements = new Dictionary<string, PublishManifestEntryVisualElement>(Model.Services.Count);

			var entryModels = new List<IEntryModel>(Model.Services.Values);
			entryModels.AddRange(Model.Storages.Values);

			bool isOddRow = true;
			foreach (IEntryModel model in entryModels)
			{
				bool wasPublished = EditorPrefs.GetBool(GetPublishedKey(model.Name), false);
				var newElement = new PublishManifestEntryVisualElement(model, wasPublished, isOddRow);
				newElement.Refresh();
				_publishManifestElements.Add(model.Name, newElement);
				_scrollContainer.Add(newElement);

				isOddRow = !isOddRow;
			}

			_generalComments = Root.Q<TextField>("largeCommentsArea");
			_generalComments.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

			_cancelButton = Root.Q<GenericButtonVisualElement >("cancelBtn");
			_cancelButton.OnClick += () => OnCloseRequested?.Invoke();

			_continueButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
			_continueButton.Button.clickable.clicked += () => OnSubmit?.Invoke(Model);
			_topMessage = Root.Q<Label>("topMessage");
			_lastUpdateTime = DateTime.Now;
			
			HandleServiceServiceDeployStatusChange(null, ServicePublishState.Unpublished);
		}

		private void HandleDeploySuccess(ManifestModel manifest, int servicesAmount)
		{
			_topMessageUpdating = false;
			const string oneService = "service";
			const string multipleServices = "services";
			_topMessage.text = $"Congratulations! You have successfully published {servicesAmount} {(servicesAmount == 1 ? oneService : multipleServices)}!";
		}

		public void PrepareForPublish()
		{
			_mainLoadingBar.Hidden = false;

			foreach (KeyValuePair<string, PublishManifestEntryVisualElement> kvp in _publishManifestElements)
			{
				var serviceModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(kvp.Key);

				if (serviceModel == null)
				{ 
					Debug.LogError($"Cannot find model: {kvp.Key}");
					continue;
				}

				kvp.Value.UpdateStatus(ServicePublishState.Unpublished);
				new DeployMSLogParser(kvp.Value.LoadingBar, serviceModel);
			}
		}

		public void HandleServiceDeployed(IDescriptor descriptor)
		{
			EditorPrefs.SetBool(GetPublishedKey(descriptor.Name), true);
			HandleServiceDeployProgress(descriptor);
		}

		private string GetPublishedKey(string serviceName)
		{
			return string.Format(Microservices.SERVICE_PUBLISHED_KEY, serviceName);
		}

		private void HandleServiceServiceDeployStatusChange(IDescriptor descriptor, ServicePublishState state)
		{
			switch (state)
			{
				case ServicePublishState.Unpublished:
					_topMessage.text = "Are you sure you want to deploy your microservices?";
					return;
				case ServicePublishState.InProgress:
					if(!_topMessageUpdating)
					{
						EditorApplication.update += UpdateTopMessageText;
						_topMessageUpdating = true;
					}
					return;
				case ServicePublishState.Failed:
					_mainLoadingBar.UpdateProgress(0, failed: true);
					foreach (KeyValuePair<string, PublishManifestEntryVisualElement> kvp in _publishManifestElements)
					{
						kvp.Value.LoadingBar.SetUpdater(null);
					}
					_topMessage.text = $"Oh no! Errors appears during publishing of {descriptor.Name}. Please check the log for detailed information.";
					break;
				case ServicePublishState.Published:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}
			EditorApplication.update -= UpdateTopMessageText;
			_topMessageUpdating = false;
			_publishManifestElements[descriptor.Name].UpdateStatus(state);
		}

		private void UpdateTopMessageText()
		{
			if (!_topMessageUpdating) 
				return;
			var currentTime = DateTime.Now;
			if(_lastUpdateTime.AddMilliseconds(MILISECOND_PER_UPDATE) > currentTime)
				return;
			_lastUpdateTime = currentTime;
			_topMessageCounter++;
			var currentTextValue =
				topMessageUpdateTexts[_topMessageCounter % topMessageUpdateTexts.Length];
			_topMessage.text = currentTextValue;
		}

		private void HandleServiceDeployProgress(IDescriptor descriptor)
		{
			_mainLoadingBar.Progress = CalculateProgress();
		}

		private float CalculateProgress()
		{
			return _publishManifestElements.Values.Average(x => x.LoadingBar.Progress);
		}
	}
}
