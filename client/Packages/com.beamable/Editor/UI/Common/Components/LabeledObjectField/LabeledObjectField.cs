using Beamable.Editor.UI.Common;
using System;
using Object = UnityEngine.Object;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants;

namespace Beamable.Editor.UI.Components
{
	public class LabeledObjectField : BeamableBasicVisualElement
	{
		private Action<Object> _onValueChanged;
		private Label _labelComponent;
		private ObjectField _objectFieldComponent;
		private Type _value;

		private string Label { get; set; }
		private Type Type { get; set; }

		public LabeledObjectField() : base(
			$"{Directories.COMMON_COMPONENTS_PATH}/{nameof(LabeledObjectField)}/{nameof(LabeledObjectField)}.uss") { }

		public override void Init()
		{
			base.Init();

			_labelComponent = new Label(Label) {name = "label"};
			Root.Add(_labelComponent);

			_objectFieldComponent = new ObjectField {name = "objectField"};
			_objectFieldComponent.objectType = Type;
			_objectFieldComponent.allowSceneObjects = false;
			_objectFieldComponent.RegisterValueChangedCallback(ValueChanged);
			Root.Add(_objectFieldComponent);
		}

		public void Setup(string label, Type type, Action<Object> onValueChanged)
		{
			Label = label;
			Type = type;
			_onValueChanged = onValueChanged;
			
			Init();
		}

		protected override void OnDestroy()
		{
			_objectFieldComponent.UnregisterValueChangedCallback(ValueChanged);
		}

		private void ValueChanged(ChangeEvent<Object> changeEvent)
		{
			_onValueChanged.Invoke(changeEvent.newValue);
		}

		public void SetValue(Object newObject)
		{
			_objectFieldComponent.value = newObject;
		}

		public void Reset()
		{
			_objectFieldComponent.value = null; 
		}
	}
}
