using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Server.Common.XmlDocs;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using System.Collections;
using System.Reflection;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Tooling.Common.OpenAPI;

/// <summary>
/// Generates OpenAPI schema definitions for complex types.
/// </summary>
public class SchemaGenerator
{
	/// <summary>
	/// Finds all complex types used in the specified service methods.
	/// </summary>
	/// <param name="startingTypes">The collection of service methods to analyze.</param>
	/// <returns>An enumeration of complex types found in the service methods.</returns>
	public static IEnumerable<Type> FindAllComplexTypes(IEnumerable<Type> startingTypes)
	{
		// construct a queue of types that we will need to search over for other types... These types are the entry points into the search.
		var toExplore = new Queue<Type>();
		foreach (var startingType in startingTypes)
		{
			toExplore.Enqueue(startingType);
		}

		// construct a set of types we've seen. It starts empty.
		var seen = new HashSet<Type>();

		// perform a BFS over the type graph until we've exhausted all types, or we've hit a safety limit.
		var safety = 99999;
		while (safety-- > 0 && toExplore.Count > 0)
		{
			var curr = toExplore.Dequeue();
			if (seen.Contains(curr)) continue;
			if (curr == null) continue;

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
			// if this an array, we need the element type
			if (curr.IsArray)
			{
				toExplore.Enqueue(curr.GetElementType());
			}
			
		}

		if (safety <= 0)
		{
			throw new InvalidOperationException("Exceeded while-loop safety limit");
		}
	}
	
	/// <summary>
	/// Finds all complex types used in the specified service methods.
	/// </summary>
	/// <param name="methods">The collection of service methods to analyze.</param>
	/// <returns>An enumeration of complex types found in the service methods.</returns>
	public static IEnumerable<Type> FindAllComplexTypes(IEnumerable<ServiceMethod> methods)
	{
		var startingTypes = new List<Type>();
		foreach (var method in methods)
		{
			startingTypes.Add(method.Method.ReturnType); // output type of a method
			foreach (var parameter in method.ParameterInfos)
			{
				startingTypes.Add(parameter.ParameterType); // and all input types of the method
			}
		}

		return FindAllComplexTypes(startingTypes);
	}

	/// <summary>
	/// Traverses the type hierarchy starting from the specified type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type from which to start the traversal.</typeparam>
	/// <returns>An enumeration of types in the hierarchy.</returns>
	public static IEnumerable<Type> Traverse<T>() => Traverse(typeof(T));

	/// <summary>
	/// Traverses the type hierarchy starting from the specified runtime type.
	/// </summary>
	/// <param name="runtimeType">The runtime type from which to start the traversal.</param>
	/// <returns>An enumeration of types in the hierarchy.</returns>
	public static IEnumerable<Type> Traverse(Type runtimeType)
	{
		yield return runtimeType;
	}

	/// <summary>
	/// Converts a runtime type into an OpenAPI schema.
	/// </summary>
	public static OpenApiSchema Convert(Type runtimeType, int depth = 1, bool sanitizeGenericType = false)
	{

		switch (runtimeType)
		{
			case {} x when x.IsAssignableTo(typeof(Optional)):
				var instance = Activator.CreateInstance(runtimeType) as Optional;
				return Convert(instance.GetOptionalType());
			case {} x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Optional<>):
				return Convert(x.GetGenericArguments()[0]);
			case { } x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>):
				return Convert(x.GetGenericArguments()[0]);
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
			case { } x when x == typeof(decimal):
				return new OpenApiSchema { Type = "number", Format = "decimal" };
			
			case { } x when x == typeof(string):
				return new OpenApiSchema { Type = "string"};
			case { } x when x == typeof(byte):
				return new OpenApiSchema { Type = "string", Format = "byte"};
			case { } x when x == typeof(Guid):
				return new OpenApiSchema { Type = "string", Format = "uuid"};
			
			// handle arrays
			case Type x when x.IsArray:
				var elemType = x.GetElementType();
				return new OpenApiSchema { Type = "array", Items = Convert(elemType, depth - 1)};
			case Type x when x.IsAssignableTo(typeof(IList)) && x.IsGenericType:
				elemType = x.GetGenericArguments()[0];
				return new OpenApiSchema { Type = "array", Items = Convert(elemType, depth - 1)};
			
			// handle maps
			case Type x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Dictionary<,>) && x.GetGenericArguments()[0] == typeof(string):
				return new OpenApiSchema
				{
					Type = "object",
					AdditionalPropertiesAllowed = true,
					AdditionalProperties = Convert(x.GetGenericArguments()[1], depth - 1)
				};

			
			case Type _ when depth <= 0:
				return new OpenApiSchema
				{
					Type = "object",
					Reference = new OpenApiReference { Id = GetQualifiedReferenceName(runtimeType), Type = ReferenceType.Schema}
				};

			case { IsEnum: true }:
				var enumNames = Enum.GetNames(runtimeType);
				return new OpenApiSchema
				{
					Enum = enumNames.Select(name => new OpenApiString(name)).Cast<IOpenApiAny>().ToList()
				};
			
			default:
				
				var schema = new OpenApiSchema { };
				var comments = DocsLoader.GetTypeComments(runtimeType);
				
				string typeName = sanitizeGenericType ? runtimeType.GetGenericSanitizedFullName() : runtimeType.Name;
				
				schema.Description = comments.Summary;
				schema.Properties = new Dictionary<string, OpenApiSchema>();
				schema.Required = new SortedSet<string>();
				schema.Type = "object";
				schema.Title = typeName;
				schema.AdditionalPropertiesAllowed = false;
				schema.Extensions = new Dictionary<string, IOpenApiExtension>
				{
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAMESPACE] = new OpenApiString(runtimeType.Namespace),
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAME] = new OpenApiString(typeName),
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME] = new OpenApiString(sanitizeGenericType ? runtimeType.GetGenericQualifiedTypeName() : GetQualifiedReferenceName(runtimeType)),
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY] = new OpenApiString(runtimeType.Assembly.GetName().Name),
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY_VERSION] = new OpenApiString(runtimeType.Assembly.GetName().Version.ToString())
				};

				if (sanitizeGenericType)
				{
					schema.Extensions[MICROSERVICE_EXTENSION_BEAMABLE_FORCE_TYPE_NAME] =
						new OpenApiString(runtimeType.Assembly.GetName().Version.ToString());
				}
				
				if (depth == 0) return schema;
				var members = UnityJsonContractResolver.GetSerializedFields(runtimeType);
				foreach (var member in members)
				{
					var name = member.Name;
					var fieldSchema = Convert(member.FieldType, depth - 1);

					var comment = DocsLoader.GetMemberComments(member);
					fieldSchema.Description = comment?.Summary;
					schema.Properties[name] = fieldSchema;

					if (!member.FieldType.IsAssignableTo(typeof(Optional)))
					{
						schema.Required.Add(name);
					}
				}

				return schema;
		}
	}

	/// <summary>
	/// Gets the fully qualified reference name for a runtime type.
	/// </summary>
	public static string GetQualifiedReferenceName(Type runtimeType)
	{
		return runtimeType.FullName.Replace("+", ".");
	}
}
