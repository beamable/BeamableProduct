using System;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.Archive;

namespace Beamable.Editor.UI.Components
{
	public class ArchiveServicePopupVisualElement : BeamableVisualElement
	{
		private Label _contentLabelTop;
		private Label _contentLabelBottom;
		private PrimaryButtonVisualElement _okButton;
		private GenericButtonVisualElement _cancelButton;
		
		public Action onConfirm;
		public Action onClose;

		public ArchiveServicePopupVisualElement() : base(
			$"{Directories.COMMON_COMPONENTS_PATH}/{nameof(ArchiveServicePopupVisualElement)}/{nameof(ArchiveServicePopupVisualElement)}")
		{

		}

		public override void Refresh()
		{
			base.Refresh();

			_contentLabelTop = Root.Q<Label>("contentLabelTop");
			_contentLabelTop.text = ARCHIVE_WINDOW_INFO_TEXT_TOP;
			
			_contentLabelBottom = Root.Q<Label>("contentLabelBottom");
			_contentLabelBottom.text = ARCHIVE_WINDOW_INFO_TEXT_BOTTOM;
				
			_okButton = Root.Q<PrimaryButtonVisualElement>("okButton");
			_okButton.Button.clickable.clicked += HandleOkButtonClicked;

			_cancelButton = Root.Q<GenericButtonVisualElement>("cancelButton");
			_cancelButton.OnClick += HandleCancelButtonClicked;

		}

		public void SetCancelButtonText(string text)
		{
			_cancelButton.SetText(text);
		}

		public void SetConfirmButtonText(string text)
		{
			_okButton.SetText(text);
		}

		private void HandleOkButtonClicked()
		{
			onConfirm?.Invoke();
			onClose?.Invoke();
		}

		private void HandleCancelButtonClicked()
		{
			onClose?.Invoke();
		}
	}
}
