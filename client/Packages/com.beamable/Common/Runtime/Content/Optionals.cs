using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using UnityEngine;

namespace Beamable.Common.Content
{
   [Serializable]
    [Agnostic]
    public abstract class Optional
    {
	    public virtual bool HasValue
	    {
		    get;
		    protected set;
	    }
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
          return new Optional<int>{Value = number};
       }
       public static Optional<double> ToOptional(this double number)
       {
          return new Optional<double>{ Value = number};
       }
    }

    [System.Serializable]
    [Agnostic]
    public class Optional<T> : Optional//, ISerializable
    {
	    [SerializeField]
	    private T value;

        public T Value
        {
	        get => value;
	        set
	        {
		        this.value = value;
		        HasValue = value != null;
	        }
        }

        public override object GetValue()
        {
            return Value;
        }

        public override void SetValue(object value)
        {
           Value = (T) value;
        }

        public override Type GetOptionalType()
        {
           return typeof(T);
        }

        // public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        // {
	       //  info.AddValue("", Value);
        // }
        //
        // // The special constructor is used to deserialize values.
        // public Optional(SerializationInfo info, StreamingContext context)
        // {
	       //  // Reset the property value using the GetValue method.
	       //  Value = (T) info.GetValue("", typeof(T));
        // }
    }

    [Agnostic]
    public class OptionalTypeConverter : TypeConverter {
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
             return new OptionalInt { Value = (int)number };
          }
          return base.ConvertTo(context, culture, value, destinationType);
       }
    }

    [System.Serializable]
    [Agnostic]
    public class OptionalBoolean : Optional<bool> { }

    [System.Serializable]
    [Agnostic]
    public class OptionalInt : Optional<int> { }
    
    [System.Serializable]
    [Agnostic]
    public class OptionalLong : Optional<long> { }

    [System.Serializable]
    [Agnostic]
    public class OptionalDouble : Optional<double> { }

    [System.Serializable]
    [Agnostic]
    public class OptionalListInt : Optional<List<int>> { }
    
    [System.Serializable]
    [Agnostic]
    public class OptionalListString : Optional<List<string>> { }

    [System.Serializable]
    [Agnostic]
    public class OptionalString : Optional<string> { }

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

       public int Add(object value)=> InternalList.Add(value);

       public void Clear() => InternalList.Clear();

       public bool Contains(object value)=> InternalList.Contains(value);

       public int IndexOf(object value) => InternalList.IndexOf(value);

       public void Insert(int index, object value)=> InternalList.Insert(index, value);

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
