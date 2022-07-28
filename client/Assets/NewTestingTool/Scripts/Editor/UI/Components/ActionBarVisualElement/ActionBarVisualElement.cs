using Beamable.Editor.NewTestingTool.Models;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class ActionBarVisualElement : TestingToolComponent
	{
		public TestingEditorModel TestingEditorModel { get; private set;  }

		private Button _scanButton;
		
		public new class UxmlFactory : UxmlFactory<ActionBarVisualElement, UxmlTraits> { }
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{ name = "custom-text", defaultValue = "nada" };

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as ActionBarVisualElement;
			}
		}

		public ActionBarVisualElement() : base(nameof(ActionBarVisualElement)) { }

		public void Init(TestingEditorModel testingEditorModel)
		{
			TestingEditorModel = testingEditorModel;
		}
		
		public override void Refresh()
		{
			base.Refresh();

			_scanButton = Root.Q<Button>("scan");
			_scanButton.clickable.clicked += TestingEditorModel.Scan;
		}
	}
}
