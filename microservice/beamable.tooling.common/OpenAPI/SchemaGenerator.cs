using Microsoft.OpenApi.Models;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Beamable.Tooling.Common.OpenAPI;

public class SchemaGenerator
{
	public SchemaGenerator()
	{
		
	}

	public IEnumerable<Type> Traverse<T>() => Traverse(typeof(T));
	public IEnumerable<Type> Traverse(Type runtimeType)
	{
		yield return runtimeType;
	}

	
	public OpenApiSchema Convert(Type runtimeType, int depth = 1)
	{

		switch (runtimeType)
		{
			
			case { } x when x == typeof(double):
				return new OpenApiSchema { Type = "number", Format = "double"};
			case { } x when x == typeof(float):
				return new OpenApiSchema { Type = "number", Format = "float"};
			
			case { } x when x == typeof(short):
				return new OpenApiSchema { Type = "integer", Format = "int16"};
			case { } x when x == typeof(int):
				return new OpenApiSchema { Type = "integer", Format = "int32"};
			case { } x when x == typeof(long):
				return new OpenApiSchema { Type = "integer", Format = "int64"};
			
			case { } x when x == typeof(bool):
				return new OpenApiSchema { Type = "boolean"};
			
			case { } x when x == typeof(string):
				return new OpenApiSchema { Type = "string"};
			case { } x when x == typeof(byte):
				return new OpenApiSchema { Type = "string", Format = "byte"};
			case { } x when x == typeof(Guid):
				return new OpenApiSchema { Type = "string", Format = "uuid"};
			
			case Type _ when depth == 0:
				return new OpenApiSchema { Reference = new OpenApiReference { Id = runtimeType.Name } };

			// handle arrays
			case Type x when x.IsArray:
				var elemType = x.GetElementType();
				return new OpenApiSchema { Items = Convert(elemType, depth - 1)};
			case Type x when x.IsAssignableTo(typeof(IList)) && x.IsGenericType:
				elemType = x.GetGenericArguments()[0];
				return new OpenApiSchema { Items = Convert(elemType, depth - 1)};
			
			// handle maps
			case Type x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Dictionary<,>) && x.GetGenericArguments()[0] == typeof(string):
				return new OpenApiSchema
				{
					AdditionalPropertiesAllowed = true,
					AdditionalProperties = Convert(x.GetGenericArguments()[1], depth - 1)
				};

				break;
			
			default:
				
				var schema = new OpenApiSchema { };
				// schema.Title = runtimeType.Name;
				schema.Properties = new Dictionary<string, OpenApiSchema>();

				if (depth == 0) return schema;
				var members = GetSerializableMembers(runtimeType);
				foreach (var member in members)
				{
					var name = member.Name;
					var fieldSchema = Convert(member.FieldType, depth - 1);
					schema.Properties[name] = fieldSchema;
				}
				
				
				return schema;
				break;
		}

		throw new NotImplementedException();
	}
	
	protected static List<FieldInfo> GetSerializableMembers(Type objectType)
	{

		IEnumerable<FieldInfo> GetAllFields(Type t)
		{
			if (t == null)
				return Enumerable.Empty<FieldInfo>();

			BindingFlags flags = BindingFlags.Public |
			                     BindingFlags.NonPublic |
			                     BindingFlags.Instance |
			                     BindingFlags.DeclaredOnly;
			return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
		}

		var fields = GetAllFields(objectType);

		bool IsPublicField(FieldInfo field) => field.IsPublic;
		bool IsReadOnly(FieldInfo field) => field.IsInitOnly;
		bool IsPublicAndWritable(FieldInfo field) => IsPublicField(field) && !IsReadOnly(field);
		bool IsMarkedSerializeAttribute(FieldInfo field) => field.GetCustomAttribute<SerializeField>() != null;

		bool ShouldIncludeField(FieldInfo field) => IsPublicAndWritable(field) || IsMarkedSerializeAttribute(field);

		var validFields = fields
			.Where(ShouldIncludeField)
			.ToList();

		return validFields.Cast<FieldInfo>().ToList();
	}
}
