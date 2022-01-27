using System.IO;
using UnityEditor;
using UnityEngine.Assertions;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Buss
{
	public class BeamableVisualElement : VisualElement
	{
		protected VisualTreeAsset TreeAsset { get; private set; }
		protected VisualElement Root { get; private set; }

		protected string UXMLPath { get; private set; }

		protected string USSPath { get; private set; }

		public BeamableVisualElement(string commonPath) : this(commonPath + ".uxml", commonPath + ".uss") { }

		public BeamableVisualElement(string uxmlPath, string ussPath)
		{
			Assert.IsTrue(File.Exists(uxmlPath), $"Cannot find {uxmlPath}");
			Assert.IsTrue(File.Exists(ussPath), $"Cannot find {ussPath}");
			UXMLPath = uxmlPath;
			USSPath = ussPath;
			TreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);

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
			}
			OnDestroy();
		}

		protected virtual void OnDestroy()
		{
			// Unregister any events...
		}

		public virtual void Refresh()
		{
			Destroy();
			Clear();

			Root = TreeAsset.CloneTree();

			this.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
			this.AddStyleSheet(USSPath);


			Add(Root);

			Root?.Query<VisualElement>(className: "--image-scale-to-fit").ForEach(elem =>
			{
				elem?.SetBackgroundScaleModeToFit();
			});
		}
	}
}
