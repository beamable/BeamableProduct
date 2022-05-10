using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Beamable.Common.Content
{
	[Serializable]
	[Agnostic]
	public abstract class Optional
	{
		public bool HasValue;
		public abstract object GetValue();

		static Optional()
		{
			TypeDescriptor.AddAttributes(typeof(Optional), new TypeConverterAttribute(typeof(OptionalTypeConverter)));
		}

		public abstract void SetValue(object value);
		public abstract Type GetOptionalType();
	}

	public static class OptionalTypes
	{

		public static Optional<int> ToOptional(this int number)
		{
			return new Optional<int> { HasValue = true, Value = number };
		}
		public static Optional<double> ToOptional(this double number)
		{
			return new Optional<double> { HasValue = true, Value = number };
		}
	}

	[System.Serializable]
	[Agnostic]
	public class Optional<T> : Optional
	{
		public T Value;
		public override object GetValue()
		{
			return Value;
		}

		public override void SetValue(object value)
		{
			Value = (T)value;
			HasValue = true;
		}

		public override Type GetOptionalType()
		{
			return typeof(T);
		}

		public T GetOrThrow()
		{
			if (!HasValue) throw new ArgumentException("Optional value does not exist, but it was forced.");
			return Value;
		}
		public T GetOrElse(T otherwise) => GetOrElse(() => otherwise);

		public T GetOrElse(Func<T> otherwise)
		{
			if (HasValue) return Value;
			return otherwise();
		}

		public Optional<T> DoIfExists(Action<T> callback)
		{
			if (HasValue) callback(Value);
			return this;
		}
	}

	[Agnostic]
	public class OptionalTypeConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (typeof(Optional).IsAssignableFrom(destinationType))
			{
				return true;
			}
			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (value is long number)
			{
				return new OptionalInt { Value = (int)number, HasValue = true };
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalValue<T> : Optional<T> where T : struct
	{
		public static implicit operator T?(OptionalValue<T> option) => option is {HasValue: true} ? option.Value : null;
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalBoolean : OptionalValue<bool> { }

	public static class OptionalBooleanExtensions
	{
		public static bool IsTruthy(this OptionalBoolean self)
		{
			return self != null && (self.HasValue && self.Value);
		}
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalInt : OptionalValue<int> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalLong : OptionalValue<long> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalDouble : OptionalValue<double> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalListInt : Optional<List<int>> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalListString : Optional<List<string>> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalString : Optional<string>
	{
		public OptionalString()
		{

		}

		public OptionalString(string value)
		{
			Value = value;
			HasValue = true;
		}
	}

	[System.Serializable]
	public abstract class DisplayableList<T> : DisplayableList
	{
		public Type ElementType => typeof(T);
	}

	[System.Serializable]
	public abstract class DisplayableList : IList
	{
		protected abstract IList InternalList { get; }
		public abstract string GetListPropertyPath();

		public void CopyTo(Array array, int index) => InternalList.CopyTo(array, index);

		public int Count => InternalList.Count;
		public bool IsSynchronized => InternalList.IsSynchronized;
		public object SyncRoot => InternalList.SyncRoot;

		public IEnumerator GetEnumerator() => InternalList.GetEnumerator();

		public int Add(object value) => InternalList.Add(value);

		public void Clear() => InternalList.Clear();

		public bool Contains(object value) => InternalList.Contains(value);

		public int IndexOf(object value) => InternalList.IndexOf(value);

		public void Insert(int index, object value) => InternalList.Insert(index, value);

		public void Remove(object value) => InternalList.Remove(value);

		public void RemoveAt(int index) => InternalList.RemoveAt(index);

		public bool IsFixedSize => InternalList.IsFixedSize;
		public bool IsReadOnly => InternalList.IsReadOnly;

		public object this[int index]
		{
			get => InternalList[index];
			set => InternalList[index] = value;
		}
	}

	[System.Serializable]
	[Agnostic]
	public class KVPair
	{
		public string Key;
		public string Value;
		public string GetKey()
		{
			return Key;
		}

		public string GetValue()
		{
			return Value;
		}
	}
}
