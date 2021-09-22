using System;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss.Components
{
    public class ConfirmationPopupVisualElement : BeamableVisualElement
    {
        public event Action OnOKButtonClicked;
        public event Action OnCancelButtonClicked;
        private Label _headerLabel;
        private Label _bodyLabel;
        private Button _okButton;
        private Button _cancelButton;

        private readonly string _windowHeader;
        private readonly string _contentText;

        public ConfirmationPopupVisualElement(string windowHeader, string contentText) : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(ConfirmationPopupVisualElement)}/{nameof(ConfirmationPopupVisualElement)}")
        {
            _windowHeader = windowHeader;
            _contentText = contentText;
        }

        public override void Refresh()
        {
            base.Refresh();

            _headerLabel = Root.Q<Label>("headerLabel");
            _headerLabel.text = _windowHeader;

            _bodyLabel = Root.Q<Label>("contentLabel");
            _bodyLabel.text = _contentText;

            _okButton = Root.Q<Button>("okButton");
            _okButton.clickable.clicked += OkButton_OnClick;

            _cancelButton = Root.Q<Button>("cancelButton");
            _cancelButton.clickable.clicked += CancelButton_OnClick;
        }

        private void OkButton_OnClick()
        {
            OnOKButtonClicked?.Invoke();
        }

        private void CancelButton_OnClick()
        {
            OnCancelButtonClicked?.Invoke();
        }
    }
}