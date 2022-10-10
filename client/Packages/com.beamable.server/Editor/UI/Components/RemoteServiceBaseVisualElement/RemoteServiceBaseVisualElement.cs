#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public abstract class RemoteServiceBaseVisualElement : MicroserviceComponent
	{
		protected RemoteServiceBaseVisualElement() : base(nameof(RemoteServiceBaseVisualElement)) { }

		public override void Refresh()
		{
			base.Refresh();
			QueryVisualElements();
			UpdateVisualElements();
		}

		private void QueryVisualElements()
		{

		}

		private void UpdateVisualElements()
		{
		}
	}
}
