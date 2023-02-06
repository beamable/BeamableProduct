using Beamable.UI.Buss;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class ComputedBussPropertyVisualElement : BussPropertyVisualElement<IComputedProperty>
	{
		private readonly StylePropertyModel _rootModel;

		private List<ComputedPropertyArg> _args = new List<ComputedPropertyArg>();
		private List<BussPropertyVisualElement> _argElements = new List<BussPropertyVisualElement>();
		
		public ComputedBussPropertyVisualElement(IComputedProperty property, StylePropertyModel rootModel) : base(property)
		{
			_rootModel = rootModel;
		}

		public override void Init()
		{
			base.Init();

			var baseType = _rootModel.PropertyProvider.GetInitialPropertyType();
			if (!BussStyle.TryGetOperatorBinding(baseType, out var operatorBinding))
			{
				Debug.LogError("Unknown compute properties");
				return;
			}
			
			var args = Property.Members.ToArray();
			var operationDropDown = new DropdownVisualElement();
			if (!operatorBinding.TryGetFactoryForOperatorType(Property.GetType(), out var initialFactory))
			{
				Debug.LogError("Unknown initial factory");
				return;
			}
			var names = operatorBinding.Factories.Select(f => f.name).ToList();
			var initialSelectedFactoryIndex = names.FindIndex(name => initialFactory.name == name);
			operationDropDown.Setup(names, i =>
			{
				var factory = operatorBinding.Factories[i];
				if (!operatorBinding.TryGetFactoryForOperatorType(Property.GetType(), out var currFactory))
				{
					Debug.LogError("Unknown initial factory after selection");
				}

				if (currFactory == factory) return; // nothing...
				
				OnBeforeChange?.Invoke();
				var newProp = factory.factory?.Invoke();
				OnValueChanged?.Invoke(newProp);
				
			}, initialSelectedFactoryIndex);
			Root.Add(operationDropDown);
			operationDropDown.Refresh();
			AddBussPropertyFieldClass(operationDropDown);
			
			
			var subProperty = Property.GetComputedProperty(_rootModel.AppliedToElement.Style);
			var subElement = subProperty.GetVisualElement(null);

			
			foreach (var arg in args)
			{
				var subModel = _rootModel.CreateChildModel(arg, prop =>
				{
					if (subElement is IBussPropertyVisualElementSupportsPreview previewAble)
					{
						subProperty = Property.GetComputedProperty(_rootModel.AppliedToElement.Style);
						previewAble.SetValueFromProperty(subProperty);
					}
					OnValueChanged?.Invoke(Property);
				});
				var elem = new StylePropertyVisualElement(subModel);
				Root.Add(elem);
				elem.Init();
			}

			if (subElement is IBussPropertyVisualElementSupportsPreview previewAble)
			{
				subElement.Init();
				subElement.DisableInput("Computed value is not directly editable.");
				previewAble.SetValueFromProperty(subProperty);
				var outputRow = new VisualElement();
				outputRow.EnableInClassList("output-row", true);
				outputRow.Add(new Label("Output"));
				outputRow.Add(subElement);
				Root.Add(outputRow);
			}
			else
			{
				Root.Add(new Label("No available preview"));
			}

		}

		public override void OnPropertyChangedExternally()
		{
			foreach (var argElement in _argElements)
			{
				argElement.OnPropertyChangedExternally();
			}
		}
	}
}
