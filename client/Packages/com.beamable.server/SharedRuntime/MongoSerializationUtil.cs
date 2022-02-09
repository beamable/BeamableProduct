// unset

using Beamable.Common;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Server
{
	public interface IMongoSerializationService
	{
		/// <summary>
		/// The MongoDB driver won't automatically serialize structs correctly.
		/// You'll need to manually register your structs with this function.
		/// This function will enable serialization of the T, and List<T>
		/// </summary>
		/// <typeparam name="T">Some type of struct.</typeparam>
		/// <returns>The same <see cref="IMongoSerializationService"/> instance to support method chaining</returns>
		IMongoSerializationService RegisterStruct<T>() where T : struct;
	}

	public class MongoSerializationService : IMongoSerializationService
	{
		public void Init()
		{
			// automatically register unity types.
			RegisterStruct<Vector2>();
			RegisterStruct<Vector3>();
			RegisterStruct<Vector4>();
			RegisterStruct<Vector2Int>();
			RegisterStruct<Vector3Int>();
			RegisterStruct<Quaternion>();
			RegisterStruct<Rect>();
			RegisterStruct<RectInt>();
			RegisterStruct<Color>();
		}

		public IMongoSerializationService RegisterStruct<T>() where T : struct
		{
			if (BsonClassMap.IsClassMapRegistered(typeof(T))) return this;
			var classMap = BsonClassMap.RegisterClassMap<T>(cm =>
			{
				cm.AutoMap();
			});
			classMap.Freeze();

			BsonSerializer.RegisterSerializer(typeof(T),
				new StructSerializer<T>(new BsonClassMapSerializer<T>(classMap)));
			BsonSerializer.RegisterSerializer(typeof(List<T>), new BsonListSerializer<T>());

			return this;
		}

		public static void RegisterClass<T>() where T : class
		{
			if (BsonClassMap.IsClassMapRegistered(typeof(T)))
				return;

			// find and set bsonID attribute manually

			FieldInfo bsonIDField = FindBsonIdField(typeof(T));

			if (bsonIDField != null)
			{
				if (bsonIDField.DeclaringType != typeof(T) && !BsonClassMap.IsClassMapRegistered(bsonIDField.DeclaringType))
				{
					var cm_base = new BsonClassMap(bsonIDField.DeclaringType);
					cm_base.AutoMap();
					cm_base.MapIdField(bsonIDField.Name).SetSerializer(new StringSerializer(BsonType.ObjectId)).SetIgnoreIfDefault(true);
					BsonClassMap.RegisterClassMap(cm_base);
				}
			}

			var classMap =  BsonClassMap.RegisterClassMap<T>(cm => {

				cm.AutoMap();

				// set bsonID attribute for T if it's needed

				if (bsonIDField != null && bsonIDField.DeclaringType == typeof(T))
					cm.MapIdField(bsonIDField.Name).SetSerializer(new StringSerializer(BsonType.ObjectId)).SetIgnoreIfDefault(true);

				// Iterate throght properites and unmap them

				PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

				if (props.Length > 0)
				{
					foreach (PropertyInfo propertyInfo in props)
						cm.UnmapProperty(propertyInfo.Name);
				}

				FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic |  BindingFlags.Instance | BindingFlags.DeclaredOnly);

				if (fields.Length > 0)
				{
					foreach (FieldInfo fieldInfo in fields)
					{
						if (!fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), false)) // we don't want to k__backingfield
						{
							// Set field as serializable if has SerializableAttribute

							if (fieldInfo.GetCustomAttribute(typeof(SerializeField)) != null)
								cm.MapField(fieldInfo.Name);
							else if (!((FieldInfo)fieldInfo).IsPublic)
								cm.UnmapField(fieldInfo.Name);

							// Set new member name if has FormerlySerializedAsAttribute

							if (fieldInfo.GetCustomAttribute(typeof(FormerlySerializedAsAttribute)) is FormerlySerializedAsAttribute formerlySerializedAttr)
								cm.GetMemberMap(fieldInfo.Name).SetElementName(formerlySerializedAttr.oldName);
						}
					}
				}
			});

			classMap.Freeze();

			BsonSerializer.RegisterSerializer(typeof(T), new StructSerializer<T>(new BsonClassMapSerializer<T>(classMap)));
		}

		private static FieldInfo FindBsonIdField(Type t)
		{
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			foreach (var field in t.GetFields(flags))
			{
				if (field.GetCustomAttribute(typeof(BsonIdAttribute)) != null)
					return field;
			}

			if (t.BaseType != null)
				return FindBsonIdField(t.BaseType);

			return null;
		}
	}

	public class StructSerializer<T> : IBsonSerializer<T>
	{
		private readonly IBsonSerializer _serializer;

		public Type ValueType => typeof(T);

		public StructSerializer(IBsonSerializer serializer)
		{
			_serializer = serializer;
			// ValueType = type;
		}

		T IBsonSerializer<T>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var obj = Deserialize(context, args);
			return (T)obj;
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
		{
			this.Serialize(context, args, (object)value);
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
		{
			_serializer.Serialize(context, args, value);
		}

		public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			BsonType bsonType = context.Reader.GetCurrentBsonType();
			if (bsonType == BsonType.Null)
			{
				context.Reader.ReadNull();
				return null;
			}
			else
			{
				object obj = Activator.CreateInstance(ValueType);

				context.Reader.ReadStartDocument();

				while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
				{
					string name = context.Reader.ReadName(Utf8NameDecoder.Instance);

					FieldInfo field = ValueType.GetField(name);
					if (field != null)
					{
						object value = BsonSerializer.Deserialize(context.Reader, field.FieldType);
						field.SetValue(obj, value);
					}

					PropertyInfo prop = ValueType.GetProperty(name);
					if (prop != null)
					{
						object value = BsonSerializer.Deserialize(context.Reader, prop.PropertyType);
						prop.SetValue(obj, value, null);
					}
				}

				context.Reader.ReadEndDocument();

				return obj;
			}
		}
	}

	public class BsonListSerializer<T>
		: IBsonSerializer<List<T>>
	{


		object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			return Deserialize(context, args);
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, List<T> value)
		{
			var values = value.Select(x => x.ToBson());
			BsonArraySerializer.Instance.Serialize(context, args, new BsonArray(values));
		}

		public List<T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var bsonArray = BsonArraySerializer.Instance.Deserialize(context, args);
			var output = new List<T>();
			foreach (var doc in bsonArray)
			{
				var elem = BsonSerializer.Deserialize<T>(doc.AsByteArray);
				output.Add(elem);

			}
			return output;
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
		{
			Serialize(context, args, (List<T>)value);
		}

		public Type ValueType => typeof(List<T>);
	}
}

