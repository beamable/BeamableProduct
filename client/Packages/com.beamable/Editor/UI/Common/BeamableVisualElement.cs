using Beamable.Editor.UI.Common;
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

namespace Beamable.Editor.UI.Components
{
	public class BeamableVisualElement : BeamableBasicVisualElement
	{
		private VisualTreeAsset TreeAsset { get; }

		private string UxmlPath { get; }

		protected BeamableVisualElement(string commonPath) : this(commonPath + ".uxml", commonPath + ".uss") { }

		private BeamableVisualElement(string uxmlPath, string ussPath) : base(ussPath)
		{
			Assert.IsTrue(File.Exists(uxmlPath), $"Cannot find {uxmlPath}");

			UxmlPath = uxmlPath;
			TreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);

			RegisterCallback<DetachFromPanelEvent>(evt =>
			{
				OnDetach();
			});
		}

		public override void Refresh()
		{
			Destroy();
			Clear();

			Root = TreeAsset.CloneTree();

			this.AddStyleSheet(BeamableComponentsConstants.COMMON_USS_PATH);
			this.AddStyleSheet(UssPath);

			Add(Root);

			Root?.Query<VisualElement>(className: "--image-scale-to-fit").ForEach(elem =>
			{
				elem?.SetBackgroundScaleModeToFit();
			});
		}
	}
}
