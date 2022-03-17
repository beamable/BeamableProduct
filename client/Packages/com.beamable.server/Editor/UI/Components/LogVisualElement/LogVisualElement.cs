using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

using static Beamable.Common.Constants;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class LogVisualElement : MicroserviceComponent
	{

		public new class UxmlFactory : UxmlFactory<LogVisualElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{ name = "custom-text", defaultValue = "nada" };

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as LogVisualElement;

			}
		}
		public ServiceModelBase Model { get; set; }
		public event Action OnDetachLogs;
		public bool EnableMoreButton = true;
		public bool EnableDetatchButton = true;

		private bool NoModel => Model == null;

		private ScrollView _scrollView;
		private VisualElement _detailView;
		private VisualElement _logWindowBody;
		private TextField _detailLabel;
		private int _scrollBlocker;
		private SearchBarVisualElement _searchLogBar;
		private Label _infoCountLbl;
		private Label _warningCountLbl;
		private Label _errorCountLbl;
		private Label _debugCountLbl;
		private Button _popupBtn;
		private Button _debugViewBtn;
		private Button _infoViewBtn;
		private Button _warningViewBtn;
		private Button _errorViewBtn;
		private Button _buildDropDown;
		private Button _advanceDropDown;
		private VisualElement _logListRoot;
		private ListView _listView;
		private string _statusClassName;

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (Model == null) return;
			Model.Logs.OnMessagesUpdated -= HandleMessagesUpdated;
			Model.Logs.OnSelectedMessageChanged -= UpdateSelectedMessageText;
			Model.Logs.OnViewFilterChanged -= LogsOnOnViewFilterChanged;
		}

		public override void Refresh()
		{
			base.Refresh();

			var clearButton = Root.Q<Button>("clear");
			clearButton.clickable.clicked += HandleClearButtonClicked;

			_advanceDropDown = Root.Q<Button>("advanceBtn");
			_advanceDropDown.tooltip = "More...";
			if (!NoModel)
			{
				var manipulator = new ContextualMenuManipulator(Model.PopulateMoreDropdown);
				manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
				_advanceDropDown.clickable.activators.Clear();
				_advanceDropDown.AddManipulator(manipulator);
			}

			_popupBtn = Root.Q<Button>("popupBtn");
			_popupBtn.clickable.clicked += OnPopoutButton_Clicked;
			_popupBtn.AddToClassList(Model.AreLogsAttached ? "attached" : "detached");
			_popupBtn.tooltip = Model.AreLogsAttached ? "Detach log container." : "Attach log container.";

			_infoCountLbl = Root.Q<Label>("infoCount");
			_warningCountLbl = Root.Q<Label>("warningCount");
			_errorCountLbl = Root.Q<Label>("errorCount");
			_debugCountLbl = Root.Q<Label>("debugCount");

			if (!NoModel)
			{
				_searchLogBar = Root.Q<SearchBarVisualElement>();
				_searchLogBar.SetValueWithoutNotify(Model.Logs.Filter);
				_searchLogBar.OnSearchChanged += Model.Logs.SetSearchLogFilter;

				_debugViewBtn = Root.Q<Button>("debug");
				_debugViewBtn.clickable.clicked += Model.Logs.ToggleViewDebugEnabled;
				_debugViewBtn.tooltip = Tooltips.Logs.ICON_DEBUG;

				_infoViewBtn = Root.Q<Button>("info");
				_infoViewBtn.clickable.clicked += Model.Logs.ToggleViewInfoEnabled;
				_infoViewBtn.tooltip = Tooltips.Logs.ICON_INFO;

				_warningViewBtn = Root.Q<Button>("warning");
				_warningViewBtn.clickable.clicked += Model.Logs.ToggleViewWarningEnabled;
				_errorViewBtn.tooltip = Tooltips.Logs.ICON_WARNING;

				_errorViewBtn = Root.Q<Button>("error");
				_errorViewBtn.clickable.clicked += Model.Logs.ToggleViewErrorEnabled;
				_errorViewBtn.tooltip = Tooltips.Logs.ICON_ERROR;
			}

			// Log
			_logListRoot = Root.Q("logListRoot");
			_listView = CreateListView();
			_logListRoot.Add(_listView);

			_detailView = Root.Q<VisualElement>("detailWindow");
			_detailLabel = _detailView.Q<TextField>();
			_detailLabel.multiline = true;
			_detailLabel.AddTextWrapStyle();

#if UNITY_2019_1_OR_NEWER
            _detailLabel.isReadOnly = true;
#elif UNITY_2018
            _detailLabel.OnValueChanged(evt => UpdateSelectedMessageText());
#endif

			_logWindowBody = Root.Q<VisualElement>("logWindowBody");
			_logWindowBody.Remove(_detailView);
			_logWindowBody.Remove(_logListRoot);
			_logWindowBody.AddSplitPane(_logListRoot, _detailView);
			_logWindowBody.SetEnabled(Model.Logs.FilteredMessages.Count > 0);

			_scrollView = _listView.Q<ScrollView>();
			_scrollView.AddToClassList("logScroller");

			if (!NoModel)
			{
				Model.Logs.HasScrolled = false;

				_scrollView.verticalScroller.valueChanged += VerticalScrollerOnvalueChanged;
				UpdateScroll();

				Model.Logs.OnMessagesUpdated -= HandleMessagesUpdated;
				Model.Logs.OnMessagesUpdated += HandleMessagesUpdated;

				Model.Logs.OnSelectedMessageChanged -= UpdateSelectedMessageText;
				Model.Logs.OnSelectedMessageChanged += UpdateSelectedMessageText;

				Model.Logs.OnViewFilterChanged -= LogsOnOnViewFilterChanged;
				Model.Logs.OnViewFilterChanged += LogsOnOnViewFilterChanged;

				LogsOnOnViewFilterChanged();
				UpdateSelectedMessageText();
			}

			if (!EnableDetatchButton)
			{
				_popupBtn.RemoveFromHierarchy();
			}

			if (!EnableMoreButton)
			{
				_advanceDropDown.RemoveFromHierarchy();
			}

			_listView.Refresh();
			UpdateCounts();
		}

		private void UpdateScroll()
		{
			EditorApplication.delayCall += () =>
			{
				_scrollView.verticalScroller.value = _scrollView.verticalScroller.highValue * Model.Logs.ScrollValue;
				_scrollView.MarkDirtyRepaint();
			};
		}

		private void OnPopoutButton_Clicked()
		{
			if (Model.AreLogsAttached)
			{
				Model.DetachLogs();
				OnDetachLogs?.Invoke();
			}
			else
			{
				Model.AttachLogs();
			}
			_popupBtn.tooltip = Model.AreLogsAttached ? "Detach log container." : "Attach log container.";
		}

		private void LogsOnOnViewFilterChanged()
		{
			void UpdateFilterButton(VisualElement el, bool active)
			{
				const string ACTIVE = "active";
				el.EnableInClassList(ACTIVE, active);
			}

			UpdateFilterButton(_debugViewBtn, Model.Logs.ViewDebugEnabled);
			UpdateFilterButton(_infoViewBtn, Model.Logs.ViewInfoEnabled);
			UpdateFilterButton(_warningViewBtn, Model.Logs.ViewWarningEnabled);
			UpdateFilterButton(_errorViewBtn, Model.Logs.ViewErrorEnabled);
		}

		private void UpdateSelectedMessageText()
		{
			var detailText = Model.Logs.Selected == null
				? string.Empty
				: $"{Model.Logs.Selected.Message}\n{Model.Logs.Selected.ParameterText}";
			_detailLabel.SetValueWithoutNotify(detailText);
		}

		private void HandleClearButtonClicked()
		{
			Model.Logs.Clear();

			EditorApplication.delayCall += () =>
			{
				_scrollView.verticalScroller.highValue = 0;
				_scrollView.verticalScroller.value = 0;
				Model.Logs.HasScrolled = false;
			};
		}

		private void HandleMessagesUpdated()
		{
			_logWindowBody.SetEnabled(Model.Logs.FilteredMessages.Count > 0);
			UpdateCounts();
			MaybeScrollToBottom();

			EditorApplication.delayCall += () =>
			{
				_listView.Refresh();
				_listView.MarkDirtyRepaint();
			};
		}

		private void VerticalScrollerOnvalueChanged(float value)
		{
			var scrollValue = _scrollView.verticalScroller.value;
			var highValue = _scrollView.verticalScroller.highValue;

			var tolerance = .001f;
			var isAtBottom = Math.Abs(scrollValue - highValue) < tolerance;

			if (_scrollBlocker == 0)
			{
				Model.Logs.HasScrolled = true;
				Model.Logs.ScrollValue = scrollValue / highValue;
				Model.Logs.IsTailingLog = isAtBottom;
			}
			else
			{
				if (Model.Logs.IsTailingLog)
				{
					MaybeScrollToBottom();
				}

				_scrollBlocker = 0;
			}
		}

		void MaybeScrollToBottom()
		{
			Model.Logs.IsTailingLog |= !Model.Logs.HasScrolled;

			if (!Model.Logs.IsTailingLog)
			{
				return; // don't do anything. We aren't tailing.
			}

			ScrollToWithoutNotify(1f); // always jump to the end.
		}

		void ScrollToWithoutNotify(float normalizedValue)
		{
			_scrollBlocker++;
			Model.Logs.ScrollValue = normalizedValue;
			UpdateScroll();
		}

		private ListView CreateListView()
		{
			var view = new ListView()
			{
				makeItem = CreateListViewElement,
				bindItem = BindListViewElement,
				selectionType = SelectionType.Single,
				itemHeight = 24,
				itemsSource = NoModel ? new List<LogMessage>() : Model.Logs.FilteredMessages
			};
			view.BeamableOnSelectionsChanged(ListView_OnSelectionChanged);
			view.Refresh();
			return view;
		}

		ConsoleLogVisualElement CreateListViewElement()
		{
			ConsoleLogVisualElement contentVisualElement = new ConsoleLogVisualElement();

			return contentVisualElement;
		}

		void BindListViewElement(VisualElement elem, int index)
		{
			ConsoleLogVisualElement consoleLogVisualElement = (ConsoleLogVisualElement)elem;
			consoleLogVisualElement.Refresh();
			consoleLogVisualElement.SetNewModel(_listView.itemsSource[index] as LogMessage);
			if (index % 2 == 0)
			{
				consoleLogVisualElement.RemoveFromClassList("oddRow");
			}
			else
			{
				consoleLogVisualElement.AddToClassList("oddRow");
			}
			consoleLogVisualElement.MarkDirtyRepaint();
		}

		private void UpdateCounts()
		{
			_infoCountLbl.text = NoModel ? "0" : Model.Logs.InfoCount.ToString();
			_debugCountLbl.text = NoModel ? "0" : Model.Logs.DebugCount.ToString();
			_warningCountLbl.text = NoModel ? "0" : Model.Logs.WarningCount.ToString();
			_errorCountLbl.text = NoModel ? "0" : (Model.Logs.ErrorCount + Model.Logs.FatalCount).ToString();
		}

		private void ListView_OnSelectionChanged(IEnumerable<object> objs)
		{
			if (objs != null && objs.FirstOrDefault() is LogMessage logMessage)
			{
				Model.Logs.SetSelectedLog(logMessage);
			}
		}

		public LogVisualElement() : base(nameof(LogVisualElement))
		{
		}
	}

}
