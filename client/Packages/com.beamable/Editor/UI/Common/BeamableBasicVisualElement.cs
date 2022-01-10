using Beamable.Editor.UI.Buss;
using System.IO;
using UnityEngine.Assertions;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Common
{
	public class BeamableBasicVisualElement : VisualElement
	{
		protected VisualElement Root
		{
			get;
			set;
		}

		protected string USSPath
		{
			get;
		}

		public BeamableBasicVisualElement(string ussPath)
		{
			Assert.IsTrue(File.Exists(ussPath), $"Cannot find {ussPath}");

			USSPath = ussPath;

			RegisterCallback<DetachFromPanelEvent>(evt =>
			{
				OnDetach();
			});
		}

		public virtual void OnDetach()
		{
			// Do any sort of cleanup
		}

		public void Destroy()
		{
			// call OnDestroy on all child elements.
			foreach (var child in Children())
			{
				if (child is BeamableVisualElement beamableChild)
				{
					beamableChild.Destroy();
				}

				if (child is BeamableBasicVisualElement beamableBasicChild)
				{
					beamableBasicChild.Destroy();
				}
			}

			OnDestroy();
		}

		protected virtual void OnDestroy() { }

		public virtual void Refresh()
		{
			Destroy();
			Clear();

			this.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
			this.AddStyleSheet(USSPath);

			Root = new VisualElement().WithName("root");
			Add(Root);

			this?.Query<VisualElement>(className: "--image-scale-to-fit").ForEach(elem =>
			{
				elem?.SetBackgroundScaleModeToFit();
			});
		}
	}
}
