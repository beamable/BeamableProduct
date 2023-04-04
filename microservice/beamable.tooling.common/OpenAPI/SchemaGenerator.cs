using Beamable.Server;
using Beamable.Server.Common;
using Microsoft.OpenApi.Models;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Beamable.Tooling.Common.OpenAPI;

public class SchemaGenerator
{

	public static IEnumerable<Type> FindAllComplexTypes(IEnumerable<ServiceMethod> methods)
	{
		
		// construct a queue of types that we will need to search over for other types... These types are the entry points into the search.
		var toExplore = new Queue<Type>();
		foreach (var method in methods)
		{
			toExplore.Enqueue(method.Method.ReturnType); // output type of a method
			foreach (var parameter in method.ParameterInfos)
			{
				toExplore.Enqueue(parameter.ParameterType); // and all input types of the method
			}
		}

		// construct a set of types we've seen. It starts empty.
		var seen = new HashSet<Type>();

		// perform a BFS over the type graph until we've exhausted all types, or we've hit a safety limit.
		var safety = 99999;
		while (safety-- > 0 && toExplore.Count > 0)
		{
			var curr = toExplore.Dequeue();
			if (seen.Contains(curr)) continue;

			seen.Add(curr);

			var shouldEmit = true;
			shouldEmit &= !curr.IsGenericType; // we don't want to emit generic types for documentation, because the generic aspect will be covered by the openAPI schema itself
			shouldEmit &= !curr.IsPrimitive; // we don't want to emit primitives, because those don't need special documentation
			shouldEmit &= curr.IsSerializable;
			shouldEmit &= !curr.IsArray;
			shouldEmit &= curr != typeof(string);
			if (shouldEmit)
			{
				yield return curr; // add the current type to the final set of types.
			}
			
			// expand on this type... 
			// need to final the serialized properties of the type.
			var fields = UnityJsonContractResolver.GetSerializedFields(curr);
			foreach (var field in fields)
			{
				toExplore.Enqueue(field.FieldType);
			}
			// but also, in C#, if this is a list, or a promise, or a task like, then we are about the _generic_ argument involved. 
			if (curr.IsGenericType)
			{
				foreach (var genType in curr.GetGenericArguments())
				{
					toExplore.Enqueue(genType);
				}
			}
			
		}
	}

	public static IEnumerable<Type> Traverse<T>() => Traverse(typeof(T));
	public static IEnumerable<Type> Traverse(Type runtimeType)
	{
		yield return runtimeType;
	}

	
	public static OpenApiSchema Convert(Type runtimeType, int depth = 1)
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
				return new OpenApiSchema { Reference = new OpenApiReference { Id = runtimeType.Name, Type = ReferenceType.Schema} };

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
