using System;
using Beamable.Editor.UI.Components;
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
        private Label _headerLabel;
        private Label _bodyLabel;
        private PrimaryButtonVisualElement _okButton;
        private Button _cancelButton;

        private readonly string _contentText;
        private readonly Action _onConfirm;
        private readonly Action _onClose;

        public ConfirmationPopupVisualElement(string contentText, Action onConfirm, Action onClose) : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(ConfirmationPopupVisualElement)}/{nameof(ConfirmationPopupVisualElement)}")
        {
            _contentText = contentText;
            _onConfirm = onConfirm;
            _onClose = onClose;
        }

        public override void Refresh()
        {
            base.Refresh();

            _bodyLabel = Root.Q<Label>("contentLabel");
            _bodyLabel.text = _contentText;

            _okButton = Root.Q<PrimaryButtonVisualElement>("okButton");
            _okButton.Button.clickable.clicked += OkButton_OnClick;

            _cancelButton = Root.Q<Button>("cancelButton");
            _cancelButton.clickable.clicked += CancelButton_OnClick;
        }

        private void OkButton_OnClick()
        {
            _onConfirm?.Invoke();
            _onClose?.Invoke();
        }

        private void CancelButton_OnClick()
        {
            _onClose?.Invoke();
        }
    }
}