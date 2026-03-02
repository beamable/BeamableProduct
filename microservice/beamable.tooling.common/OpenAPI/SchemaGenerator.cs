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
using Beamable.Common.Semantics;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Tooling.Common.OpenAPI;

/// <summary>
/// Generates OpenAPI schema definitions for complex types.
/// </summary>
public class SchemaGenerator
{
	/// <summary>
	/// This struct represents a type that should be included in the OAPI generated for the microservice.
	/// For the most 
	/// </summary>
	public struct OAPIType : IEquatable<OAPIType>
	{
		public ServiceMethod SourceCallable;
		public Type Type;

		public OAPIType(ServiceMethod method, Type type)
		{
			SourceCallable = method;
			Type = type;
		}

		public bool IsFromCallable() => SourceCallable != null && SourceCallable.Method.GetCustomAttribute<CallableAttribute>(true) != null;
		public bool IsFromFederation() => SourceCallable != null && SourceCallable.IsFederatedCallbackMethod;
		public bool IsFromBeamGenerateSchema() => SourceCallable == null && Type.GetCustomAttribute<BeamGenerateSchemaAttribute>() != null;

		public bool IsFromCallableWithNoClientGen() => IsFromCallable() && SourceCallable.Method.GetCustomAttribute<CallableAttribute>(true).Flags.HasFlag(CallableFlags.SkipGenerateClientFiles);

		public bool IsBeamSemanticType() => Type.GetInterfaces()
			.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBeamSemanticType<>));
		
		public bool ShouldSkipClientCodeGeneration() => (IsFromFederation() || IsFromCallableWithNoClientGen() || IsBeamSemanticType()) && !IsFromBeamGenerateSchema();
		
		public bool IsPrimitive()
		{
			return Type.IsPrimitive || Type == typeof(string);
		}

		public bool IsSerializable() => Type.IsSerializable;

		public bool IsOptional() => typeof(Optional).IsAssignableFrom(Type);

		public bool IsNullable() => Nullable.GetUnderlyingType(Type) != null;

		public bool IsIncluded()
		{
			var shouldEmit = true;

			// The type must be serializable as we were bound by Unity's serialization logic (so for legacy reasons, we need this to be true).
			shouldEmit &= IsSerializable();

			// We don't want to emit primitives, because those are known implicitly by the OAPI spec generation code.
			shouldEmit &= !IsPrimitive();

			// We don't want to emit generic types for documentation, because the generic aspect will be covered by the openAPI schema itself
			// No arrays, dictionaries and collections because the OAPI has its own definitions for those (so we don't need to include them as Schemas)
			shouldEmit &= !Type.IsGenericType;
			shouldEmit &= !Type.IsArray;

			// Nullables are not supported, use optional instead
			shouldEmit &= !IsNullable();
			shouldEmit &= !IsOptional();

			// Only types coming in from callables, federations and marked with BeamGenerateSchema should be found.
			shouldEmit &= (IsFromCallable() || IsFromFederation() || IsFromBeamGenerateSchema());

			return shouldEmit;
		}

		public bool Equals(OAPIType other) => Equals(SourceCallable, other.SourceCallable) && Equals(Type, other.Type);

		public override bool Equals(object obj) => obj is OAPIType other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				return ((SourceCallable != null ? SourceCallable.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
			}
		}

		public static bool operator ==(OAPIType left, OAPIType right) => left.Equals(right);
		public static bool operator !=(OAPIType left, OAPIType right) => !left.Equals(right);
	}

	/// <summary>
	/// Finds all complex types that must be included in the OAPI document.
	/// </summary>
	/// <param name="startingTypes">The collection of starting <see cref="OAPIType"/>s to analyze.</param>
	/// <returns>An enumeration of <see cref="OAPIType"/> found in the service methods.</returns>
	public static IEnumerable<OAPIType> FindAllTypesForOAPI(IEnumerable<OAPIType> startingTypes)
	{
		// construct a queue of types that we will need to search over for other types... These types are the entry points into the search.
		var toExplore = new Queue<OAPIType>();
		foreach (var startingType in startingTypes)
		{
			toExplore.Enqueue(startingType);
		}

		// construct a set of types we've seen. It starts empty.
		var seen = new HashSet<OAPIType>();

		// perform a BFS over the type graph until we've exhausted all types, or we've hit a safety limit.
		var safety = 99999;
		while (safety-- > 0 && toExplore.Count > 0)
		{
			var curr = toExplore.Dequeue();
			if (seen.Contains(curr)) continue;
			if (curr.Type == null) continue;

			// Keep track of the types we've seen already so we don't double-check it.
			seen.Add(curr);

			// add the current type to the final set of types.
			if (curr.IsIncluded()) { yield return curr; }

			// expand on this type... 
			// need to final the serialized properties of the type.
			var fields = UnityJsonContractResolver.GetSerializedFields(curr.Type);
			foreach (var field in fields)
			{
				toExplore.Enqueue(new OAPIType(curr.SourceCallable, field.FieldType));
			}

			// but also, in C#, if this is a list, or a promise, or a task like, then we are about the _generic_ argument involved. 
			if (curr.Type.IsGenericType)
			{
				foreach (var genType in curr.Type.GetGenericArguments())
				{
					toExplore.Enqueue(new OAPIType(curr.SourceCallable, genType));
				}
			}

			// if this an array, we need the element type
			if (curr.Type.IsArray)
			{
				toExplore.Enqueue(new OAPIType(curr.SourceCallable, curr.Type.GetElementType()));
			}
		}

		if (safety <= 0)
		{
			throw new InvalidOperationException("Exceeded while-loop safety limit");
		}
	}

	/// <summary>
	/// Finds all complex types used in the specified service methods that must be included in the OAPI document.
	/// </summary>
	/// <param name="methods">The collection of service methods to analyze.</param>
	/// <returns>An enumeration of complex types found in the service methods.</returns>
	public static IEnumerable<OAPIType> FindAllTypesForOAPI(IEnumerable<ServiceMethod> methods)
	{
		var startingTypes = new List<OAPIType>();
		foreach (var method in methods)
		{
			startingTypes.Add(new OAPIType(method, method.Method.ReturnType)); // output type of a method
			foreach (var parameter in method.ParameterInfos)
			{
				startingTypes.Add(new OAPIType(method, parameter.ParameterType)); // and all input types of the method
			}
		}

		return FindAllTypesForOAPI(startingTypes);
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
			case { } x when x.IsAssignableTo(typeof(Optional)):
				var instance = Activator.CreateInstance(runtimeType) as Optional;
				return Convert(instance.GetOptionalType(), depth - 1);
			case { } x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Optional<>):
				return Convert(x.GetGenericArguments()[0], depth - 1);
			case { } x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>):
				return Convert(x.GetGenericArguments()[0], depth - 1);
			case { } x when x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBeamSemanticType<>)):
				var semanticType = x.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBeamSemanticType<>));
				var semanticTypeSchema = Convert(semanticType.GetGenericArguments()[0], depth - 1);
				if (Activator.CreateInstance(runtimeType) is IBeamSemanticType semanticTypeInstance)
				{
					semanticTypeSchema.Extensions[SCHEMA_SEMANTIC_TYPE_NAME_KEY] =
						new OpenApiString(semanticTypeInstance.SemanticName);
				}

				return semanticTypeSchema;
			case { } x when x == typeof(double):
				return new OpenApiSchema { Type = "number", Format = "double" };
			case { } x when x == typeof(float):
				return new OpenApiSchema { Type = "number", Format = "float" };

			case { } x when x == typeof(short):
				return new OpenApiSchema { Type = "integer", Format = "int16" };
			case { } x when x == typeof(int):
				return new OpenApiSchema { Type = "integer", Format = "int32" };
			case { } x when x == typeof(long):
				return new OpenApiSchema { Type = "integer", Format = "int64" };

			case { } x when x == typeof(bool):
				return new OpenApiSchema { Type = "boolean" };
			case { } x when x == typeof(decimal):
				return new OpenApiSchema { Type = "number", Format = "decimal" };

			case { } x when x == typeof(string):
				return new OpenApiSchema { Type = "string" };
			case { } x when x == typeof(byte):
				return new OpenApiSchema { Type = "string", Format = "byte" };
			case { } x when x == typeof(Guid):
				return new OpenApiSchema { Type = "string", Format = "uuid" };

			// handle arrays
			case Type x when x.IsArray:
				var elemType = x.GetElementType();
				OpenApiSchema arrayOpenApiSchema = elemType is { IsGenericType: true } ? Convert(elemType, 1, true) : Convert(elemType, depth - 1);
				return new OpenApiSchema { Type = "array", Items = arrayOpenApiSchema };
			case Type x when x.IsAssignableTo(typeof(IList)) && x.IsGenericType:
				elemType = x.GetGenericArguments()[0];
				OpenApiSchema listOpenApiSchema = elemType is { IsGenericType: true } ? Convert(elemType, 1, true) : Convert(elemType, depth - 1);
				return new OpenApiSchema { Type = "array", Items = listOpenApiSchema };

			// handle maps
			case Type x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Dictionary<,>) && x.GetGenericArguments()[0] == typeof(string):
				return new OpenApiSchema
				{
					Type = "object",
					AdditionalPropertiesAllowed = true,
					AdditionalProperties = Convert(x.GetGenericArguments()[1], depth - 1),
					Extensions = new Dictionary<string, IOpenApiExtension>
					{
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAMESPACE] = new OpenApiString(runtimeType.Namespace),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAME] = new OpenApiString(runtimeType.Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME] = new OpenApiString(runtimeType.GetGenericQualifiedTypeName()),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY] = new OpenApiString(runtimeType.Assembly.GetName().Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY_VERSION] = new OpenApiString(runtimeType.Assembly.GetName().Version.ToString()),
						[MICROSERVICE_EXTENSION_BEAMABLE_FORCE_TYPE_NAME] = new OpenApiString(runtimeType.GetGenericSanitizedFullName())
					}
				};


			case Type _ when depth <= 0:
				return new OpenApiSchema { Type = "object", Reference = new OpenApiReference { Id = GetQualifiedReferenceName(runtimeType), Type = ReferenceType.Schema } };

			case { IsEnum: true }:
				var enumNames = Enum.GetNames(runtimeType);
				return new OpenApiSchema
				{
					Enum = enumNames.Select(name => new OpenApiString(name)).Cast<IOpenApiAny>().ToList(),
					Extensions = new Dictionary<string, IOpenApiExtension>
					{
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAMESPACE] = new OpenApiString(runtimeType.Namespace),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAME] = new OpenApiString(runtimeType.Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME] = new OpenApiString(GetQualifiedReferenceName(runtimeType)),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY] = new OpenApiString(runtimeType.Assembly.GetName().Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY_VERSION] = new OpenApiString(runtimeType.Assembly.GetName().Version.ToString())
					}
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
						new OpenApiString(runtimeType.GetGenericSanitizedFullName());
				}

				if (depth == 0) return schema;
				var members = UnityJsonContractResolver.GetSerializedFields(runtimeType);
				foreach (var member in members)
				{
					var name = member.Name;
					var fieldSchema = Convert(member.FieldType, depth - 1);

					var comment = DocsLoader.GetMemberComments(member);
					fieldSchema.Description = comment?.Summary;


					Type classSemanticType = member.FieldType.GetInterfaces().FirstOrDefault(i =>
						i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBeamSemanticType<>));
					if (classSemanticType != null && Activator.CreateInstance(member.FieldType) is IBeamSemanticType memberSemanticTypeInstance)
					{
						fieldSchema.Extensions[SCHEMA_SEMANTIC_TYPE_NAME_KEY] =
							new OpenApiString(memberSemanticTypeInstance.SemanticName);
					}

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
