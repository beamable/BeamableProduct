using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Toolbox.Components
{
	public class SupportDropdownVisualElement : ToolboxComponent
	{
		public IToolboxViewService Model { get; set; }

		public SupportDropdownVisualElement() : base(nameof(SupportDropdownVisualElement))
		{ }

		public override void Refresh()
		{
			base.Refresh();
			// add two rows as testing
			var listRoot = Root.Q<VisualElement>("typeList");
			var allTypes = Enum.GetValues(typeof(SupportStatus)).Cast<SupportStatus>().ToList();

			SetTypesList(allTypes, listRoot);
		}

		private void SetTypesList(IEnumerable<SupportStatus> allTypes, VisualElement listRoot)
		{
			listRoot.Clear();
			foreach (var supportStatus in allTypes)
			{
				var typeName = supportStatus.Serialize();


				var row = new FilterRowVisualElement();
				row.OnValueChanged += nextValue =>
				{
					Model.SetSupportStatus(supportStatus, nextValue);
				};

				row.FilterName = typeName;
				row.Refresh();
				var isOrientationSupported = (Model.Query?.HasSupportConstraint ?? false)
				                             && Model.Query.FilterIncludes(supportStatus);
				row.SetValue(isOrientationSupported);

				listRoot.Add(row);
			}
		}
	}
}

