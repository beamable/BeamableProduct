using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Editor.UI.Buss;
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
		
		private LabeledColorPickerVisualElement _colorType;
		private LabeledColorPickerVisualElement _vertexColorType;
		private LabeledTextField _floatType;
		private LabeledTextField _floatFromFloatType;
		private LabeledDropdownVisualElement _enumType;
		private LabeledSpritePickerVisualElement _spriteType;
		private LabeledTextField _fontType;

		private readonly Dictionary<string, BeamableVisualElement> _typesDict = new Dictionary<string, BeamableVisualElement>();
		private BeamableVisualElement _currentElement;
		
		public override void Refresh()
		{
			base.Refresh();
			
			Root.Q<LabeledTextField>("variableName").Refresh();
			
			_colorType = Root.Q<LabeledColorPickerVisualElement>("colorType");
			_typesDict.Add("Color", _colorType);
			_colorType.Refresh();

			_vertexColorType = Root.Q<LabeledColorPickerVisualElement>("vertexColorType");
			_typesDict.Add("VertexColor", _vertexColorType);
			_vertexColorType.Refresh();
			
			_floatType = Root.Q<LabeledTextField>("floatType");
			_typesDict.Add("Float", _floatType);
			_floatType.Refresh();
			
			_floatFromFloatType = Root.Q<LabeledTextField>("floatFromFloatType");
			_typesDict.Add("FloatFromFloat", _floatFromFloatType);
			_floatFromFloatType.Refresh();
			
			_enumType = Root.Q<LabeledDropdownVisualElement>("enumType");
			_typesDict.Add("Enum", _enumType);
			var enumProperties = Helper.GetAllClassesNamesInheritedFrom(typeof(EnumBussProperty<>));
			_enumType.Setup(enumProperties, i => Debug.LogWarning(""));
			_enumType.Refresh();
			
			_spriteType = Root.Q<LabeledSpritePickerVisualElement>("spriteType");
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
