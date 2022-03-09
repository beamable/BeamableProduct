using Beamable.Editor.UI.Components;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
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
		private static readonly string[] TemplateSizes = { "small", "medium", "large" };
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
		private const string CHECKBOX_TOOLTIP = "Enable/disable the service";

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
		private BeamableCheckboxVisualElement _checkbox;
		private DropdownVisualElement _sizeDropdown;
		private TextField _commentField;
		private Label _stateLabel;

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

			_loadingBar = Root.Q<LoadingBarElement>();
			_loadingBar.SmallBar = true;
			_loadingBar.Hidden = true;
			_loadingBar.Refresh();

			_checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
			_checkbox.Refresh();
			_checkbox.SetWithoutNotify(!IsRemoteOnly && Model.Enabled);
			_checkbox.OnValueChanged += b => Model.Enabled = b;
			_checkbox.tooltip = CHECKBOX_TOOLTIP;

			_sizeDropdown = Root.Q<DropdownVisualElement>("sizeDropdown");
			_sizeDropdown.Setup(TemplateSizes.ToList(), null);
			_sizeDropdown.Refresh();

			var nameLabel = Root.Q<Label>("nameMS");
			nameLabel.text = Model.Name;

			_commentField = Root.Q<TextField>("commentsText");
			_commentField.value = Model.Comment;
			_commentField.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

			_stateLabel = Root.Q<Label>("stateLabel");

			var icon = Root.Q<Image>("typeImage");
			_checkImage = Root.Q<Image>("checkImage");

			if (Model is ManifestEntryModel serviceModel)
			{
				icon.AddToClassList(MICROSERVICE_IMAGE_CLASS);

				List<string> dependencies = new List<string>();
				foreach (var dep in serviceModel.Dependencies)
				{
					dependencies.Add(dep.id);
				}

				var depsList = Root.Q<ExpandableListVisualElement>("depsList");
				depsList.Setup(dependencies);
			}
			else
			{
				icon.AddToClassList(STORAGE_IMAGE_CLASS);
			}

			SetEnabled(!IsRemoteOnly);

			if (!IsRemoteOnly)
			{
				UpdateStatus(ServicePublishState.Unpublished);
			}
		}

		public void HandlePublishStarted()
		{
			_checkbox.SetEnabled(false);
			_sizeDropdown.SetEnabled(false);
			_commentField.SetEnabled(false);
		}

		public void SetOddRow(bool isOdd) => EnableInClassList("oddRow", isOdd);

		public void UpdateStatus(ServicePublishState state)
		{
			switch (state)
			{
				case ServicePublishState.Failed:
				{
					_loadingBar.UpdateProgress(0, failed: true);
					_stateLabel.text = "FAILED";
					return;
				}

				case ServicePublishState.Published:
				{
					_stateLabel.text = "DONE";
					break;
				}

				case ServicePublishState.InProgress:
				{
					_stateLabel.text = "PUBLISHING";
					break;
				}

				default:
				{
					_stateLabel.text = "READY";
					break;
				}
			}
			
			PublishState = state;

			RemoveFromClassList(_currentPublishState);
			_currentPublishState = CheckImageClasses[state];
			AddToClassList(_currentPublishState);
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
