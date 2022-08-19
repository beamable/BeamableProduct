using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Components;
using System;
using System.Collections;
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
		static readonly SupportStatus[] AllTypes = Enum.GetValues(typeof(SupportStatus)).Cast<SupportStatus>().ToArray();
		private Hashtable _filterRows;
		public IToolboxViewService Model { get; set; }

		public SupportDropdownVisualElement() : base(nameof(SupportDropdownVisualElement))
		{ }

		public override void Refresh()
		{
			base.Refresh();
			// add two rows as testing
			var listRoot = Root.Q<VisualElement>("typeList");

			SetTypesList(AllTypes, listRoot);
		}

		private void SetTypesList(IEnumerable<SupportStatus> allTypes, VisualElement listRoot)
		{
			listRoot.Clear();
			_filterRows = new Hashtable();
			foreach (var supportStatus in allTypes)
			{
				var typeName = supportStatus.Serialize();

				
				var row = new FilterRowVisualElement();
				row.OnValueChanged += nextValue =>
				{
					Model.SetSupportStatus(supportStatus, nextValue, true);
					if(nextValue)
						DisableRest(supportStatus);
				};

				row.FilterName = typeName;
				row.Refresh();
				var isOrientationSupported = (Model.Query?.HasSupportConstraint ?? false)
				                             && Model.Query.FilterIncludes(supportStatus);
				row.SetValue(isOrientationSupported);

				listRoot.Add(row);
				_filterRows.Add(supportStatus,row);
			}
		}

		private void DisableRest(SupportStatus supportStatus)
		{
			foreach (var status in AllTypes)
			{
				if(!_filterRows.Contains(status)) continue;

				((FilterRowVisualElement)_filterRows[status]).SetValue(supportStatus == status);
			}
		}
	}
}

