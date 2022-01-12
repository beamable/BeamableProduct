using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public enum ServicePublishState
	{
		Unpublished,
		InProgress,
		Failed,
		Published,
	}

	public class PublishManifestEntryVisualElement : MicroserviceComponent,
	                                                 IComparable<PublishManifestEntryVisualElement>
	{
		private static readonly string[] TemplateSizes = {"small", "medium", "large"};
		private static readonly Dictionary<ServicePublishState, string> CheckImageClasses =
			new Dictionary<ServicePublishState, string>()
			{
				{ServicePublishState.Unpublished, "unpublished"},
				{ServicePublishState.Published, "published"},
				{ServicePublishState.InProgress, "publish-inProgress"},
				{ServicePublishState.Failed, "publish-failed"},
			};

		private const string MICROSERVICE_IMAGE_CLASS = "microserviceImage";
		private const string STORAGE_IMAGE_CLASS = "storageImage";

		public IEntryModel Model { get; }
		public int Index => _index;
		public bool IsRemoteOnly => _remoteOnly;
		public ServicePublishState PublishState { get; private set; }

		public ILoadingBar LoadingBar
		{
			get
			{
				_loadingBar.Hidden = false;
				return _loadingBar;
			}
		}

		private readonly bool _wasPublished;
		private readonly int _index;
		private readonly bool _remoteOnly;

		private Image _checkImage;
		private LoadingBarElement _loadingBar;
		private string _currentPublishState;
		private VisualElement _mainElement;

		public PublishManifestEntryVisualElement(IEntryModel model,
		                                         bool argWasPublished,
		                                         int elementIndex,
		                                         bool isRemoteOnly) : base(nameof(PublishManifestEntryVisualElement))
		{
			Model = model;
			_wasPublished = argWasPublished;
			_index = elementIndex;
			_remoteOnly = isRemoteOnly;
		}

		public override void Refresh()
		{
			base.Refresh();

			_mainElement = Root.Q<VisualElement>("mainContainer");
			_loadingBar = Root.Q<LoadingBarElement>();
			_loadingBar.SmallBar = true;
			_loadingBar.Hidden = true;
			_loadingBar.Refresh();

			var checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
			checkbox.Refresh();
			checkbox.SetWithoutNotify(Model.Enabled);
			checkbox.OnValueChanged += b => Model.Enabled = b;

			var sizeDropdown = Root.Q<DropdownVisualElement>("sizeDropdown");
			sizeDropdown.Setup(TemplateSizes.ToList(), null);
			sizeDropdown.Refresh();

			var nameLabel = Root.Q<Label>("nameMS");
			nameLabel.text = Model.Name;

			var commentField = Root.Q<TextField>("commentsText");
			commentField.value = Model.Comment;
			commentField.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

			var icon = Root.Q<Image>("typeImage");
			_checkImage = Root.Q<Image>("checkImage");

			if (Model is ManifestEntryModel serviceModel)
			{
				icon.AddToClassList(MICROSERVICE_IMAGE_CLASS);

				var microserviceModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(serviceModel.Name);

				if (microserviceModel != null && microserviceModel.Dependencies != null)
				{
					List<string> dependencies = new List<string>();
					foreach (var dep in microserviceModel.Dependencies)
					{
						dependencies.Add(dep.Name);
					}

					var depsList = Root.Q<ExpandableListVisualElement>("depsList");
					depsList.Setup(dependencies);
				}
			}
			else
			{
				icon.AddToClassList(STORAGE_IMAGE_CLASS);
			}

			UpdateStatus(_wasPublished ? ServicePublishState.Published : ServicePublishState.Unpublished);
			if (IsRemoteOnly)
			{
				SetEnabled(false);
				Root.tooltip = "Service is available on remote but is not present in local environment.";
			}
		}

		public void SetOddRow(bool isOdd)
		{
			_mainElement.RemoveFromClassList("oddRow");
			if (isOdd)
			{
				_mainElement.AddToClassList("oddRow");
			}
		}

		public void UpdateStatus(ServicePublishState state)
		{
			if (state == PublishState)
				return;
			PublishState = state;
			if (state == ServicePublishState.Failed)
			{
				_loadingBar.UpdateProgress(0, failed: true);
				return;
			}

			_checkImage.RemoveFromClassList(_currentPublishState);
			_currentPublishState = CheckImageClasses[state];
			_checkImage.AddToClassList(_currentPublishState);
		}

		public int CompareTo(PublishManifestEntryVisualElement other)
		{
			if (IsRemoteOnly)
				return 1;
			if (other.IsRemoteOnly)
				return -1;
			if (PublishState == other.PublishState)
				return Index.CompareTo(other.Index);

			return GetPublishStateOrder(PublishState).CompareTo(GetPublishStateOrder(other.PublishState));
		}

		private static int GetPublishStateOrder(ServicePublishState state)
		{
			switch (state)
			{
				case ServicePublishState.Failed:
					return 0;
				case ServicePublishState.InProgress:
					return 1;
				case ServicePublishState.Unpublished:
					return 2;
				case ServicePublishState.Published:
					return 3;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}
		}
	}
}
