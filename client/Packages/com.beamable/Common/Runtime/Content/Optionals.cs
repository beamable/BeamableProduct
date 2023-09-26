using Beamable.Common.Shop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

#pragma warning disable CS0618

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
	[DebuggerDisplay("{HasValue ? (Value == null ? \"no value\" : Value.ToString()) : \"no value\"}")]
	public class Optional<T> : Optional
	{
		public static implicit operator T(Optional<T> option) => option?.HasValue == true ? (T)option.Value : default(T);
		public static implicit operator Optional<T>(T val)
		{
			var x = new Optional<T>();
			x.Value = val;
			x.HasValue = true;
			return x;
		}

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

		public virtual void Set(T value)
		{
			Value = value;
			HasValue = true;
		}

		/// <summary>
		/// Erase the value of the Optional, and mark the instance such that the result from the <see cref="Optional.HasValue"/> property be false.
		/// </summary>
		public virtual void Clear()
		{
			HasValue = false;
			Value = default;
		}

		public override Type GetOptionalType()
		{
			return typeof(T);
		}

		public T GetOrThrow() => GetOrThrow(null);

		public T GetOrThrow(Func<Exception> exFactory)
		{
			if (!HasValue) throw exFactory?.Invoke() ?? new ArgumentException("Optional value does not exist, but it was forced.");
			return Value;
		}

		public T GetOrElse(T otherwise) => GetOrElse(() => otherwise);

		public T GetOrElse(Func<T> otherwise)
		{
			if (HasValue) return Value;
			return otherwise();
		}

		public TResultOptional Map<TResult, TResultOptional>(Func<T, TResult> func, bool allowNull = false)
			where TResultOptional : Optional<TResult>, new()
			// where TResult
		{
			var result = new TResultOptional();
			if (HasValue)
			{
				var resultVal = func(Value);
				if (allowNull || resultVal != null)
				{
					result.Set(resultVal);
				}
			}
			return result;
		}

		public Optional<T> DoIfExists(Action<T> callback)
		{
			if (HasValue) callback(Value);
			return this;
		}

		public Optional<T> DoIfNotExists(Action callback)
		{
			if (!HasValue) callback();
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
	[DebuggerDisplay("{HasValue ? Value.ToString() : \"no value\"}")]
	public class OptionalValue<T> : Optional<T> where T : struct
	{
		public static implicit operator T?(OptionalValue<T> option) => option?.HasValue == true ? (T?)option.Value : null;
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
	public class OptionalBool : OptionalValue<bool>
	{
		public OptionalBool() { }

		public OptionalBool(bool value)
		{
			Value = value;
			HasValue = true;
		}
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalInt : OptionalValue<int>
	{
		public OptionalInt() { }

		public OptionalInt(int value)
		{
			Value = value;
			HasValue = true;
		}
	}

	[System.Serializable]
	public class OptionalObject : Optional<object> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalLong : OptionalValue<long>
	{
		public OptionalLong()
		{

		}

		public OptionalLong(long value)
		{
			Set(value);
		}
		public static implicit operator OptionalLong(long d) => new OptionalLong(d);
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalFloat : OptionalValue<float> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalByte : OptionalValue<byte> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalShort : OptionalValue<short> { }


	[System.Serializable]
	[Agnostic]
	public class OptionalGuid : OptionalValue<Guid> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalDouble : OptionalValue<double> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalList<T> : Optional<List<T>> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalArray<T> : Optional<T[]> { }

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfInt) + " instead")]
	public class OptionalIntArray : OptionalArray<int> { }

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfString) + " instead")]
	public class OptionalStringArray : OptionalArray<string> { }

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfFloat) + " instead")]
	public class OptionalFloatArray : OptionalArray<float> { }

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfDouble) + " instead")]
	public class OptionalDoubleArray : OptionalArray<double> { }

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfShort) + " instead")]
	public class OptionalShortArray : OptionalArray<short> { }

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfLong) + " instead")]
	public class OptionalLongArray : OptionalArray<long>
	{
		public OptionalLongArray()
		{

		}

		public OptionalLongArray(IEnumerable<long> data)
		{
			Value = data.ToArray();
			HasValue = true;
		}

	}

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfUuid) + " instead")]
	public class OptionalUuidArray : OptionalArray<Guid> { }

	[System.Serializable]
	[Agnostic]
	[Obsolete("use " + nameof(OptionalArrayOfByte) + " instead")]
	public class OptionalByteArray : OptionalArray<byte> { }


	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfInt : OptionalArray<int> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfString : OptionalArray<string> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfFloat : OptionalArray<float> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfDouble : OptionalArray<double> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfShort : OptionalArray<short> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfLong : OptionalArray<long>
	{
		public OptionalArrayOfLong()
		{

		}

		public OptionalArrayOfLong(IEnumerable<long> data)
		{
			Value = data.ToArray();
			HasValue = true;
		}

	}

	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfUuid : OptionalArray<Guid> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalArrayOfByte : OptionalArray<byte> { }



	[System.Serializable]
	[Agnostic]
	public class OptionalDictionaryStringToObject : Optional<Dictionary<string, object>> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalListInt : Optional<List<int>> { }

	[Serializable]
	public class OptionalDateTime : Optional<DateTime>
	{
		public OptionalDateTime() { }

		public OptionalDateTime(DateTime dt)
		{
			Value = dt;
			HasValue = true;
		}
	}

	[System.Serializable]
	[Agnostic]
	public class OptionalListString : Optional<List<string>> { }

	[System.Serializable]
	[Agnostic]
	public class OptionalString : Optional<string>
	{
		public static OptionalString FromString(string value)
		{
			var optional = new OptionalString();

			if (value != null)
			{
				optional.Set(value);
			}

			return optional;
		}
		
		public OptionalString()
		{

		}

		public OptionalString(string value)
		{
			Value = value;
			HasValue = true;
		}

		public string GetNonEmptyOrElse(Func<string> otherwise)
		{
			if (HasNonEmptyValue) return Value;
			return otherwise();
		}


		public bool HasNonEmptyValue => HasValue && !string.IsNullOrEmpty(Value);
	}

	[System.Serializable]
	public class ReadonlyOptionalString : OptionalString
	{
		public ReadonlyOptionalString(OptionalString source)
		{
			HasValue = source.HasValue;
			Value = source.Value;
		}

		public ReadonlyOptionalString()
		{
			HasValue = false;
		}

		public ReadonlyOptionalString(string value)
		{
			HasValue = true;
			Value = value;
		}

		public override void SetValue(object value)
		{
			throw new InvalidOperationException("Cannot write to a readonly string");
		}

		public override void Clear()
		{
			throw new InvalidOperationException("Cannot write to a readonly string");
		}

		public override void Set(string value)
		{
			throw new InvalidOperationException("Cannot write to a readonly string");
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
