// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;
using System.Collections.Generic;

namespace Beamable.Serialization
{
	public partial class JsonSerializable
	{
		/// <summary>
		/// The <see cref="TypeLookupFactory{T}"/> allows the <see cref="JsonSerializable.FromJson{T}(string,bool)"/> method
		/// to read JSON with polymorphic data types.
		/// It is required that each sub type of a polymorphic root implement a "type" field.
		/// The "type" field is used by this factory to generate the correct instance.
		/// Use the <see cref="Add{TSub}"/> function to create sub types.
		/// </summary>
		/// <typeparam name="TBase">The root type of the polymorphic type chain.</typeparam>
		public class TypeLookupFactory<TBase> : ISerializableFactory
			where TBase : ISerializable
		{
			private Dictionary<string, Func<TBase>> _typeToFactory = new Dictionary<string, Func<TBase>>();

			/// <summary>
			/// Registers a sub type for the polymorphic serialization
			/// </summary>
			/// <param name="type">The type name that will be detected in JSON and mapped to the given type.</param>
			/// <typeparam name="TSub"></typeparam>
			/// <returns>the current factory instance, to support method chaining.</returns>
			public TypeLookupFactory<TBase> Add<TSub>(string type) where TSub : TBase, new()
			{
				_typeToFactory[type] = () => new TSub();
				return this;
			}

			bool ISerializableFactory.CanCreate(Type type)
			{
				return type.IsAssignableFrom(typeof(TBase));
			}

			ISerializable ISerializableFactory.TryCreate(Type _, IDictionary<string, object> dict)
			{
				if (!dict.TryGetValue("type", out var typeObj) || !(typeObj is string typeStr))
				{
					return null;
				}

				if (!_typeToFactory.TryGetValue(typeStr, out var factory))
				{
					return null;
				}

				return factory();
			}
		}

	}
}
