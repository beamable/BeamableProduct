using System;
using Beamable.Editor.UI.Components;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine.Assertions;
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

		protected BeamableBasicVisualElement(string ussPath)
		{
			Assert.IsTrue(File.Exists(ussPath), $"Cannot find {ussPath}");

			UssPath = ussPath;

			RegisterCallback<DetachFromPanelEvent>(evt =>
			{
				OnDetach();
			});
		}

		public virtual void Refresh() { }

		protected virtual void OnDestroy() { }

		protected virtual void OnDetach() { }

		public virtual void Init()
		{
			Destroy();
			Clear();

			this.AddStyleSheet(Files.COMMON_USS_FILE);
			this.AddStyleSheet(UssPath);

			Root = new VisualElement();
			Root.name = "root";
			Add(Root);

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

		public EditorWindow TryGetEditorWindow()
		{
			try { 
				var ownerProperty = panel.GetType().GetProperty("ownerObject");
				var owner = ownerProperty.GetValue(panel);
				var window = owner.GetType().BaseType.GetProperty("actualView", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
				return (EditorWindow)window;}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
