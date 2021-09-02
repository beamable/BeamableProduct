using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Model;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class LogVisualElement : MicroserviceComponent
    {
        const string RmbWillCopyTextToClipboard = "RMB will copy text to clipboard";
        
        private Button _buildDropDown;
        private Button _advanceDropDown;

        private VisualElement _logListRoot;
        private ListView _listView;
        private string _statusClassName;

        public new class UxmlFactory : UxmlFactory<LogVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
                {name = "custom-text", defaultValue = "nada"};

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

        private TextField _nameTextField;
        private string _nameBackup;
        // private List<LogMessageModel> testLogList;
        private Label _statusLabel;
        private VisualElement _statusIcon;
        private VisualElement _remoteStatusIcon;
        private Label _remoteStatusLabel;
        private Button _popupBtn;

        private object _logVisualElement;
        private Button _startButton;
        private ScrollView _scrollView;
        private VisualElement _detailView;
        private Label _detailLabel;
        private int _scrollBlocker;
        private Label _infoCountLbl;
        private Label _warningCountLbl;
        private Label _errorCountLbl;
        private Label _debugCountLbl;
        private Button _debugViewBtn;
        private Button _infoViewBtn;
        private Button _warningViewBtn;
        private Button _errorViewBtn;
        public MicroserviceModel Model { get; set; }
        bool NoModel { get; set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(Model == null) return;
            Model.Logs.OnMessagesUpdated -= OnMessages_Updated;
            Model.Logs.OnMessagesUpdated -= OnMessages_Updated;
            Model.Logs.OnViewFilterChanged -= LogsOnOnViewFilterChanged;
        }

        public override void Refresh()
        {
            base.Refresh();
            NoModel = Model == null;
            var clearButton = Root.Q<Button>("clear");
            clearButton.clickable.clicked += OnClearButton_Clicked;

            _advanceDropDown = Root.Q<Button>("advanceBtn");
            if (!NoModel)
            {
                var manipulator = new ContextualMenuManipulator(Model.PopulateMoreDropdown);
                manipulator.activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
                _advanceDropDown.clickable.activators.Clear();
                _advanceDropDown.AddManipulator(manipulator);
            }

            _popupBtn = Root.Q<Button>("popupBtn");
            _popupBtn.clickable.clicked += OnPopoutButton_Clicked;

            _infoCountLbl = Root.Q<Label>("infoCount");
            _warningCountLbl = Root.Q<Label>("warningCount");
            _errorCountLbl = Root.Q<Label>("errorCount");
            _debugCountLbl = Root.Q<Label>("debugCount");
            UpdateCounts();

            if(!NoModel)
            {
                _debugViewBtn = Root.Q<Button>("debug");
                _debugViewBtn.clickable.clicked += Model.Logs.ToggleViewDebugEnabled;

                _infoViewBtn = Root.Q<Button>("info");
                _infoViewBtn.clickable.clicked += Model.Logs.ToggleViewInfoEnabled;

                _warningViewBtn = Root.Q<Button>("warning");
                _warningViewBtn.clickable.clicked += Model.Logs.ToggleViewWarningEnabled;

                _errorViewBtn = Root.Q<Button>("error");
                _errorViewBtn.clickable.clicked += Model.Logs.ToggleViewErrorEnabled;
            }


            // Log
            _logListRoot = Root.Q("logListRoot");
            _listView = CreateListView();
            _logListRoot.Add(_listView);

            _detailView = Root.Q<VisualElement>("detailWindow");
            _detailView.tooltip = RmbWillCopyTextToClipboard;
            _detailLabel = _detailView.Q<Label>();
#if UNITY_2019_1_OR_NEWER
            _detailLabel.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
#elif UNITY_2018
            _detailLabel.style.wordWrap = true;
#endif

            var detailManipulator = new ContextualMenuManipulator(HandleDetailLogClicked);
            detailManipulator.activators.Add(new ManipulatorActivationFilter {button = MouseButton.RightMouse});
            _detailView.AddManipulator(detailManipulator);

            var splitRoot = Root.Q<VisualElement>("logWindowBody");
            splitRoot.Remove(_detailView);
            splitRoot.Remove(_logListRoot);
            splitRoot.AddSplitPane(_logListRoot, _detailView);

            _scrollView = _listView.Q<ScrollView>();
            _scrollView.AddToClassList("logScroller");

            if (!NoModel)
            {
                Model.Logs.HasScrolled = false;
                _scrollView.verticalScroller.valueChanged += VerticalScrollerOnvalueChanged;


                EditorApplication.delayCall += () =>
                {
                    _scrollView.verticalScroller.highValue = _listView.itemHeight * _listView.itemsSource.Count;
                    _scrollView.verticalScroller.value = Model.Logs.ScrollValue;
                    _scrollView.MarkDirtyRepaint();
                };

                Model.Logs.OnMessagesUpdated -= OnMessages_Updated;
                Model.Logs.OnMessagesUpdated += OnMessages_Updated;

                Model.Logs.OnSelectedMessageChanged -= LogsOnOnSelectedMessageChanged;
                Model.Logs.OnSelectedMessageChanged += LogsOnOnSelectedMessageChanged;

                Model.Logs.OnViewFilterChanged -= LogsOnOnViewFilterChanged;
                Model.Logs.OnViewFilterChanged += LogsOnOnViewFilterChanged;

                LogsOnOnViewFilterChanged();
                LogsOnOnSelectedMessageChanged();
            }
            _listView.Refresh();
        }

        private void HandleDetailLogClicked(ContextualMenuPopulateEvent e)
        {
            Debug.Log($"Copied to clipboard: {_detailLabel.text}");
            EditorGUIUtility.systemCopyBuffer = _detailLabel.text;
        }

        private void OnPopoutButton_Clicked()
        {
            if (Model.AreLogsAttached)
            {
                Model.DetachLogs();
            }
            else
            {
                Model.AttachLogs();
            }
        }

        private void LogsOnOnViewFilterChanged()
        {
            const string ACTIVE = "active";
            _debugViewBtn.RemoveFromClassList(ACTIVE);
            _infoViewBtn.RemoveFromClassList(ACTIVE);
            _errorViewBtn.RemoveFromClassList(ACTIVE);
            _warningViewBtn.RemoveFromClassList(ACTIVE);
            if (Model.Logs.ViewDebugEnabled) _debugViewBtn.AddToClassList(ACTIVE);
            if (Model.Logs.ViewInfoEnabled) _infoViewBtn.AddToClassList(ACTIVE);
            if (Model.Logs.ViewWarningEnabled) _warningViewBtn.AddToClassList(ACTIVE);
            if (Model.Logs.ViewErrorEnabled) _errorViewBtn.AddToClassList(ACTIVE);
        }

        private void LogsOnOnSelectedMessageChanged()
        {
            if (Model.Logs.Selected == null)
            {
                _detailLabel.text = string.Empty;
                return;
            }

            var detailText = $"{Model.Logs.Selected.Message}\n{Model.Logs.Selected.ParameterText}";
            _detailLabel.text = detailText;
        }

        private void OnClearButton_Clicked()
        {
            Model.Logs.Clear();

            EditorApplication.delayCall += () =>
            {
                _scrollView.verticalScroller.highValue = 0;
                _scrollView.verticalScroller.value = 0;
                Model.Logs.HasScrolled = false;
            };
        }

        private void OnMessages_Updated()
        {
            _listView.Refresh();
            _listView.MarkDirtyRepaint();

            UpdateCounts();
            MaybeScrollToBottom();
        }

        private void VerticalScrollerOnvalueChanged(float value)
        {
            if (_scrollBlocker == 0)
            {
                Model.Logs.HasScrolled = true;
                Model.Logs.ScrollValue = value;

                var scrollValue = _scrollView.verticalScroller.value;
                var highValue = _scrollView.verticalScroller.highValue;

                var tolerance = .0001f;
                var isAtBottom = Math.Abs(scrollValue - highValue) < tolerance;

                Model.Logs.IsTailingLog = isAtBottom;
            }
            else
            {
                _scrollBlocker = 0;
            }
        }

        void MaybeScrollToBottom()
        {
            var highValue = _scrollView.verticalScroller.highValue;
            Model.Logs.IsTailingLog |= !Model.Logs.HasScrolled;


            if (!Model.Logs.IsTailingLog)
            {
                return; // don't do anything. We aren't tailing.
            }

            ScrollToWithoutNotify(_listView.itemHeight * _listView.itemsSource.Count); // always jump to the end.
        }

        void ScrollToWithoutNotify(float value)
        {
            EditorApplication.delayCall += () =>
            {
                _scrollBlocker++;
                _scrollView.scrollOffset = new Vector2(0, value);
                _scrollView.MarkDirtyRepaint();
            };

            Model.Logs.ScrollValue = value;
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