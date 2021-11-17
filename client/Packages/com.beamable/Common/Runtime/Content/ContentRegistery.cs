using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Content;
using UnityEngine;

namespace Beamable.Common.Content
{
	/// <summary>
	/// This type defines part of the %Beamable %ContentObject system.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class ContentTypePair
	{
		public Type Type;
		public string Name;
	}

	/// <summary>
	/// This type defines part of the %Beamable %ContentObject system.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public static class ContentRegistry
	{
		private static readonly Dictionary<string, Type> contentTypeToClass = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, string> classToContentType = new Dictionary<Type, string>();

		static ContentRegistry()
		{
			LoadRuntimeTypeData();
		}

		public static void LoadRuntimeTypeData(HashSet<Type> contentTypes = null)
		{
			contentTypeToClass.Clear();
			classToContentType.Clear();

			contentTypes = contentTypes ?? GetTypesFromAssemblies();
			foreach (var type in contentTypes)
			{
				string
					typeName = GetContentTypeName(
						type); // XXX Do I need this!??? Maybe the order they come back in is sneaky for the type->name

				var formerlySerializedTypeNames = GetAllValidContentTypeNames(type, true).ToList();
				foreach (var possibleTypeName in formerlySerializedTypeNames)
				{
					if (possibleTypeName == null) continue;
					contentTypeToClass[possibleTypeName] = type;
				}

				contentTypeToClass[typeName] = type;
				classToContentType[type] = typeName;
			}
		}

		public static HashSet<Type> GetTypesFromAssemblies()
		{
			var types = new HashSet<Type>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var asmName = assembly.GetName().Name;
				if ("Tests".Equals(asmName)) continue; // TODO think harder about this.
				try
				{
					foreach (var type in assembly.GetTypes())
					{
						bool hasContentAttribute = type.GetCustomAttribute<ContentTypeAttribute>(false) != null;
						bool isAssignableFromIContentObject = typeof(IContentObject).IsAssignableFrom(type);

#if !DB_MICROSERVICE
						bool isAssignableFromScriptableObject = typeof(ScriptableObject).IsAssignableFrom(type);
#else
                  bool isAssignableFromScriptableObject = true;
#endif

						if (hasContentAttribute && isAssignableFromIContentObject && isAssignableFromScriptableObject)
						{
							types.Add(type);
						}
					}
				}
				catch (Exception ex)
				{
					BeamableLogger.LogError(ex);
				}
			}

			return types;
		}

		private static IEnumerable<string> GetAllValidContentTypeNames(Type contentType, bool includeFormerlySerialized)
		{
			if (contentType == null)
			{
				yield return null;
				yield break;
			}

#if !DB_MICROSERVICE
			if (contentType == typeof(ScriptableObject))
			{
				yield return null;
				yield break;
			}

#endif
			var contentTypeAttribute = contentType.GetCustomAttribute<ContentTypeAttribute>(false);

			if (contentTypeAttribute == null)
			{
				/*
				 * [ContentType("x")]
				 * class X : ContentObject
				 *
				 * class Y : X
				 *
				 * [ContentType("z")]
				 * class Z : Y
				 *
				 * x.z.foo
				 */
				//
				var baseNames = GetAllValidContentTypeNames(contentType.BaseType, includeFormerlySerialized);
				foreach (var baseName in baseNames)
				{
					yield return baseName;
				}

				yield break;
			}

			var startType = contentTypeAttribute.TypeName;

			var possibleNames = new HashSet<string> {startType};

			if (includeFormerlySerialized)
			{
				var formerlySerializedAsAttributes =
					contentType.GetCustomAttributes<ContentFormerlySerializedAsAttribute>(false);
				foreach (var formerlySerializedAsAttribute in formerlySerializedAsAttributes)
				{
					if (string.IsNullOrEmpty(formerlySerializedAsAttribute?.OldTypeName)) continue;
					possibleNames.Add(formerlySerializedAsAttribute.OldTypeName);
				}
			}

			var possibleEndNames = GetAllValidContentTypeNames(contentType.BaseType, includeFormerlySerialized);

			foreach (var possibleEnd in possibleEndNames)
			{
				foreach (var possibleStart in possibleNames)
				{
					if (possibleStart != null && possibleEnd != null)
					{
						yield return string.Join(".", possibleEnd, possibleStart);
					}
					else
					{
						yield return possibleEnd ?? possibleStart;
					}
				}
			}
		}

		public static string GetContentTypeName(Type contentType)
		{
			return GetAllValidContentTypeNames(contentType, false).First();
		}

		public static IEnumerable<ContentTypePair> GetAll()
		{
			foreach (var kvp in classToContentType)
			{
				yield return new ContentTypePair {Type = kvp.Key, Name = kvp.Value};
			}
		}

		public static IEnumerable<Type> GetContentTypes()
		{
			return classToContentType.Keys;
		}

		public static IEnumerable<string> GetContentClassIds()
		{
			return contentTypeToClass.Keys;
		}

		public static string GetTypeNameFromId(string id)
		{
			return id.Substring(0, id.LastIndexOf("."));
		}

		public static string GetContentNameFromId(string id)
		{
			return id.Substring(id.LastIndexOf(".") + 1);
		}

		public static Type GetTypeFromId(string id)
		{
			var typeName = GetTypeNameFromId(id);

			if (!contentTypeToClass.TryGetValue(typeName, out var type))
			{
				// the type doesn't exist, but maybe we can try again?

				var hasAnotherDot = typeName.IndexOf('.') > -1;
				if (hasAnotherDot)
				{
					return GetTypeFromId(typeName);
				}
				else
				{
					return typeof(ContentObject);
				}
			}

			return type;
		}

		public static bool TryGetType(string typeName, out Type type)
		{
			return contentTypeToClass.TryGetValue(typeName, out type);
		}

		public static bool TryGetName(Type type, out string name)
		{
			return classToContentType.TryGetValue(type, out name);
		}

		public static Type NameToType(string name)
		{
			if (contentTypeToClass.TryGetValue(name, out var type))
			{
				return type;
			}

			return typeof(ContentObject);
		}

		public static string TypeToName(Type type)
		{
			if (classToContentType.TryGetValue(type, out var name))
			{
				return name;
			}
			else
			{
				throw new Exception($"No content name found for type=[{type.Name}]");
			}
		}
	}
}
