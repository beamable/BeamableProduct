using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Beamable.Editor.NewTestingTool.UI.Components
{
	public class ColumnListVisualElement : TestingToolComponent
	{
		public new class UxmlFactory : UxmlFactory<ColumnListVisualElement, UxmlTraits> { }

		public ColumnListVisualElement() : base(nameof(ColumnListVisualElement)) { }
		
		public override void Refresh()
		{
			base.Refresh();
		}
	}
}
