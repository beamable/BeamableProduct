using Beamable.Editor.UI.Components;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;
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
		Verifying // Checking if the image actually starts up correctly.
	}

	public class PublishManifestEntryVisualElement : MicroserviceComponent,
													 IComparable<PublishManifestEntryVisualElement>
	{
		public IEntryModel Model { get; }
		public int Index => _index;
		public bool IsRemote => _isRemote;
		public bool IsLocal => _isLocal;
		
		public ServicePublishState PublishState { get; private set; }
		public ILoadingBar LoadingBar
		{
			get
			{
				_loadingBar.Hidden = false;
				return _loadingBar;
			}
		}

		private Image _checkImage;
		private LoadingBarElement _loadingBar;
		private BeamableCheckboxVisualElement _enableState;
		private DropdownVisualElement _sizeDropdown;
		private TextField _commentField;
		private Label _stateLabel;
		private Label _serviceName;
		private VisualElement _knowLocationEntry;

		private string _currentPublishState;

		private readonly bool _wasPublished;
		private readonly int _index;
		private readonly bool _isRemote;
		private readonly bool _isLocal;

		// private static readonly string[] TemplateSizes = { "small", "medium", "large" };
		private static readonly Dictionary<ServicePublishState, string> CheckImageClasses =
			new Dictionary<ServicePublishState, string>
			{
				{ServicePublishState.Unpublished, "unpublished"},
				{ServicePublishState.Published, "published"},
				{ServicePublishState.InProgress, "publish-inProgress"},
				{ServicePublishState.Failed, "publish-failed"},
				{ServicePublishState.Verifying, "publish-inProgress"},
			};

		private readonly Dictionary<string, string> _serviceTypeToProperTypeName = new Dictionary<string, string>
		{
			{ "Microservice", "MicroService" }, 
			{ "mongov1", "StorageObject" }
		};

		public PublishManifestEntryVisualElement(IEntryModel model,
		                                         bool argWasPublished,
		                                         int elementIndex,
		                                         bool isLocal,
		                                         bool isRemote) : base(nameof(PublishManifestEntryVisualElement))
		{
			Model = model;
			_wasPublished = argWasPublished;
			_index = elementIndex;
			_isLocal = isLocal;
			_isRemote = isRemote;
		}

		public override void Refresh()
		{
			base.Refresh();

			_loadingBar = Root.Q<LoadingBarElement>();
			_loadingBar.SmallBar = true;
			_loadingBar.Hidden = true;
			_loadingBar.Refresh();

			_enableState = Root.Q<BeamableCheckboxVisualElement>("enableState");
			_enableState.Refresh();
			_enableState.SetWithoutNotify(_wasPublished);
			UpdateEnableState(_wasPublished);
			_enableState.OnValueChanged += UpdateEnableState;
			_enableState.tooltip = CHECKBOX_TOOLTIP;

			Root.Q<Image>("serviceIcon").AddToClassList(TryGetServiceProperTypeName(Model.Type));

			_serviceName = Root.Q<Label>("serviceName");
			_serviceName.text = Model.Name;
			_serviceName.RegisterCallback<GeometryChangedEvent>(OnLabelSizeChanged);

			_knowLocationEntry = Root.Q<VisualElement>("knowLocationEntry");
			var locationString = string.Empty;

			if (IsLocal)
			{
				var image = new Image { name = "locationLocal" };
				locationString += "Local";
				_knowLocationEntry.Add(image);
			}
			if (IsLocal && IsRemote)
			{
				var image = new Image { name = "separator"};
				locationString += " & ";
				_knowLocationEntry.Add(image);
			}
			if (IsRemote)
			{
				var image = new Image { name = "locationRemote" };
				locationString += "Remote";
				_knowLocationEntry.Add(image);
			}
			
			var locationLabel = new Label {name = "locationLabel", text = locationString};
			_knowLocationEntry.Add(locationLabel);

			if (Model is ManifestEntryModel serviceModel)
			{
				var dependencies = serviceModel.Dependencies.Select(dep => dep.id).ToList();
				Root.Q<ExpandableListVisualElement>("dependenciesList").Setup(dependencies);
			}
			
			// _sizeDropdown = Root.Q<DropdownVisualElement>("sizeDropdown");
			// _sizeDropdown.Setup(TemplateSizes.ToList(), null);
			// _sizeDropdown.Refresh();
			
			_commentField = Root.Q<TextField>("comment");
			_commentField.value = Model.Comment;
			_commentField.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);
			
			_stateLabel = Root.Q<Label>("status");
			UpdateStatus(ServicePublishState.Unpublished);
		}

		private void UpdateEnableState(bool isEnabled)
		{
			Model.Enabled = isEnabled;
			_enableState.EnableInClassList("enabled", isEnabled);
			_enableState.EnableInClassList("disabled", !isEnabled);
		}
		private void OnLabelSizeChanged(GeometryChangedEvent evt)
		{
			var width = evt.newRect.width;
			var maxCharacters = Mathf.CeilToInt(width / 10) - 1;

			if (Model.Name.TryEllipseText(maxCharacters, out string labelText))
			{
				_serviceName.text = labelText;
				_serviceName.tooltip = Model.Name;
				return;
			}

			_serviceName.tooltip = string.Empty;
			_serviceName.text = Model.Name;
		}
		public void HandlePublishStarted()
		{
			_enableState.SetEnabled(false);
			// _sizeDropdown.SetEnabled(false);
			_commentField.SetEnabled(false);
		}
		public void UpdateStatus(ServicePublishState state)
		{
			switch (state)
			{
				case ServicePublishState.Failed:
					_loadingBar.UpdateProgress(0, failed: true);
					_stateLabel.AddToClassList("error");
					_stateLabel.text = "FAILED";
					return;
				case ServicePublishState.Published:
					_stateLabel.text = "DONE";
					break;
				case ServicePublishState.InProgress:
					_stateLabel.text = "PUBLISHING";
					break;
				case ServicePublishState.Verifying:
					_stateLabel.text = "VERIFYING";
					break;
				default:
					_stateLabel.text = "READY";
					break;
			}

			PublishState = state;

			RemoveFromClassList(_currentPublishState);
			_currentPublishState = CheckImageClasses[state];
			AddToClassList(_currentPublishState);
		}
		public int CompareTo(PublishManifestEntryVisualElement other)
		{
			if (IsRemote)
				return 1;
			if (other.IsRemote)
				return -1;
			if (PublishState == other.PublishState)
				return Index.CompareTo(other.Index);

			return GetPublishStateOrder(PublishState).CompareTo(GetPublishStateOrder(other.PublishState));
		}
		public int GetPublishStateOrder(ServicePublishState state)
		{
			switch (state)
			{
				case ServicePublishState.Failed:
					return 0;
				case ServicePublishState.InProgress:
					return 1;
				case ServicePublishState.Verifying:
					return 2;
				case ServicePublishState.Unpublished:
					return 3;
				case ServicePublishState.Published:
					return 4;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}
		}
		private string TryGetServiceProperTypeName(string type) => _serviceTypeToProperTypeName.ContainsKey(type) ? _serviceTypeToProperTypeName[type] : type;
	}
}
