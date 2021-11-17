using System.Collections.Generic;
using System;
using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
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
	public class PublishPopup : MicroserviceComponent
	{
		public new class UxmlFactory : UxmlFactory<PublishPopup, UxmlTraits> { }

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
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
		private Button _cancelButton;
		private PrimaryButtonVisualElement _continueButton;
		private ScrollView _scrollContainer;
		private Dictionary<string, PublishManifestEntryVisualElement> _publishManifestElements;
		private bool _isPublishDisabled;

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
				return;

			_scrollContainer = Root.Q<ScrollView>("manifestsContainer");
			_publishManifestElements = new Dictionary<string, PublishManifestEntryVisualElement>(Model.Services.Count);

			List<IEntryModel> entryModels = new List<IEntryModel>(Model.Services.Values);
			entryModels.AddRange(Model.Storages.Values);

			bool isOddRow = true;
			_isPublishDisabled = false;
			foreach (var model in entryModels)
			{
				if (!MicroserviceConfiguration.Instance.EnableStoragePreview && model is StorageEntryModel)
				{
					continue;
				}

				if (MicroserviceConfiguration.Instance.EnableStoragePreview)
				{
					DisablePublishFeature(
						"Warning! In order to publish services you must disable Storage Preview first.");
				}

				if (model is ManifestEntryModel)
				{
					var serviceModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(model.Name);
					if (serviceModel != null && !(serviceModel is RemoteMicroserviceModel) &&
					    serviceModel.Descriptor.IsPublishFeatureDisabled())
					{
						DisablePublishFeature(
							"Warning! Publish feature is disabled due to Microservices dependent on Storage Objects.");
					}
				}

				bool wasPublished = EditorPrefs.GetBool(GetPublishedKey(model.Name), false);
				var newElement = new PublishManifestEntryVisualElement(model, wasPublished, isOddRow);
				newElement.Refresh();
				_publishManifestElements.Add(model.Name, newElement);
				_scrollContainer.Add(newElement);

				isOddRow = !isOddRow;
			}

			_generalComments = Root.Q<TextField>("largeCommentsArea");
			_generalComments.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

			_cancelButton = Root.Q<Button>("cancelBtn");
			_cancelButton.clickable.clicked += () => OnCloseRequested?.Invoke();

			_continueButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
			_continueButton.Button.clickable.clicked += () => OnSubmit?.Invoke(Model);
			_continueButton.SetEnabled(!_isPublishDisabled);
		}

		public void PrepareForPublish()
		{
			Root.Q<VisualElement>("header").RemoveFromHierarchy();
			_generalComments.RemoveFromHierarchy();
			_continueButton.RemoveFromHierarchy();
			_cancelButton.RemoveFromHierarchy();
			foreach (var kvp in _publishManifestElements)
				kvp.Value.RemoveFromHierarchy();
			_publishManifestElements.Clear();

			foreach (var kvp in Model.Services)
			{
				var microserviceModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(kvp.Value.Name);

				if (microserviceModel == null)
				{
					Debug.LogError($"Cannot find model: {microserviceModel}");
					continue;
				}

				var newElement = new LoadingBarElement();
				newElement.Refresh();
				_scrollContainer.Add(newElement);
				new DeployMSLogParser(newElement, microserviceModel, true);
			}
		}

		public void ServiceDeployed(IDescriptor descriptor)
		{
			EditorPrefs.SetBool(GetPublishedKey(descriptor.Name), true);
		}

		private string GetPublishedKey(string serviceName) =>
			string.Format(Microservices.SERVICE_PUBLISHED_KEY, serviceName);

		private void DisablePublishFeature(string message)
		{
			Root.Q<Label>("warningMessage").text = message;
			_isPublishDisabled = true;
		}
	}
}
