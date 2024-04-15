using Beamable.Common;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Components;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public class MigrationConfirmationVisualElement : MicroserviceComponent
	{
		public event Action OnCancelled;
		public event Action OnClosed;

		private GenericButtonVisualElement _cancelBtn;
		private PrimaryButtonVisualElement _migrateBtn;

		private List<IDescriptor> _allDescriptors;
		protected Label _messageLabel;

		public MigrationConfirmationVisualElement(List<IDescriptor> allDescriptors) : base(nameof(MigrationConfirmationVisualElement))
		{
			_allDescriptors = allDescriptors;
		}

		public MigrationConfirmationVisualElement(string name) : base(name)
		{
		}

		public override void Refresh()
		{
			base.Refresh();
			SetMessageLabel();

			var label = Root.Q<Label>("listTitle");
			label.text = Constants.Migration.LIST_TITLE;

			_cancelBtn = Root.Q<GenericButtonVisualElement>("cancelBtn");
			_cancelBtn.OnClick += CancelButton_OnClicked;

			_migrateBtn = Root.Q<PrimaryButtonVisualElement>("downloadBtn");
			_migrateBtn.Button.clickable.clicked += MigrateButton_OnClicked;

			var servicesScroll = Root.Q<ScrollView>("servicesScroll");
			foreach (var descriptor in _allDescriptors)
			{
				var path = new Label(descriptor.AttributePath);
				servicesScroll.Add(path);
			}
		}

		protected virtual void SetMessageLabel()
		{
			_messageLabel = Root.Q<Label>("message");
			_messageLabel.text = Constants.Migration.POPUP_LABEL;
			_messageLabel.AddTextWrapStyle();
		}

		private void CancelButton_OnClicked()
		{
			OnCancelled?.Invoke();
		}

		private void MigrateButton_OnClicked()
		{
			OnClosed?.Invoke();
			HandleDownload();
		}


		private void HandleDownload()
		{
			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
			_ = codeService.Migrate(_allDescriptors);
		}
	}
}
