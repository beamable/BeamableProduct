using Beamable.UI.Sdf;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public interface IComputedProperty : IBussProperty
	{
		IEnumerable<ComputedPropertyArg> Members { get; }
		IBussProperty GetComputedProperty(BussStyle style);
	}

	public interface IComputedProperty<T> : IComputedProperty
	{
		T GetComputedValue(BussStyle style);
	}
	

	[Serializable]
	public class ComputedPropertyArg : ISerializationCallbackReceiver
	{
		public static ComputedPropertyArg Create<T>(string name)
			where T : IBussProperty, new() =>
			Create(name, () => new T());

		public static ComputedPropertyArg Create<T>(string name, Func<T> factory)
			where T : IBussProperty
		{
			return new ComputedPropertyArg(name, factory());
		}

		[SerializeField]
		private string _name;
		public string Name => _name;
		public IBussProperty Property { get; private set; }
		
		public IBussProperty TemplateProperty { get; private set; }
		
		[SerializeField]
		private SerializableValueObject _serializable;

		/// <summary>
		/// The template property's job is to remember the initial value for this argument.
		/// </summary>
		[SerializeField]
		private SerializableValueObject _templateProperty;
		
		public ComputedPropertyArg(string name, IBussProperty arg)
		{
			_name = name;
			Property = arg;
			TemplateProperty = arg.CopyProperty();
			_serializable = new SerializableValueObject();
			_serializable.Set(Property);
			_templateProperty = new SerializableValueObject();
			_templateProperty.Set(TemplateProperty);
		}

		public void SetProperty(IBussProperty property)
		{
			Property = property;
			_serializable.Set(property);
		}

		public bool TryGetProperty<T>(BussStyle style, out T property) where T : class, IBussProperty
		{
			if (Property is VariableProperty variableProperty)
			{
				return style.TryGetFromVariable(variableProperty.VariableName, out property);
			}
			else if (Property is T typedProperty)
			{
				property = typedProperty;
				return true;
			}

			property = default(T);
			return false;
		}

		public void OnBeforeSerialize()
		{
			_serializable.Set(Property);
			_templateProperty.Set(TemplateProperty);
		}

		public void OnAfterDeserialize()
		{
			Property = _serializable.Get() as IBussProperty;
			TemplateProperty = _templateProperty.Get() as IBussProperty;
		}
	}
}
