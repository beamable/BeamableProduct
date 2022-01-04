using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Editor.UI.Buss;
using System;
using TMPro;
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

		private LabeledTextField _variableName;
		
		private LabeledColorPickerVisualElement _colorType;
		private LabeledColorPickerVisualElement _vertexColorType;
		private LabeledTextField _floatType;
		private LabeledTextField _floatFromFloatType;
		private LabeledDropdownVisualElement _enumType;
		private LabeledSpritePickerVisualElement _spriteType;
		private LabeledTextField _fontType;
		
		private readonly Dictionary<string, Tuple<BeamableVisualElement, IBussProperty>> _typesDict = new Dictionary<string, Tuple<BeamableVisualElement, IBussProperty>>();
		private IBussProperty _currentBussProperty;

		private List<Type> _allEnumTypes = new List<Type>();

		public override void Refresh()
		{
			base.Refresh();

			_variableName = Root.Q<LabeledTextField>("variableName");
			_variableName.Refresh();
			
			_colorType = Root.Q<LabeledColorPickerVisualElement>("colorType");
			_colorType.Refresh();
			_typesDict.Add("Color", new Tuple<BeamableVisualElement, IBussProperty>(_colorType, new SingleColorBussProperty()));

			_vertexColorType = Root.Q<LabeledColorPickerVisualElement>("vertexColorType");
			_vertexColorType.Refresh();
			_typesDict.Add("VertexColor", new Tuple<BeamableVisualElement, IBussProperty>(_vertexColorType, new VertexColorBussProperty()));
			
			_floatType = Root.Q<LabeledTextField>("floatType");
			_floatType.Refresh();
			_typesDict.Add("Float", new Tuple<BeamableVisualElement, IBussProperty>(_floatType, new FloatBussProperty()));
			
			_floatFromFloatType = Root.Q<LabeledTextField>("floatFromFloatType");
			_floatFromFloatType.Refresh();
			_typesDict.Add("FloatFromFloat", new Tuple<BeamableVisualElement, IBussProperty>(_floatFromFloatType, new FractionFloatBussProperty()));

			_allEnumTypes = Helper.GetAllClassesInheritedFrom(typeof(EnumBussProperty<>)).ToList();
			_enumType = Root.Q<LabeledDropdownVisualElement>("enumType");
			var enumProperties = Helper.GetAllClassesNamesInheritedFrom(typeof(EnumBussProperty<>));
			_enumType.Setup(enumProperties, HandleEnumSwitch);
			_enumType.Refresh();
			//_typesDict.Add("Enum", new Tuple<BeamableVisualElement, IBussProperty>(_enumType, new SdfModeBussProperty()));
			
			_spriteType = Root.Q<LabeledSpritePickerVisualElement>("spriteType");
			_spriteType.Refresh();
			_typesDict.Add("Sprite", new Tuple<BeamableVisualElement, IBussProperty>(_spriteType, new SpriteBussProperty()));
			
			_fontType = Root.Q<LabeledTextField>("fontType");
			_fontType.Refresh();
			_typesDict.Add("Font", new Tuple<BeamableVisualElement, IBussProperty>(_fontType, new FontBussAssetProperty()));

			var selectType = Root.Q<LabeledDropdownVisualElement>("selectType");
			selectType.Setup(_typesDict.Keys.ToList(), HandleChangeTypeVisibility);	
			selectType.Refresh();

			Root.Q<PrimaryButtonVisualElement>("confirmBtn").Button.clickable.clicked += HandleConfirmButton;
		}
		private void HandleChangeTypeVisibility(int visibleIndex)
		{
			int index = 0;
			foreach ((BeamableVisualElement item1, IBussProperty item2) in _typesDict.Values)
			{
				item1.EnableInClassList("hide",index != visibleIndex);
				if (index == visibleIndex)
					_currentBussProperty = item2;
				
				index++;
			}
		}
		
		private void HandleEnumSwitch(int index)
		{
			var type = _allEnumTypes[index];
			_currentBussProperty = Activator.CreateInstance(type) as IBussProperty;
			_typesDict["Enum"] = new Tuple<BeamableVisualElement, IBussProperty>(_enumType, _currentBussProperty);
		}

		private void HandleConfirmButton()
		{
			Debug.LogWarning($"{_variableName.Value} : {_currentBussProperty.GetType().Name}");
			
			IBussProperty result = _currentBussProperty;
			switch (_currentBussProperty)
			{
				case SingleColorBussProperty _: result = new SingleColorBussProperty(_colorType.SelectedColor); break;
				case VertexColorBussProperty _: result = new VertexColorBussProperty(_vertexColorType.SelectedColor); break;
				case FloatBussProperty _: result = new FloatBussProperty(); break;
				case FractionFloatBussProperty _: result = new FractionFloatBussProperty(); break;
				case SpriteBussProperty _: result = new SpriteBussProperty(_spriteType.SelectedSprite); break;
				case FontBussAssetProperty _: result = new FontBussAssetProperty(new TMP_FontAsset()); break;
			}
			
			BussStyleSheetUtility.TryAddProperty(new BussStyleDescription(), _variableName.Value, result, out var _);
		}
	}
}
