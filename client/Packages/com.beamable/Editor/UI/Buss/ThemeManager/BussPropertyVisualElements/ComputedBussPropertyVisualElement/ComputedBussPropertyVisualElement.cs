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

			// var baseType = BussStyle.GetBaseType(_rootModel.PropertyProvider.Key);
			var baseType = _rootModel.PropertyProvider.GetInitialPropertyType();
			if (!BussStyle.TryGetOperatorBinding(baseType, out var operatorBinding))
			{
				Debug.LogError("Unknown compute properties");
				return;
			}
			
			// create field???
			var args = Property.Members.ToArray();

			var operationDropDown = new DropdownVisualElement();
			if (!operatorBinding.TryGetFactoryForOperatorType(Property.GetType(), out var initialFactory))
			{
				Debug.LogError("Unknown initial factory");
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
				
				// TODO: add undo record
				OnBeforeChange?.Invoke();
				var newProp = factory.factory?.Invoke();
				OnValueChanged?.Invoke(newProp);
				
			}, initialSelectedFactoryIndex);
			Root.Add(operationDropDown);
			operationDropDown.Refresh();
			AddBussPropertyFieldClass(operationDropDown);
			foreach (var arg in args)
			{
				
				var subModel = _rootModel.CreateChildModel(arg, prop =>
				{
					OnValueChanged?.Invoke(Property);
				});
				var elem = new StylePropertyVisualElement(subModel);
			
				Root.Add(elem);
				elem.Init();
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
