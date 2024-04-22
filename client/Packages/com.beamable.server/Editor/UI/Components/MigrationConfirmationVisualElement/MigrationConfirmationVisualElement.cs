using Beamable.Common;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Components;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
		private ProgressBar _progressBar;

		private List<IDescriptor> _allDescriptors;
		private CancellationTokenSource _cts;
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

			_progressBar = Root.Q<ProgressBar>("progressBar");
			_progressBar.value = 0;
			_progressBar.visible = false;

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
			_cts.Cancel();
			OnCancelled?.Invoke();
		}

		private void MigrateButton_OnClicked()
		{
			HandleDownload();
		}


		private void HandleDownload()
		{
			_progressBar.visible = true;
			_migrateBtn.Disable();

			_cts = new CancellationTokenSource();

			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
			_ = codeService.Migrate(_allDescriptors, (progress, message) =>
			{
				_progressBar.value = progress;
				_progressBar.title = message;

				if (progress >= 100)
				{
					OnClosed?.Invoke();
				}
			}, _cts.Token).Then(_ =>
			{
				_cts.Dispose();
			});
		}
	}
}
