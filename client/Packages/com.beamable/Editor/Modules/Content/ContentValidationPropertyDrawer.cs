using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{

#if !BEAMABLE_NO_VALIDATION_DRAWERS
	[CustomPropertyDrawer(typeof(ValidationAttribute), true)]
#endif
	public class ContentValidationPropertyDrawer : PropertyDrawer
	{
		private GUIStyle _lblStyle;
		private const int WIDTH = 3;
		private const int OFFSET = -10;
		private const int MAX_VALIDATION_CACHE_ENTRIES = 512;
		private static readonly Dictionary<ValidationCacheKey, ValidationCacheEntry>
			_validationCache = new Dictionary<ValidationCacheKey, ValidationCacheEntry>();

		#region Cache Data Types

		private readonly struct ValidationCacheKey : IEquatable<ValidationCacheKey>
		{
			public readonly int TargetInstanceId;
			public readonly string PropertyPath;

			public ValidationCacheKey(int targetInstanceId, string propertyPath)
			{
				TargetInstanceId = targetInstanceId;
				PropertyPath = propertyPath;
			}

			public bool Equals(ValidationCacheKey other)
			{
				return TargetInstanceId == other.TargetInstanceId
				       && string.Equals(PropertyPath, other.PropertyPath, StringComparison.Ordinal);
			}

			public override bool Equals(object obj)
			{
				return obj is ValidationCacheKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (TargetInstanceId * 397) ^
					       (PropertyPath != null ? PropertyPath.GetHashCode() : 0);
				}
			}
		}

		private readonly struct ValidationRevision : IEquatable<ValidationRevision>
		{
			private readonly uint _propertyHash;
			private readonly Guid _objectRevision;
			private readonly bool _usesObjectRevision;

			private ValidationRevision(uint propertyHash, Guid objectRevision, bool usesObjectRevision)
			{
				_propertyHash = propertyHash;
				_objectRevision = objectRevision;
				_usesObjectRevision = usesObjectRevision;
			}

			public static ValidationRevision Create(SerializedProperty property, ContentObject contentObject)
			{
#if UNITY_2022_2_OR_NEWER
				// contentHash tracks the specific property rather than invalidating
				// every validation field when one field changes.
				// For a direct managed reference, contentHash only represents the
				// managed-reference id, not all referenced contents. Use the
				// ContentObject revision for that case.
				if (property.propertyType != SerializedPropertyType.ManagedReference)
				{
					return new ValidationRevision(
						property.contentHash,
						Guid.Empty,
						false);
				}
#endif

				// Beamable currently supports Unity 2021.3. contentHash is unavailable
				// there, so use the ContentObject validation revision as a safe,
				// slightly coarser fallback.
				return new ValidationRevision(
					0,
					contentObject.ValidationGuid,
					true);
			}

			public bool Equals(ValidationRevision other)
			{
				if (_usesObjectRevision != other._usesObjectRevision)
					return false;

				return _usesObjectRevision ? _objectRevision.Equals(other._objectRevision)
					: _propertyHash == other._propertyHash;
			}

			public override bool Equals(object obj)
			{
				return obj is ValidationRevision other && Equals(other);
			}

			public override int GetHashCode()
			{
				return _usesObjectRevision
					? _objectRevision.GetHashCode()
					: (int)_propertyHash;
			}
		}

		private sealed class ValidationCacheEntry
		{
			public ValidationRevision PropertyRevision;
			public int ManifestRevision;
			public ValidationResult Result;
		}

		private sealed class ValidationResult
		{
			public static readonly ValidationResult Empty = new(Array.Empty<ContentException>(), 0);
			public ContentException[] Exceptions { get; }
			public int AdditionalLineCount { get; }

			public ValidationResult(ContentException[] exceptions, int additionalLineCount)
			{
				Exceptions = exceptions;
				AdditionalLineCount = additionalLineCount;
			}
		}

		#endregion

		#region Cache LifeCycle Cleanup

		private static void ClearValidationCache()
		{
			_validationCache.Clear();
		}

		private static void InvalidateValidationResult(SerializedProperty property)
		{
			if (property == null ||
			   property.serializedObject == null ||
			   property.serializedObject.targetObject == null)
			{
				return;
			}

			var key = new ValidationCacheKey(property.serializedObject.targetObject.GetInstanceID(),
				property.propertyPath);

			_validationCache.Remove(key);
		}

		#endregion

		static ContentValidationPropertyDrawer()
		{
			Selection.selectionChanged += ClearValidationCache;
			Undo.undoRedoPerformed += ClearValidationCache;
			AssemblyReloadEvents.beforeAssemblyReload += ClearValidationCache;
			EditorApplication.quitting += ClearValidationCache;
		}

		private ValidationResult GetValidationResult(SerializedProperty property)
		{
			var contentObject = property.serializedObject.targetObject as ContentObject;
			if (contentObject == null || !BeamEditor.IsInitialized)
			{
				return ValidationResult.Empty;
			}

			var contentService = BeamEditorContext.Default.CliContentService;
			var validationContext = contentService.GetValidationContext();

			// Do not cache the uninitialized state. Once initialization completes,
			// the next layout/draw must perform validation.
			if (validationContext is not {Initialized: true})
			{
				return ValidationResult.Empty;
			}

			var key = new ValidationCacheKey(contentObject.GetInstanceID(), property.propertyPath);
			var propertyRevision = ValidationRevision.Create(property, contentObject);
			var currentManifestRevision = contentService.ManifestChangedCount;

			if (_validationCache.TryGetValue(key, out var cachedEntry) &&
			   cachedEntry.PropertyRevision.Equals(propertyRevision) &&
			   cachedEntry.ManifestRevision == currentManifestRevision)
			{
				return cachedEntry.Result;
			}

			var result = ComputeValidationResult(
				property,
				contentObject,
				validationContext);

			// Defensive upper bound for removed array elements and objects that
			// remain alive without causing a Unity selection change.
			if (_validationCache.Count >= MAX_VALIDATION_CACHE_ENTRIES &&
			    !_validationCache.ContainsKey(key))
			{
				_validationCache.Clear();
			}

			_validationCache[key] = new ValidationCacheEntry
			{
				PropertyRevision = propertyRevision,
				ManifestRevision = currentManifestRevision,
				Result = result
			};

			return result;
		}

		private ValidationResult ComputeValidationResult(SerializedProperty property, ContentObject contentObject, IValidationContext validationContext)
		{
			if (fieldInfo == null) return ValidationResult.Empty;

			var attributes = fieldInfo.GetCustomAttributes<ValidationAttribute>().ToArray();
			if (attributes.Length == 0) return ValidationResult.Empty;

			var parentValue = ContentRefPropertyDrawer.GetTargetParentObjectOfProperty(property);
			var isArray = TryGetArrayIndex(property, out var arrayIndex);
			var wrapper = new ValidationFieldWrapper(fieldInfo, parentValue);
			var validationArgs = ContentValidationArgs.Create(wrapper,
			                                                  contentObject,
			                                                  validationContext,
			                                                  arrayIndex,
			                                                  isArray);

			var exceptions = new List<ContentException>();
			var additionalLineCount = 0;

			foreach (var attribute in attributes)
			{
				try
				{
					attribute.Validate(validationArgs);
				}
				catch (ContentException ex)
				{
					exceptions.Add(ex);
					var messageLineCount =
						1 + ex.FriendlyMessage.Count(character => character == '\n');

					additionalLineCount += messageLineCount;
				}
			}
			return new ValidationResult(
				exceptions.ToArray(),
				additionalLineCount);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			label.tooltip = PropertyDrawerHelper.SetTooltipWithFallback(fieldInfo, property);

			var baseHeight = RefEditorGUI.DefaultPropertyHeight(property, label);
			if (property.serializedObject.isEditingMultipleObjects || !BeamEditor.IsInitialized)
			{
				return baseHeight;
			}

			var validationResult =
				GetValidationResult(property);

			return baseHeight +
			       EditorGUIUtility.singleLineHeight *
			       validationResult.AdditionalLineCount;
		}

		/// <summary>
		/// For <see cref="string"/> fields (including <c>List&lt;string&gt;</c> elements) decorated with
		/// <see cref="MustReferenceContent"/>, draws an editable text field with a dropdown button that opens a
		/// searchable list of valid content ids. Returns <c>false</c> for anything else so the caller falls back
		/// to the default property field.
		/// </summary>
		private bool TryDrawContentDropdown(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				return false;
			}

			// Catches MustReferenceContent and its MustBe* subclasses, which preset AllowedTypes.
			var mustReference = fieldInfo.GetCustomAttribute<MustReferenceContent>();
			if (mustReference == null)
			{
				return false;
			}

			var lineHeight = EditorGUIUtility.singleLineHeight;
			var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
			var fieldX = position.x + EditorGUIUtility.labelWidth;
			var fieldWidth = position.width - EditorGUIUtility.labelWidth;
			const float buttonWidth = 22f;
			var textRect = new Rect(fieldX, position.y, Mathf.Max(0, fieldWidth - buttonWidth), lineHeight);
			var buttonRect = new Rect(fieldX + textRect.width, position.y, buttonWidth, lineHeight);

			EditorGUI.PrefixLabel(labelRect, label);

			EditorGUI.BeginChangeCheck();
			var newValue = EditorGUI.DelayedTextField(textRect, property.stringValue);
			if (EditorGUI.EndChangeCheck())
			{
				property.stringValue = newValue;
				property.serializedObject.ApplyModifiedProperties();
			}

			if (EditorGUI.DropdownButton(buttonRect, new GUIContent(string.Empty, label.tooltip), FocusType.Keyboard, EditorStyles.popup))
			{
				var wnd = ScriptableObject.CreateInstance<ContentStringSearchWindow>();
				wnd.Property = property;
				wnd.Object = property.serializedObject.targetObject;
				wnd.AllowedTypes = mustReference.AllowedTypes;
				wnd.AllowNull = mustReference.AllowNull;
				wnd.Init();

				var xy = EditorGUIUtility.GUIToScreenPoint(new Vector2(textRect.x, textRect.y));
				wnd.ShowAsDropDown(new Rect((int)xy.x, (int)xy.y + (int)lineHeight, 0, 0),
				   new Vector2(fieldWidth, 300));
			}

			return true;
		}

		protected bool TryGetArrayIndex(SerializedProperty property, out int arrayIndex)
		{
			arrayIndex = 0;

			var rightBracketIndex = property.propertyPath.LastIndexOf(']');
			if (rightBracketIndex == property.propertyPath.Length - 1)
			{
				var leftBracketIndex = property.propertyPath.LastIndexOf('[');
				if (leftBracketIndex > 0 &&
					int.TryParse(property.propertyPath.Substring(leftBracketIndex + 1,
					   (rightBracketIndex - leftBracketIndex) - 1), out var index))
				{
					arrayIndex = index;
					return true;
				}
			}
			return false;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label.tooltip = PropertyDrawerHelper.SetTooltipWithFallback(fieldInfo, property);

			if (property.serializedObject.isEditingMultipleObjects || !BeamEditor.IsInitialized)
			{
				RefEditorGUI.DefaultPropertyField(position, property, label);
				return; // Multiple-object validation is not supported.
			}

			var validationResult = GetValidationResult(property);

			EditorGUI.BeginChangeCheck();

			if (!TryDrawContentDropdown(position, property, label))
			{
				RefEditorGUI.DefaultPropertyField(position, property, label);
			}

			var propertyChanged = EditorGUI.EndChangeCheck();

			if (propertyChanged)
			{
				// Do not recompute immediately. Unity calculated position.height
				// during the preceding layout pass using the previous result.
				// The next layout/repaint will compute the updated validation result.
				InvalidateValidationResult(property);
			}

			var exceptions = validationResult.Exceptions;

			if (exceptions.Length == 0)
			{
				return;
			}

			if (_lblStyle == null)
			{
				_lblStyle = new GUIStyle(GUI.skin.label) {fontSize = (int)(GUI.skin.label.fontSize * 0.7f)};
				_lblStyle.normal.textColor = Color.red;
				_lblStyle.hover.textColor = Color.red;
			}

			for (var i = 0; i < exceptions.Length; i++)
			{
				var exception = exceptions[i];
				var content = new GUIContent($"  {exception.FriendlyMessage}");
				var newlineCount = exception.FriendlyMessage.Count(character => character == '\n');
				var errorHeight = EditorGUIUtility.singleLineHeight * (newlineCount + 1);
				var errorPosition = new Rect(
					position.x,
					position.y +
					position.height +
					EditorGUIUtility.singleLineHeight *
					(i - (newlineCount + 1)),
					position.width,
					errorHeight);

				EditorGUI.LabelField(errorPosition, content, _lblStyle);
			}

			var errorBarPosition = new Rect(
				position.x - WIDTH + OFFSET,
				position.y - 1,
				WIDTH,
				position.height + 2);

			EditorGUI.DrawRect(errorBarPosition, Color.red);
		}
	}

	public static class RefEditorGUI
	{
		public delegate bool DefaultPropertyFieldDelegate(Rect position, SerializedProperty property, GUIContent label);

		public delegate float DefaultPropertyFieldHeight(SerializedProperty property, GUIContent label);

		private static Dictionary<Type, Type> _fieldTypeToDrawerType;
		private static Type[] _propertyDrawerTypes;
		public static DefaultPropertyFieldDelegate DefaultPropertyField;
		public static DefaultPropertyFieldHeight DefaultPropertyHeight;
		public static DefaultPropertyFieldDelegate VanillaPropertyField;
		static RefEditorGUI()
		{
			_propertyDrawerTypes = TypeCache.GetTypesDerivedFrom<PropertyDrawer>().ToArray();

			var t = typeof(EditorGUI);
			var delegateType = typeof(DefaultPropertyFieldDelegate);
			var m = t.GetMethod("DefaultPropertyField", BindingFlags.Static | BindingFlags.NonPublic);
			VanillaPropertyField = (DefaultPropertyFieldDelegate)System.Delegate.CreateDelegate(delegateType, m);

			_fieldTypeToDrawerType = new Dictionary<Type, Type>();
			DefaultPropertyHeight = (property, label) =>
			{
				var parentType = property.serializedObject.targetObject.GetType();
				var field = parentType.GetField(property.propertyPath);

				var fieldType = GetPropertyType(property);
				if (!_fieldTypeToDrawerType.ContainsKey(fieldType))
				{
					var drawerType = GetPropertyDrawerType(fieldType);
					_fieldTypeToDrawerType.Add(fieldType, drawerType);
				}

				var foundDrawerType = _fieldTypeToDrawerType[fieldType];
				if (foundDrawerType == null)
				{
					return EditorGUI.GetPropertyHeight(property, label);
				}
				else
				{
					var instance = (PropertyDrawer)Activator.CreateInstance(foundDrawerType);
					return instance.GetPropertyHeight(property, label);
				}
			};
			DefaultPropertyField = (position, property, label) =>
			{
				var parentType = property.serializedObject.targetObject.GetType();
				var field = parentType.GetField(property.propertyPath);

				var fieldType = GetPropertyType(property);
				if (!_fieldTypeToDrawerType.ContainsKey(fieldType))
				{
					var drawerType = GetPropertyDrawerType(fieldType);
					_fieldTypeToDrawerType.Add(fieldType, drawerType);
				}

				var foundDrawerType = _fieldTypeToDrawerType[fieldType];
				if (foundDrawerType == null)
				{
					EditorGUI.BeginProperty(position, label, property);
					EditorGUI.PropertyField(position, property, label, true);
					EditorGUI.EndProperty();
				}
				else
				{
					var instance = (PropertyDrawer)Activator.CreateInstance(foundDrawerType);
					instance.OnGUI(position, property, label);

				}
				return true;
			};

		}

		static Type GetPropertyType(SerializedProperty prop)
		{
			//gets parent type info
			string[] slices = prop.propertyPath.Split('.');
			System.Type type = prop.serializedObject.targetObject.GetType();

			for (int i = 0; i < slices.Length; i++)
			{
				if (slices[i] == "Array")
				{
					i++; //skips "data[x]"
					type = type.GetElementType() ?? type.GetGenericArguments()[0]; //gets info on array elements
				}

				//gets info on field and its type
				else
				{
					type = type.GetField(slices[i],
						  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy |
						  BindingFlags.Instance)
					   .FieldType;

				}
			}

			return type;
		}


		static Type GetPropertyDrawerType(Type fieldType)
		{
			return _propertyDrawerTypes.FirstOrDefault(drawerType =>
			{
				var attributes = drawerType.GetCustomAttributes<CustomPropertyDrawer>();
				var attribute = attributes.FirstOrDefault();
				//var attribute = drawerType.GetCustomAttribute<CustomPropertyDrawer>();
				if (attribute == null) return false;

				var typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);
				var useChildrenField = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.Instance | BindingFlags.NonPublic);
				var drawerTargetType = (Type)typeField?.GetValue(attribute);
				var drawerChildren = (bool)useChildrenField?.GetValue(attribute);

				bool match;
				if (drawerChildren)
				{
					match = drawerTargetType.IsAssignableFrom(fieldType);
				}
				else
				{
					match = fieldType == drawerTargetType;
				}
				return match;
			});
		}
	}
}
