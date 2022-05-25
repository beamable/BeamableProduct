using Beamable.Editor.UI.Components;
using System.IO;
using UnityEngine.Assertions;
using Beamable.Common.Dependencies;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants;

namespace Beamable.Editor.UI.Common
{
	public class BeamableBasicVisualElement : VisualElement
	{
		protected VisualElement Root { get; set; }
		protected string UssPath { get; }
		private readonly bool _createRoot;

		public IDependencyProvider provider;

		protected BeamableBasicVisualElement(string ussPath, bool createRoot = true)
		{
			Assert.IsTrue(File.Exists(ussPath), $"Cannot find {ussPath}");

			UssPath = ussPath;
			_createRoot = createRoot;

			provider = BeamEditorContext.Default.ServiceScope;

			RegisterCallback<DetachFromPanelEvent>(evt =>
			{
				OnDetach();
			});
		}

		public void Refresh(IDependencyProvider provider)
		{
			this.provider = provider;

			Refresh();
		}

		public virtual void Refresh() { }

		protected virtual void OnDestroy() { }

		protected virtual void OnDetach() { }

		public virtual void Init()
		{
			Clear();

			this.AddStyleSheet(Files.COMMON_USS_FILE);
			this.AddStyleSheet(UssPath);

			if (_createRoot)
			{
				Root = new VisualElement();
				Root.name = "root";
				Add(Root);
			}
			else
			{
				Root = this;
			}

			this?.Query<VisualElement>(className: "--image-scale-to-fit").ForEach(elem =>
			{
				elem?.SetBackgroundScaleModeToFit();
			});
		}

		public void Destroy()
		{
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
	}
}
