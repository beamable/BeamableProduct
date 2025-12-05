using Beamable.Common.Reflection;
using Beamable.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = System.Object;

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

	public class ContentTypeReflectionCache : IReflectionSystem
	{
		private static readonly BaseTypeOfInterest ICONTENT_OBJECT_BASE_TYPE;
		private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

		private static readonly AttributeOfInterest CONTENT_TYPE_ATTRIBUTE;
		private static readonly AttributeOfInterest CONTENT_TYPE_FORMERLY_SERIALIZED_AS_ATTRIBUTE;
		private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

		internal static ContentTypeReflectionCache Instance;
		static ContentTypeReflectionCache()
		{
			ICONTENT_OBJECT_BASE_TYPE = new BaseTypeOfInterest(typeof(IContentObject), false);
			CONTENT_TYPE_ATTRIBUTE = new AttributeOfInterest(typeof(ContentTypeAttribute), new Type[] { }, new[] { typeof(IContentObject) });
			CONTENT_TYPE_FORMERLY_SERIALIZED_AS_ATTRIBUTE = new AttributeOfInterest(typeof(ContentFormerlySerializedAsAttribute), new Type[] { }, new[] { typeof(IContentObject) });
			BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest>() { ICONTENT_OBJECT_BASE_TYPE, };
			ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest>() { CONTENT_TYPE_ATTRIBUTE, CONTENT_TYPE_FORMERLY_SERIALIZED_AS_ATTRIBUTE };
		}

		public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
		public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

		public IReadOnlyDictionary<string, Type> ContentTypeToClass => _contentTypeToClass;
		public IReadOnlyDictionary<Type, string> ClassToContentType => _classToContentType;

		private Dictionary<string, Type> _contentTypeToClass = new Dictionary<string, Type>(StringComparer.Ordinal);
		private Dictionary<Type, string> _classToContentType = new Dictionary<Type, string>();
		private TypeFieldInfoReflectionCache _typeFieldInfos = null;
		public void ClearCachedReflectionData()
		{
			_contentTypeToClass.Clear();
			_classToContentType.Clear();
		}

		public TypeFieldInfoReflectionCache GetTypeFieldsCache() => _typeFieldInfos;

		public void OnSetupForCacheGeneration()
		{
			_contentTypeToClass = new Dictionary<string, Type>(StringComparer.Ordinal);
			_classToContentType = new Dictionary<Type, string>();
			_typeFieldInfos = new TypeFieldInfoReflectionCache(this);
		}

		public void OnReflectionCacheBuilt(PerBaseTypeCache perBaseTypeCache, PerAttributeCache perAttributeCache)
		{ }

		public void OnBaseTypeOfInterestFound(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
		{
			// Gather all validation results ensuring the ContentTypeAttribute exists and Validation
			var contentTypeAttributePairs = new List<MemberAttribute>();
			{
				var warningMessage = "This type is not deserializable by Beamable and you will not be able to create content of this type directly via the Content Manager!";
				var validationResults = cachedSubTypes.GetAndValidateAttributeExistence(CONTENT_TYPE_ATTRIBUTE,
																						info => new AttributeValidationResult(
																							null, info, ReflectionCache.ValidationResultType.Warning, warningMessage));

				validationResults.SplitValidationResults(out var valid, out var warnings, out var errors);

				// manually remove the ContentObject from the warnings set.
				warnings.RemoveAll(r => r.Pair.Info == typeof(ContentObject));

				// TODO: [AssistantRemoval] Add (ID_CONTENT_TYPE_ATTRIBUTE_MISSING) as a Static Check.
				// TODO: [AssistantRemoval] Add (ID_INVALID_CONTENT_TYPE_ATTRIBUTE) as a Static Check.

				contentTypeAttributePairs.AddRange(valid.Select(a => a.Pair));
			}

			// Gather all validation results ensuring the ContentTypeAttribute exists and Validation
			var formerContentTypeAttributePairs = new List<MemberAttribute>();
			{
				var validationResults = cachedSubTypes.GetAndValidateAttributeExistence(CONTENT_TYPE_FORMERLY_SERIALIZED_AS_ATTRIBUTE,
																						info => new AttributeValidationResult(null, info, ReflectionCache.ValidationResultType.Valid, ""));

				validationResults.SplitValidationResults(out var valid, out _, out var errors);

				// TODO: [AssistantRemoval] Add (ID_INVALID_CONTENT_FORMERLY_SERIALIZED_AS_ATTRIBUTE) as a Static Check.

				formerContentTypeAttributePairs.AddRange(valid.Where(a => a.Pair.Attribute != null).Select(a => a.Pair));
			}
			// Quick hack to "fake" the ContentFormerlySerializedAttribute is a ContentType Attribute --- easy and simple way to verify name collisions is to pretend they are of the same type.
			var typeConversionOfAttributes = formerContentTypeAttributePairs
				.Select(input => new MemberAttribute(input.Info, new ContentTypeAttribute(input.AttrAs<ContentFormerlySerializedAsAttribute>().OldTypeName)));

			// Check if no name collisions happened ---- TODO: There's most definitely a better way to do this...
			var mappings = contentTypeAttributePairs.Union(typeConversionOfAttributes).ToList();
			var nameValidationResults = mappings.GetAndValidateUniqueNamingAttributes<ContentTypeAttribute>();
			{
				nameValidationResults.PerAttributeNameValidations.SplitValidationResults(out var valid,
																						 out _,
																						 out var errors);

				// TODO: [AssistantRemoval] Add (ID_INVALID_CONTENT_TYPE_ATTRIBUTE) as a Static Check --- use errors list here.
				// TODO: [AssistantRemoval] Add (ID_CONTENT_TYPE_NAME_COLLISION) as a Static Check --- use nameValidationResults list here.


				var validContentTypes = valid.Select(v => v.Pair.Info as Type).ToList();

				// Cache data 😃
				foreach (var type in validContentTypes)
				{
					AddContentTypeToDictionaries(type);
					_ = _typeFieldInfos.GetFieldInfos(type, false, true);
				}
			}
			Instance = this;
		}

		public void AddContentTypeToDictionaries(Type type) => AddContentTypeToDictionaries(type, _contentTypeToClass, _classToContentType);
		public static void AddContentTypeToDictionaries(Type type, Dictionary<string, Type> contentTypeToClassDict, Dictionary<Type, string> classToContentTypeDict)
		{
			// Guaranteed to exist due to validation.
			var typeName = GetAllValidContentTypeNames(type, false).First();
			if(typeName == null) return;
			var formerlySerializedTypeNames = GetAllValidContentTypeNames(type, true);
			foreach (var possibleTypeName in formerlySerializedTypeNames)
			{
				if (possibleTypeName == null) continue;

				if (contentTypeToClassDict.ContainsKey(possibleTypeName))
					contentTypeToClassDict[possibleTypeName] = type;
				else
					contentTypeToClassDict.Add(possibleTypeName, type);
			}

			// Adds to dictionaries caches for use in serialization/deserialization
			if (contentTypeToClassDict.ContainsKey(typeName))
				contentTypeToClassDict[typeName] = type;
			else
				contentTypeToClassDict.Add(typeName, type);

			if (classToContentTypeDict.ContainsKey(type))
				classToContentTypeDict[type] = typeName;
			else
				classToContentTypeDict.Add(type, typeName);
		}

		public void OnAttributeOfInterestFound(AttributeOfInterest attributeType, IReadOnlyList<MemberAttribute> cachedMemberAttributes) { }

		public static IEnumerable<string> GetAllValidContentTypeNames(Type contentType, bool includeFormerlySerialized)
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

			var possibleNames = new HashSet<string> { startType };

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
			if(Instance._classToContentType.TryGetValue(contentType, out var name))
			{
				return name;
			}

			return GetAllValidContentTypeNames(contentType, false).First();
		}

		public IEnumerable<ContentTypePair> GetAll() => ClassToContentType.Select(kvp => new ContentTypePair { Type = kvp.Key, Name = kvp.Value });

		public IEnumerable<Type> GetContentTypes() => ClassToContentType.Keys;
		public IEnumerable<string> GetContentClassIds() => ContentTypeToClass.Keys;


		public static int GetLastDotInContentId(string id)
		{
			for (int i = id.Length - 1; i >= 0; i--)
			{
				if (id[i] == '.')
					return i;
			}

			return 0;
		}

		public static string GetTypeNameFromId(string id)
		{
			int lastDot = GetLastDotInContentId(id);
			return id.Substring(0, lastDot);
		}

		public static string GetContentNameFromId(string id)
		{
			int lastDot = GetLastDotInContentId(id);
			return id.Substring(lastDot + 1);
		}

		public bool TryGetType(string typeName, out Type type) => ContentTypeToClass.TryGetValue(typeName, out type);

		public bool TryGetName(Type type, out string name) => ClassToContentType.TryGetValue(type, out name);

		public Type NameToType(string name) => ContentTypeToClass.TryGetValue(name, out var type) ? type : typeof(ContentObject);

		public string TypeToName(Type type) => ClassToContentType.TryGetValue(type, out var name) ? name : throw new ContentNotFoundException(type);
		public bool HasContentTypeValidClass(string contentId) => ContentTypeToClass.ContainsKey(contentId);

		public Type GetTypeFromId(string id)
		{
			var typeName = GetTypeNameFromId(id);

			if (!ContentTypeToClass.TryGetValue(typeName, out var type))
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
	}

	public class TypeFieldInfoReflectionCache
	{
		public readonly struct FieldInfoWrapper
		{
			public readonly FieldInfo RawField;
			public readonly string SerializedName;
			public readonly string FieldName;
			public readonly ReadOnlyCollection<string> FormerlySerializedAs;
			public readonly int FormerlySerializedAsLength;
			public readonly Type FieldType;

			public FieldInfoWrapper(string serializedName,FieldInfo rawField, string backingFieldName,
			                        ReadOnlyCollection<string> formerlySerializedAs)
			{
				FieldType = rawField.FieldType;
				SerializedName = serializedName;
				if (string.IsNullOrWhiteSpace(backingFieldName))
				{
					FieldName = serializedName;
					FormerlySerializedAs = formerlySerializedAs;
				}
				else
				{
					FieldName = backingFieldName;
					var oldFieldAsBackup = new List<string>(formerlySerializedAs);
					oldFieldAsBackup.Add(serializedName);
					FormerlySerializedAs = oldFieldAsBackup.AsReadOnly();
				}
				FormerlySerializedAsLength = FormerlySerializedAs.Count;
				RawField = rawField;
			}

			public bool TryGetPropertyForField(ICollection<string> keys, out string field)
			{
				field = string.Empty;
				foreach (var key in keys)
				{
					if (string.Equals(key, FieldName, StringComparison.Ordinal))
					{
						field = key;
						return true;
					}
					for (int i = 0; i < FormerlySerializedAsLength; i++)
					{
						if(string.Equals(key, FormerlySerializedAs[i], StringComparison.Ordinal))
						{
							field = key;
						}
					}
				}

				return !string.IsNullOrWhiteSpace(field);
			}

			public bool TrySetValue(object obj, object value)
			{
				if (!FieldType.IsInstanceOfType(value)) return false;

				RawField.SetValue(obj, value);
				return true;
			}
			public object GetValue(object obj) => RawField.GetValue(obj);
		}
		private readonly Dictionary<Type, ReadOnlyCollection<FieldInfoWrapper>> _typeInfoCache = new  Dictionary<Type, ReadOnlyCollection<FieldInfoWrapper>>();
		private readonly Dictionary<Type, ReadOnlyCollection<FieldInfoWrapper>> _typeInfoCacheWithIgnoredFields = new  Dictionary<Type, ReadOnlyCollection<FieldInfoWrapper>>();
		private readonly ContentTypeReflectionCache _reflectionCache;

		public TypeFieldInfoReflectionCache(ContentTypeReflectionCache cache)
		{
			_reflectionCache = cache;
			foreach (var contentType in _reflectionCache.GetContentTypes())
			{
				_ = GetFieldInfos(contentType, false, true);
			}
		}

		public static bool TryGetArrayValueType(Type baseType, out Type elementType)
		{
			var hasMatchingType = baseType.GenericTypeArguments.Length == 1;
			if (hasMatchingType)
			{
				elementType = baseType.GenericTypeArguments[0];
				return true;
			}

			var hasBaseType = baseType.BaseType != typeof(Object);
			if (hasBaseType)
			{
				return TryGetArrayValueType(baseType.BaseType, out elementType);
			}
			elementType = null;
			return false;
		}

		
		public ReadOnlyCollection<FieldInfoWrapper> GetFieldInfos(Type type, bool withIgnoredFields = false, bool addToCache = false)
		{
			if(withIgnoredFields && _typeInfoCacheWithIgnoredFields.TryGetValue(type, out var info))
			{
				return info;
			}
			if(!withIgnoredFields && _typeInfoCache.TryGetValue(type, out info))
			{
				return info;
			}
			FieldInfoWrapper CreateFieldWrapper(FieldInfo field)
			{
				var attr = field.GetCustomAttribute<ContentFieldAttribute>();
				string serializedName;
				string backingField = null;
				var formerlySerializedAs = new List<string>();
				if (attr != null && !string.IsNullOrEmpty(attr.SerializedName))
				{
					serializedName = attr.SerializedName;
				}
				else if (field.Name.StartsWith("<") && field.Name.Contains('>'))
				{
					int endIndex = 1;
					for (; endIndex < field.Name.Length; endIndex++)
					{
						if (field.Name[endIndex] == '>')
						{
							break;
						}
					}

					serializedName = field.Name.Substring(1, endIndex-1);
					backingField = $"<{serializedName}>k__BackingField";
				}
				else
				{
					serializedName = field.Name;
				}

				if (attr != null && attr.FormerlySerializedAs != null)
				{
					for (var index = 0; index < attr.FormerlySerializedAs.Length; index++)
					{
						formerlySerializedAs.Add(attr.FormerlySerializedAs[index]);
					}
				}
				else
				{
					var formerlySerializedAttrs = field.GetCustomAttributes<UnityEngine.Serialization.FormerlySerializedAsAttribute>();
					foreach (var formerlyAttr in formerlySerializedAttrs)
					{
						if (!string.IsNullOrEmpty(formerlyAttr.oldName))
						{
							formerlySerializedAs.Add(formerlyAttr.oldName);
						}
					}
				}

				return new FieldInfoWrapper(serializedName, field, backingField, formerlySerializedAs.AsReadOnly());
			}
			bool TryGetAllPrivateFields(Type currentType, out FieldInfo[] infos)
			{
				infos = null;
				var shouldSkip = currentType == null;
				shouldSkip |= currentType == typeof(System.Object);
				shouldSkip |= currentType == typeof(ScriptableObject);
				shouldSkip |= currentType == typeof(ContentObject);

				// XXX: Revisit this check when we allow customers to only implement IContentObject instead of subclass ContentObject
				shouldSkip |= currentType.BaseType == typeof(System.Object) &&
				                            currentType.GetInterfaces().Contains(typeof(IContentObject));
				if (shouldSkip)
				{
					return false;
				}

				// private fields are only available via reflection on the target type, and any base type fields will need to be gathered by manually walking the type tree.
				var privateFields = currentType
					.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
				for (int i = privateFields.Count - 1; i >= 0; i--)
				{
					if (privateFields[i].GetCustomAttribute(typeof(SerializeField)) == null &&
					    privateFields[i].GetCustomAttribute(typeof(ContentFieldAttribute)) == null)
					{
						privateFields.RemoveAt(i);
					}
				}
				if(TryGetAllPrivateFields(currentType.BaseType, out var extraInfos))
				{
					privateFields.AddRange(extraInfos);
				}

				infos = privateFields.ToArray();
				return true;
			}

			var listOfPublicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field =>
				field.GetCustomAttribute<NonSerializedAttribute>() == null &&
				!typeof(Delegate).IsAssignableFrom(field.FieldType)).ToList();

			if (!TryGetAllPrivateFields(type, out var privateFieldInfos))
			{
				return new ReadOnlyCollection<FieldInfoWrapper>(new FieldInfoWrapper[]{});
			}

			var serializableFields = listOfPublicFields.Union(privateFieldInfos).ToList();
			var notIgnoredFields = serializableFields.Where(field => field.GetCustomAttribute<IgnoreContentFieldAttribute>() == null);

			var serializableFieldsWrapper = serializableFields.Select(CreateFieldWrapper);
			var notIgnoredFieldsWrapper = notIgnoredFields.Select(CreateFieldWrapper);
			serializableFieldsWrapper = serializableFieldsWrapper.OrderBy(n => n.SerializedName.ToString());
			notIgnoredFieldsWrapper = notIgnoredFieldsWrapper.OrderBy(n => n.SerializedName.ToString());

			var notIgnoredFieldsResult = new ReadOnlyCollection<FieldInfoWrapper>(notIgnoredFieldsWrapper.ToArray());
			var allFieldsResult = new ReadOnlyCollection<FieldInfoWrapper>(serializableFieldsWrapper.ToArray());
			if (addToCache)
			{
				_typeInfoCache[type] = notIgnoredFieldsResult;
				_typeInfoCacheWithIgnoredFields[type] = allFieldsResult;
				foreach (var field in allFieldsResult)
				{
					var t = field.FieldType;
					if (typeof(Optional).IsAssignableFrom(t))
					{
						var optional = (Optional)Activator.CreateInstance(t);
						t = optional.GetOptionalType();
					}
					if (typeof(IDictionaryWithValue).IsAssignableFrom(t))
					{
						var dict = (IDictionaryWithValue)Activator.CreateInstance(t);
						t = dict.ValueType;
					} else if (typeof(IList).IsAssignableFrom(t))
					{

						if (TryGetArrayValueType(t, out var elementType))
						{
							t = elementType;
						}
					}
					t = Nullable.GetUnderlyingType(t) ?? t;
					if (!_typeInfoCacheWithIgnoredFields.ContainsKey(t))
					{
						_ = GetFieldInfos(t, withIgnoredFields, true);
					}
				}
			}
			if (withIgnoredFields)
			{
				return allFieldsResult;
			}
			else
			{
				return notIgnoredFieldsResult;
			}
		}


		/// <summary>
		/// Retrieves the <see cref="Type"/> associated with the given content ID.
		/// </summary>
		/// <param name="contentId">The identifier of the content for which the type is to be retrieved.</param>
		/// <returns>The <see cref="Type"/> corresponding to the specified content ID.</returns>
		public Type GetTypeById(string contentId)
		{
			return _reflectionCache.GetTypeFromId(contentId);
		}

		public (string, string) GetTypeNameAndContentName(string contentId)
		{
			int i = GetLastDotIndex(contentId);
			return (contentId.Substring(0, i), contentId.Substring(i + 1));
		}


		private int GetLastDotIndex(string contentId)
		{
			int lastDot = 0;
			for (int i = contentId.Length - 1; i >= 0; i--)
			{
				if(contentId[i] == '.')
				{
					lastDot = i;
					break;
				}
			}
			return lastDot;
		}
	}
}
