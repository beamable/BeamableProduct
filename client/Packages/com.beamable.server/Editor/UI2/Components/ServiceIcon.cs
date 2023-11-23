using Beamable.Common;
using Beamable.Editor.UI.Model;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2.Components
{
	public class ServiceIcon : Image, IBeamoServiceElement
	{
		private string _customStatusClassName;
		public new class UxmlFactory : UxmlFactory<ServiceIcon, UxmlTraits> { }
		public void FeedData(IBeamoServiceDefinition model, BeamEditorContext context)
		{
			model.Updated -= HandleModelUpdate;
			model.Updated += HandleModelUpdate;
			HandleModelUpdate(model);
		}

		private void HandleModelUpdate(IBeamoServiceDefinition model)
		{
			ClearClassList();

			var statusClassName = string.IsNullOrWhiteSpace(_customStatusClassName)
				? model.IsRunningOnRemote == BeamoServiceStatus.Running
					? $"{model.ServiceType}_remoteEnabled"
					: $"{model.ServiceType}_remoteDisabled"
				: $"{model.ServiceType}_{_customStatusClassName}";

			tooltip = model.IsRunningOnRemote == BeamoServiceStatus.Running ? Constants.Tooltips.Microservice.ICON_REMOTE_RUNNING : Constants.Tooltips.Microservice.ICON_REMOTE_DISABLE;
			AddToClassList(statusClassName);
			AddToClassList(model.ServiceType.ToString());
		}
	}
}
