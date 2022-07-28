using Beamable.NewTestingTool.Core.Models;
using UnityEngine.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class RegisteredTestVisualElement : TestingToolComponent
	{
		public RegisteredTest RegisteredTest { get; set; }
		
		private Label _testableName;
		
		public new class UxmlFactory : UxmlFactory<RegisteredTestVisualElement, UxmlTraits> { }

		public RegisteredTestVisualElement() : base(nameof(RegisteredTestVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();
			_testableName = Root.Q<Label>("testableName");
			_testableName.text = RegisteredTest.TestClassName;
		}
	}
}
