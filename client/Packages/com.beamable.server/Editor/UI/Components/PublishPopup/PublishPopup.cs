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

		public Action OnCloseRequested;
		public Action<ManifestModel> OnSubmit;

		public ManifestModel Model
		{
			get;
			set;
		}

		private TextField _generalComments;
		private GenericButtonVisualElement _cancelButton;
		private PrimaryButtonVisualElement _primarySubmitButton;
		private ScrollView _scrollContainer;
		private Dictionary<string, PublishManifestEntryVisualElement> _publishManifestElements;
		private LoadingBarElement _mainLoadingBar;
		private PublishStatusVisualElement _topMessage;

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

			Microservices.OnServiceDeployStatusChanged -= HandleServiceDeployStatusChanged;
			Microservices.OnServiceDeployStatusChanged += HandleServiceDeployStatusChanged;
			Microservices.OnServiceDeployProgress -= HandleServiceDeployProgress;
			Microservices.OnServiceDeployProgress += HandleServiceDeployProgress;
			Microservices.OnDeployFailed -= HandleDeployFailed;
			Microservices.OnDeployFailed += HandleDeployFailed;
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

			int elementNumber = 0;
			foreach (IEntryModel model in entryModels)
			{
				bool wasPublished = EditorPrefs.GetBool(GetPublishedKey(model.Name), false);
				var remoteOnly = !MicroservicesDataModel.Instance.ContainsModel(model.Name);
				var newElement = new PublishManifestEntryVisualElement(model, wasPublished, elementNumber, remoteOnly);
				newElement.Refresh();
				_publishManifestElements.Add(model.Name, newElement);
				_scrollContainer.Add(newElement);

				elementNumber++;
			}

			_generalComments = Root.Q<TextField>("largeCommentsArea");
			_generalComments.AddPlaceholder("General comment");
			_generalComments.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

			_cancelButton = Root.Q<GenericButtonVisualElement>("cancelBtn");
			_cancelButton.OnClick += () => OnCloseRequested?.Invoke();

			_primarySubmitButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
			_primarySubmitButton.Button.clickable.clicked += HandlePrimaryButtonClicked;
			_topMessage = Root.Q<PublishStatusVisualElement>("topMessage");
			_topMessage.Refresh();

			SortServices();
		}

		private void SortServices()
		{
			int Comparer(VisualElement a, VisualElement b)
			{
				if (a is PublishManifestEntryVisualElement firstManifestElement &&
				    b is PublishManifestEntryVisualElement secondManifestElement)
				{
					return firstManifestElement.CompareTo(secondManifestElement);
				}

				return 0;
			}
			_scrollContainer.Sort(Comparer);
			
			var publishElements = _scrollContainer.Children();
			bool isOdd = false;
			foreach (VisualElement child in publishElements)
			{
				if (!(child is PublishManifestEntryVisualElement manifestEntryVisualElement))
					continue;

				manifestEntryVisualElement.SetOddRow(isOdd);
				isOdd = !isOdd;
			}
		}

		private void HandlePrimaryButtonClicked()
		{
			foreach (PublishManifestEntryVisualElement manifestEntryVisualElement in _publishManifestElements.Values)
				manifestEntryVisualElement.HandlePublishStarted();

			_topMessage.HandleSubmitClicked();
			_primarySubmitButton.SetText("Publishing...");
			_primarySubmitButton.Disable();
			ReplaceCommentWithLogger();
			OnSubmit?.Invoke(Model);
		}

		void ReplaceCommentWithLogger()
		{
			var parent = _generalComments.parent;
			_generalComments.RemoveFromHierarchy();
			var logger = new PublishLoggerVisualElement();
			parent.Add(logger);
			logger.Refresh();
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

		public void PrepareForPublish()
		{
			_mainLoadingBar.Hidden = false;

			foreach (KeyValuePair<string, PublishManifestEntryVisualElement> kvp in _publishManifestElements)
			{
				if (kvp.Value.IsRemoteOnly)
					continue;
				var serviceModel = MicroservicesDataModel.Instance.GetModel<ServiceModelBase>(kvp.Key);

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

		private void HandleServiceDeployStatusChanged(IDescriptor descriptor, ServicePublishState state)
		{
			_publishManifestElements[descriptor.Name]?.UpdateStatus(state);
			SortServices();
			switch (state)
			{
				case ServicePublishState.Failed:
					_primarySubmitButton.Enable();
					_mainLoadingBar.UpdateProgress(0, failed: true);
					foreach (KeyValuePair<string, PublishManifestEntryVisualElement> kvp in _publishManifestElements)
					{
						kvp.Value.LoadingBar.SetUpdater(null);
					}
					break;
			}
		}

		private void HandleServiceDeployProgress(IDescriptor descriptor)
		{
			_mainLoadingBar.Progress = CalculateProgress();
		}

		private float CalculateProgress()
		{
			return _publishManifestElements.Values.Where(element => !element.IsRemoteOnly)
			                               .Average(x => x.LoadingBar.Progress);
		}
	}
}
