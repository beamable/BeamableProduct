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
			
			#region setup operation picker
			var operationDropDown = new DropdownVisualElement();
			if (!operatorBinding.TryGetDescriptorForOperatorType(Property.GetType(), out var initialDescriptor))
			{
				Debug.LogError("Unknown initial descriptor");
				return;
			}
			var names = operatorBinding.Descriptors.Select(f => f.name).ToList();
			var initialSelectedFactoryIndex = names.FindIndex(name => initialDescriptor.name == name);
			operationDropDown.Setup(names, i =>
			{
				var descriptor = operatorBinding.Descriptors[i];
				if (!operatorBinding.TryGetDescriptorForOperatorType(Property.GetType(), out var currDescriptor))
				{
					Debug.LogError("Unknown initial descriptor after selection");
				}

				if (currDescriptor == descriptor) return; // nothing...
				
				OnBeforeChange?.Invoke();
				var newProp = descriptor.factory?.Invoke();
				OnValueChanged?.Invoke(newProp);
				
			}, initialSelectedFactoryIndex);
			Root.Add(operationDropDown);
			operationDropDown.Refresh();
			AddBussPropertyFieldClass(operationDropDown);
			#endregion
			
			var subProperty = Property.GetComputedProperty(_rootModel.AppliedToElement.Style);
			var subElement = subProperty.GetVisualElement(null);

			var args = Property.Members.ToArray();
			foreach (var arg in args)
			{

				void UpdateComputedOutput()
				{
					if (subElement is IBussPropertyVisualElementSupportsPreview previewAbleElement)
					{
						subProperty = Property.GetComputedProperty(_rootModel.AppliedToElement.Style);
						previewAbleElement.SetValueFromProperty(subProperty);
					}
					OnValueChanged?.Invoke(Property);
				}
				
				var subModel = _rootModel.CreateChildModel(arg, prop =>
				{
					UpdateComputedOutput();
					OnValueChanged?.Invoke(Property);
				});
				var elem = new StylePropertyVisualElement(subModel);
				
				Root.Add(elem);
				elem.Init();
				
				var subBussVisualElements = elem.Query<BussPropertyVisualElement>().ToList();
				foreach (var subBussVisualElement in subBussVisualElements)
				{
					subBussVisualElement.onExternalChange += UpdateComputedOutput;
				}
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
