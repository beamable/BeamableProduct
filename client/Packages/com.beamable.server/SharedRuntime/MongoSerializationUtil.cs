// unset

using Beamable.Common;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
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
			if (BsonClassMap.IsClassMapRegistered(typeof(T))) return;

			if (!BsonClassMap.IsClassMapRegistered(typeof(StorageDocument)))
			{
				// Map base StorageDocument class

				var baseClassMap = BsonClassMap.RegisterClassMap<StorageDocument>(cm =>
				{
					cm.AutoMap();

					// Need to be set again because for RegisterClassMap<T> it will be lost

					cm.MapIdField("_id").SetSerializer(new StringSerializer(BsonType.ObjectId)).SetIgnoreIfDefault(true); 
				});
			}

			var classMap =  BsonClassMap.RegisterClassMap<T>(cm => {

				cm.AutoMap();

				// Iterate throght properites and unmap them

				PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

				if (props.Length > 0)
				{
					foreach (PropertyInfo propertyInfo in props)
					{
						Debug.LogError($"UnmapProperty {propertyInfo.Name}");
						cm.UnmapProperty(propertyInfo.Name);
					}
				}

				MemberInfo[] members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.NonPublic |  BindingFlags.Instance | BindingFlags.DeclaredOnly);

				if (members.Length > 0)
				{
					foreach (MemberInfo memberInfo in members)
					{
						if (!memberInfo.IsDefined(typeof(CompilerGeneratedAttribute), false)) // we don't want to k__backingfield
						{
							if (memberInfo.MemberType == MemberTypes.Field)
							{
								// Set field as serializable if has SerializableAttribute

								if (memberInfo.GetCustomAttribute(typeof(SerializeField)) != null)
									cm.MapField(memberInfo.Name);
								else if (!((FieldInfo)memberInfo).IsPublic)
								{
									Debug.LogError($"UnmapField {memberInfo.Name}");
									cm.UnmapField(memberInfo.Name);
								}

							}

							// Set new member name if has FormerlySerializedAsAttribute

							if (memberInfo.GetCustomAttribute(typeof(FormerlySerializedAsAttribute)) is FormerlySerializedAsAttribute formerlySerializedAttr)
								cm.GetMemberMap(memberInfo.Name).SetElementName(formerlySerializedAttr.oldName);
						}
					}
				}
			});

			classMap.Freeze();

			BsonSerializer.RegisterSerializer(typeof(T), new StructSerializer<T>(new BsonClassMapSerializer<T>(classMap)));
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

