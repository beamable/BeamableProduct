using Beamable.Editor.Toolbox.Components;
using Beamable.Editor.UI.Buss;
using System;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Toolbox.Models
{
	public class StorageDepencencyWarningModel : AnnouncementModelBase
	{
		public string TitleLabelText => "PREVIEW FEATURE";

		public string DescriptionLabelText =>
			"Deployment of Microservices with Storage Object Dependencies will not be possible until a future version of Beamable.";

		public Action OnIgnore;

		public override BeamableVisualElement CreateVisualElement()
		{
			return new StorageDepencencyWarningVisualElement() { StorageDepencencyWarningModel = this };
		}
	}
}
