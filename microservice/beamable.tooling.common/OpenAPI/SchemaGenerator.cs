using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Server.Common.XmlDocs;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using System.Collections;
using System.Reflection;
using UnityEngine;
using ZLogger;
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
		public bool ShouldNotGenerateClientCode() => (IsFromFederation() || IsFromCallableWithNoClientGen()) && !IsFromBeamGenerateSchema();
		
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
	/// Generates a dictionary of schemas that can be used to populate the OpenAPI docs.
	/// </summary>
	/// <param name="oapiTypes"></param>
	/// <param name="requiredTypes"></param>
	/// <returns>Dictionary of OpenApiSchemas</returns>
	public static Dictionary<string,OpenApiSchema> ToOpenApiSchemasDictionary(IList<OAPIType> oapiTypes, ref HashSet<Type> requiredTypes)
	{
		var result = new Dictionary<string,OpenApiSchema>(oapiTypes.Count);
		var toSkip = new HashSet<int>(oapiTypes.Count);
		for (int i = 0; i < oapiTypes.Count; i++)
		{
			if(toSkip.Contains(i))
				continue;
			var shouldGenerateClientCode = !oapiTypes[i].ShouldNotGenerateClientCode();

			for (int j = i + 1; j < oapiTypes.Count; j++)
			{
				if (oapiTypes[j].Type != oapiTypes[i].Type)
				{
					continue;
				}

				toSkip.Add(j);

				if (!shouldGenerateClientCode && !oapiTypes[j].ShouldNotGenerateClientCode())
				{
					shouldGenerateClientCode = true;
				}
			}
			
			// We check because the same type can both be an extra type (declared via BeamGenerateSchema) AND be used in a signature; so we de-duplicate the concatenated lists.
			// If all usages of this type (within a sub-graph of types starting from a ServiceMethod) is set to NOT generate the client code, we won't.
			// Otherwise, even if just a single usage of the type wants the client code to be generated, we do generate it.
			// That's what this thing does.
			var type = oapiTypes[i].Type;
			var key = GetQualifiedReferenceName(type);
			var schema = Convert(type, ref requiredTypes);
			schema.AddExtension(METHOD_SKIP_CLIENT_GENERATION_KEY, new OpenApiBoolean(shouldGenerateClientCode));
			BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Adding Schema to Microservice OAPI docs. Type={type.FullName}, WillGenClient={shouldGenerateClientCode}");
			result.Add(key, schema);
		}
		return result;
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

	public static bool TryAddMissingSchemaTypes(ref OpenApiDocument oapiDoc, HashSet<Type> requiredTypes)
	{
		var newRequiredTypes = new HashSet<Type>();
		foreach (Type requiredType in requiredTypes)
		{
			if (requiredType.IsBasicType())
			{
				continue;
			}
			var key = requiredType.GetSanitizedFullName();
			if(oapiDoc.Components.Schemas.ContainsKey(key))
				continue;
			var schema = Convert(requiredType, ref newRequiredTypes);
			oapiDoc.Components.Schemas.Add(key, schema);
		}

		if (newRequiredTypes.Count > 0)
		{
			return TryAddMissingSchemaTypes(ref oapiDoc, newRequiredTypes);
		}

		return true;
	}

	/// <summary>
	/// Converts a runtime type into an OpenAPI schema.
	/// </summary>
	public static OpenApiSchema Convert(Type runtimeType, ref HashSet<Type> requiredTypes, int depth = 1, bool sanitizeGenericType = false)
	{
		switch (runtimeType)
		{
			case { } x when x.IsAssignableTo(typeof(Optional)):
				var instance = Activator.CreateInstance(runtimeType) as Optional;
				return Convert(instance.GetOptionalType(), ref requiredTypes,depth - 1, sanitizeGenericType);
			case { } x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Optional<>):
				return Convert(x.GetGenericArguments()[0], ref requiredTypes,depth - 1, sanitizeGenericType);
			case { } x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>):
				return Convert(x.GetGenericArguments()[0], ref requiredTypes,depth - 1, sanitizeGenericType);
			case { } x when x == typeof(double):
				return new OpenApiSchema { Type = "number", Format = "double" };
			case { } x when x == typeof(float):
				return new OpenApiSchema { Type = "number", Format = "float" };

			case { } x when x == typeof(short):
				return new OpenApiSchema { Type = "integer", Format = "int16", Minimum = short.MinValue, Maximum = short.MaxValue };
			case { } x when x == typeof(ushort):
				return new OpenApiSchema { Type = "integer", Format = "int16", Minimum = ushort.MinValue, Maximum = ushort.MaxValue };
			case { } x when x == typeof(int):
				return new OpenApiSchema { Type = "integer", Format = "int32" };
			case { } x when x == typeof(uint):
				return new OpenApiSchema { Type = "integer", Format = "int32", Minimum = uint.MinValue, Maximum = uint.MaxValue };
			case { } x when x == typeof(long):
				return new OpenApiSchema { Type = "integer", Format = "int64" };
			case { } x when x == typeof(ulong):
				return new OpenApiSchema { Type = "integer", Format = "int64", Minimum = ulong.MinValue, Maximum = ulong.MaxValue };

			case { } x when x == typeof(short):
				return new OpenApiSchema { Type = "integer", Format = "int32" };
			
			case { } x when x == typeof(bool):
				return new OpenApiSchema { Type = "boolean" };
			case { } x when x == typeof(decimal):
				return new OpenApiSchema { Type = "number", Format = "decimal" };

			case { } x when x == typeof(string):
				return new OpenApiSchema { Type = "string" };
			case { } x when x == typeof(char):
				return new OpenApiSchema { Type = "string", MaxLength = 1, MinLength = 1};
			case { } x when x == typeof(byte):
				return new OpenApiSchema { Type = "string", Format = "byte", Minimum = byte.MinValue, Maximum = byte.MaxValue};
			case { } x when x == typeof(sbyte):
				return new OpenApiSchema { Type = "string", Format = "byte", Minimum = sbyte.MinValue, Maximum = sbyte.MaxValue };
			case { } x when x == typeof(Guid):
				return new OpenApiSchema { Type = "string", Format = "uuid" };

			// handle arrays
			case Type x when x.IsArray:
				var elemType = x.GetElementType();
				OpenApiSchema arrayOpenApiSchema = elemType is { IsGenericType: true } ? Convert(elemType, ref requiredTypes, depth, true) : Convert(elemType, ref requiredTypes,depth - 1);
				return new OpenApiSchema { Type = "array", Items = arrayOpenApiSchema };
			case Type x when x.IsAssignableTo(typeof(IList)) && x.IsGenericType:
				elemType = x.GetGenericArguments()[0];
				OpenApiSchema listOpenApiSchema = elemType is { IsGenericType: true } ? Convert(elemType, ref requiredTypes, depth, true) : Convert(elemType, ref requiredTypes,depth - 1);
				return new OpenApiSchema { Type = "array", Items = listOpenApiSchema };

			// handle maps
			case Type x when x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Dictionary<,>) && x.GetGenericArguments()[0] == typeof(string):
				return new OpenApiSchema
				{
					Type = "object",
					AdditionalPropertiesAllowed = true,
					AdditionalProperties = Convert(x.GetGenericArguments()[1], ref requiredTypes,depth - 1, sanitizeGenericType),
					Extensions = new Dictionary<string, IOpenApiExtension>
					{
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAMESPACE] = new OpenApiString(runtimeType.Namespace),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAME] = new OpenApiString(runtimeType.Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME] = new OpenApiString(runtimeType.GetSanitizedFullName()),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY] = new OpenApiString(runtimeType.Assembly.GetName().Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY_VERSION] = new OpenApiString(runtimeType.Assembly.GetName().Version.ToString()),
						[MICROSERVICE_EXTENSION_BEAMABLE_FORCE_TYPE_NAME] = new OpenApiString(runtimeType.GetSanitizedFullName())
					}
				};
			case Type x when IsDictionary(x):
				var das= GetDictionaryTypes(x);
				return new OpenApiSchema
				{
					Type = "object",
					AdditionalPropertiesAllowed = true,
					
					AdditionalProperties = Convert(das.Value.ValueType, ref requiredTypes,depth - 1, sanitizeGenericType),
					Extensions = new Dictionary<string, IOpenApiExtension>
					{
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAMESPACE] = new OpenApiString(runtimeType.Namespace),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAME] = new OpenApiString(runtimeType.Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME] = new OpenApiString(runtimeType.GetSanitizedFullName()),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY] = new OpenApiString(runtimeType.Assembly.GetName().Name),
						[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY_VERSION] = new OpenApiString(runtimeType.Assembly.GetName().Version.ToString()),
						[MICROSERVICE_EXTENSION_BEAMABLE_FORCE_TYPE_NAME] = new OpenApiString(runtimeType.GetSanitizedFullName())
					}
				};


			case Type _ when depth <= 0:
				requiredTypes.Add(runtimeType);
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

				string typeName = sanitizeGenericType ? runtimeType.GetSanitizedFullName() : runtimeType.Name;

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
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME] = new OpenApiString(sanitizeGenericType ? runtimeType.GetSanitizedFullName() : GetQualifiedReferenceName(runtimeType)),
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY] = new OpenApiString(runtimeType.Assembly.GetName().Name),
					[MICROSERVICE_EXTENSION_BEAMABLE_TYPE_OWNER_ASSEMBLY_VERSION] = new OpenApiString(runtimeType.Assembly.GetName().Version.ToString())
				};

				if (runtimeType.GetCustomAttribute<ContentTypeAttribute>() is { } contentTypeAttribute)
				{
					schema.Extensions["x-beamable-content-type-name"] = new OpenApiString(contentTypeAttribute.TypeName);
				}
				if (sanitizeGenericType)
				{
					schema.Extensions[MICROSERVICE_EXTENSION_BEAMABLE_FORCE_TYPE_NAME] =
						new OpenApiString(runtimeType.GetSanitizedFullName());
				}

				if (depth == 0) { 
					requiredTypes.Add(runtimeType);
					return schema;
				}
				var members = UnityJsonContractResolver.GetSerializedFields(runtimeType);
				foreach (var member in members)
				{
					var name = member.Name;
					var fieldSchema = Convert(member.FieldType,ref requiredTypes, depth - 1, sanitizeGenericType);

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
		return Uri.EscapeUriString(runtimeType.GetSanitizedFullName());
	}
	static bool IsDictionary(Type type)
	{
		if (type == null) return false;

		return type.GetInterfaces().Any(i => 
			       i.IsGenericType && 
			       i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) 
		       || IsSubclassOfRawGeneric(typeof(Dictionary<,>), type);
	}
	static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
		while (toCheck != null && toCheck != typeof(object)) {
			var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
			if (generic == cur) {
				return true;
			}
			toCheck = toCheck.BaseType;
		}
		return false;
	}
	static (Type KeyType, Type ValueType)? GetDictionaryTypes(Type type)
	{
		// Look for the IDictionary<TKey, TValue> interface
		var dictionaryIntf = type.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && 
			                     i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

		if (dictionaryIntf != null)
		{
			var args = dictionaryIntf.GetGenericArguments();
			return (args[0], args[1]);
		}

		return null;
	}
}
