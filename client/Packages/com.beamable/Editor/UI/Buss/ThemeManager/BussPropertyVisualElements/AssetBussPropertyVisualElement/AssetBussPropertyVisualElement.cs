using System;
using Beamable.UI.Buss;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Beamable.Editor.UI.Components
{
	public class AssetBussPropertyVisualElement : BussPropertyVisualElement<BaseAssetProperty>
	{
		public AssetBussPropertyVisualElement(BaseAssetProperty property) : base(property) { }
		
		private ObjectField _field;

		public override void Refresh()
		{
			base.Refresh();
			
			_field = new ObjectField();
			AddBussPropertyFieldClass(_field);
			_field.objectType = Property.GetAssetType();
			_field.value = Property.GenericAsset;
			_mainElement.Add(_field);

			_field.RegisterValueChangedCallback(OnValueChange);
		}

		private void OnValueChange(ChangeEvent<Object> evt)
		{
			Property.GenericAsset = evt.newValue;
		}
		
		public override void OnPropertyChangedExternally()
		{
			_field.value = Property.GenericAsset;
		}
	}
}
