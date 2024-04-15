#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

using Beamable.Server.Editor;
using static Beamable.Common.Constants;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class MigrationServiceLinkVisualElement : MicroserviceComponent
	{
		public IDescriptor Model { get; set; }
		public VisualElement _checkIcon;

		public MigrationServiceLinkVisualElement() : base(nameof(MigrationServiceLinkVisualElement))
		{
		}

		public override void Refresh()
		{
			base.Refresh();

			var nameLbl = Root.Q<Label>("title");
			nameLbl.text = Model.AttributePath;

			_checkIcon = Root.Q<VisualElement>("checkIcon");
			_checkIcon.tooltip = Tooltips.ContentManager.UNCHECK;
		}

		// Content is downloaded.
		public void MarkChecked()
		{
			_checkIcon.RemoveFromClassList("unchecked");
			_checkIcon.AddToClassList("checked");
			_checkIcon.tooltip = Tooltips.ContentManager.CHECKED;
		}
	}
}
