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
		private GenericButtonVisualElement  _cancelButton;
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

			Microservices.OnServiceDeployStatusChanged -= HandleServiceServiceDeployStatusChange;
			Microservices.OnServiceDeployStatusChanged += HandleServiceServiceDeployStatusChange;
			Microservices.OnServiceDeployProgress -= HandleServiceDeployProgress;
			Microservices.OnServiceDeployProgress += HandleServiceDeployProgress;
			OnSubmit -= SubmitClicked;
			OnSubmit += SubmitClicked;

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

			_primarySubmitButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
			_primarySubmitButton.Button.clickable.clicked += () => OnSubmit?.Invoke(Model);
			_topMessage = Root.Q<PublishStatusVisualElement>("topMessage");
			_topMessage.Refresh();
			OnSubmit -= _topMessage.HandleSubmitClicked;
			OnSubmit += _topMessage.HandleSubmitClicked;
			
			HandleServiceServiceDeployStatusChange(null, ServicePublishState.Unpublished);
		}

		private void SubmitClicked(ManifestModel _) => _primarySubmitButton.Disable();

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
				case ServicePublishState.InProgress:
					return;
				case ServicePublishState.Failed:
					_primarySubmitButton.Enable();
					_mainLoadingBar.UpdateProgress(0, failed: true);
					foreach (KeyValuePair<string, PublishManifestEntryVisualElement> kvp in _publishManifestElements)
					{
						kvp.Value.LoadingBar.SetUpdater(null);
					}
					break;
				case ServicePublishState.Published:
					_primarySubmitButton.Enable();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}
			_publishManifestElements[descriptor.Name].UpdateStatus(state);
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
