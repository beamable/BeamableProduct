using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
namespace Beamable.Editor.Toolbox.Components
{
	public class NewVariableVisualElement : BeamableVisualElement
	{
		public NewVariableVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_COMPONENTS_PATH}/{nameof(NewVariableVisualElement)}/{nameof(NewVariableVisualElement)}")
		{
		}
		
		private LabeledTextField _colorType;
		private LabeledTextField _vertexColorType;
		private LabeledTextField _floatType;
		private LabeledTextField _floatFromFloatType;
		private LabeledTextField _enumType;
		private LabeledTextField _spriteType;
		private LabeledTextField _fontType;

		private readonly Dictionary<string, BeamableVisualElement> _typesDict = new Dictionary<string, BeamableVisualElement>();
		private BeamableVisualElement _currentElement;
		
		public override void Refresh()
		{
			base.Refresh();
			
			Root.Q<LabeledTextField>("variableName").Refresh();
			
			_colorType = Root.Q<LabeledTextField>("colorType");
			_typesDict.Add("Color", _colorType);
			_colorType.Refresh();

			_vertexColorType = Root.Q<LabeledTextField>("vertexColorType");
			_typesDict.Add("VertexColor", _vertexColorType);
			_vertexColorType.Refresh();
			
			_floatType = Root.Q<LabeledTextField>("floatType");
			_typesDict.Add("Float", _floatType);
			_floatType.Refresh();
			
			_floatFromFloatType = Root.Q<LabeledTextField>("floatFromFloatType");
			_typesDict.Add("FloatFromFloat", _floatFromFloatType);
			_floatFromFloatType.Refresh();
			
			_enumType = Root.Q<LabeledTextField>("enumType");
			_typesDict.Add("Enum", _enumType);
			_enumType.Refresh();
			
			_spriteType = Root.Q<LabeledTextField>("spriteType");
			_typesDict.Add("Sprite", _spriteType);
			_spriteType.Refresh();
			
			_fontType = Root.Q<LabeledTextField>("fontType");
			_typesDict.Add("Font", _fontType);
			_fontType.Refresh();

			var selectType = Root.Q<LabeledDropdownVisualElement>("selectType");
			selectType.Setup(_typesDict.Keys.ToList(), HandleChangeTypeVisibility);	
			selectType.Refresh();
		}
		private void HandleChangeTypeVisibility(int visibleIndex)
		{
			int index = 0;
			foreach (var element in _typesDict.Values)
			{
				element.EnableInClassList("hide",index != visibleIndex);
				index++;
			}
		}
	}
}
