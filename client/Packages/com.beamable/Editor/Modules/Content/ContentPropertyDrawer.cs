using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.Editor.Content
{

#if !BEAMABLE_NO_REF_DRAWERS
	[CustomPropertyDrawer(typeof(BaseContentRef), true)]
#endif
	public class ContentRefPropertyDrawer : PropertyDrawer
	{
		private static BeamEditorContext _beamable;
		private static bool _requestedBeamable;
		private static Dictionary<Type, HashSet<string>> _typeToContent = new Dictionary<Type, HashSet<string>>();
		private static Dictionary<Type, double> _typeToRefreshAt = new Dictionary<Type, double>();

		public static void ResetContent()
		{
			_typeToContent = new Dictionary<Type, HashSet<string>>();
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
			var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
			var fieldValue = GetTargetObjectOfProperty(property) as BaseContentRef;

			if (fieldValue == null) return;

			label.tooltip = PropertyDrawerHelper.SetTooltipWithFallback(fieldInfo, property);

			var idVal = fieldValue.GetId();
			idVal = string.IsNullOrEmpty(idVal)
			   ? "<none>"
			   : idVal;

			var referenceType = fieldValue.GetReferencedBaseType();
			var time = EditorApplication.timeSinceStartup;
			if (_beamable == null)
			{
				if (!_requestedBeamable)
				{
					_requestedBeamable = true;
					var beamable = BeamEditorContext.Default;
					_beamable = beamable;
				}
			}
			else if (!_typeToContent.ContainsKey(referenceType) || time > _typeToRefreshAt[referenceType])
			{
				_typeToRefreshAt[referenceType] = time + 5; // every five seconds; rescan
				
				var allContent =
				   new HashSet<string>(_beamable.CliContentService.GetContentsFromType(referenceType, true).Select(x => x.FullId));
				_typeToContent[referenceType] = allContent;
			}

			if (_typeToContent.ContainsKey(referenceType) && !_typeToContent[referenceType].Contains(idVal) && !idVal.Contains("(missing)"))
			{
				idVal += " (missing)";
			}

			EditorGUI.PrefixLabel(labelRect, label);
			var buttonClick = EditorGUI.DropdownButton(fieldRect, new GUIContent(idVal, label.tooltip), FocusType.Keyboard);
			if (buttonClick)
			{
				var wnd = ScriptableObject.CreateInstance<ContentRefSearchWindow>();
				wnd.Property = property;
				wnd.FieldInfo = fieldInfo;
				wnd.FieldValue = fieldValue;
				wnd.Label = label;
				wnd.Object = property.serializedObject.targetObject;

				wnd.Init();
				var xy = EditorGUIUtility.GUIToScreenPoint(new Vector2(fieldRect.x, fieldRect.y));
				wnd.ShowAsDropDown(new Rect((int)xy.x, (int)xy.y + fieldRect.height, 0, 0),
				   new Vector2(fieldRect.width, 300));
			}
		}
		public static object GetTargetObjectOfProperty(SerializedProperty prop)
		   => GetTargetObjectOfProperty(prop, prop.serializedObject.targetObject);
		private static object GetTargetObjectOfProperty(SerializedProperty prop, object obj)
		{
			if (prop == null) return null;
			var path = prop.propertyPath.Replace(".Array.data[", "[");
			var elements = path.Split('.');

			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "")
					   .Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else
				{
					obj = GetValue_Imp(obj, element);
				}
			}

			return obj;
		}
		public static IEnumerable<object> GetTargetObjectsOfProperty(SerializedProperty prop)
		{
			var targetObjectsOfProperty = new object[prop.serializedObject.targetObjects.Length];
			for (var i = 0; i < prop.serializedObject.targetObjects.Length; i++)
			{
				var obj = GetTargetObjectOfProperty(prop, prop.serializedObject.targetObjects[i]);
				if (obj != null)
				{
					targetObjectsOfProperty[i] = obj;
				}
			}
			return targetObjectsOfProperty;
		}
		public static object GetTargetParentObjectOfProperty(SerializedProperty prop, int parentsUp = 1)
		{
			if (prop == null) return null;

			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			elements = elements.Take(elements.Length - parentsUp).ToArray();
			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "")
					   .Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else
				{
					obj = GetValue_Imp(obj, element);
				}
			}

			return obj;
		}

		private static object GetValue_Imp(object source, string name)
		{
			if (source == null)
				return null;
			var type = source.GetType();

			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name,
				   BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}

			return null;
		}
		private static object GetValue_Imp(object source, string name, int index)
		{
			var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();
			//while (index-- >= 0)
			//    enm.MoveNext();
			//return enm.Current;

			for (int i = 0; i <= index; i++)
			{
				if (!enm.MoveNext()) return null;
			}

			return enm.Current;
		}
	}

	/// <summary>
	/// One selectable row in a content search popup.
	/// </summary>
	public struct ContentSearchOption
	{
		public string DisplayName;
		public string DisplayNameLower;
		public string Id;
	}

	/// <summary>
	/// Base class for the searchable content-id dropdown popups. Owns the shared search bar, the
	/// virtualized scroll list (only the rows intersecting the viewport are drawn, so it scales to
	/// thousands of entries) and the selection/validation plumbing. Subclasses only supply the option
	/// list and how a pick is written back. The windowing approach mirrors
	/// <c>ContentWindow_ItemsPanel</c>.
	/// </summary>
	public abstract class ContentSearchWindowBase : EditorWindow, IDelayedActionWindow
	{
		public SerializedProperty Property { get; set; }
		public Object Object { get; set; }

		protected readonly SearchData _searchData = new SearchData();

		private Vector2 _scrollPos;
		private string _searchString;
		private bool _initialized;
		private GUIStyle _normalStyle, _activeStyle;
		private List<ContentSearchOption> _options;
		private List<ContentSearchOption> _filtered;
		private string _filterKey;
		private bool _filterKeyValid;
		private int _selectedIndex;
		private Texture2D _highlightTexture;
		private Texture2D _activeTexture;
		public List<Action> delayedActions = new List<Action>();

		private const float ScrollBarWidth = 18f;
		private const float ToolbarHeight = 30f;

		/// <summary>Search field control name; unique per window type so focus is not shared.</summary>
		protected abstract string SearchControlName { get; }

		/// <summary>The full, unfiltered option list. Called once after Beamable is initialized.</summary>
		protected abstract List<ContentSearchOption> BuildOptions();

		/// <summary>The current value, used to pre-select / scroll a row into view on open.</summary>
		protected abstract string CurrentId { get; }

		/// <summary>Writes the chosen option into the backing property/object.</summary>
		protected abstract void ApplySelection(ContentSearchOption option);

		public async void Init()
		{
			await BeamEditorContext.Default.InitializePromise;

			_options = BuildOptions();

			var currId = CurrentId;
			_selectedIndex = 0;
			if (!string.IsNullOrEmpty(currId))
			{
				_selectedIndex = Mathf.Max(0, _options.FindIndex(o => currId.Equals(o.Id)));
			}
			_scrollPos = new Vector2(0, _selectedIndex * EditorGUIUtility.singleLineHeight);

			_highlightTexture = MakeTex(1, 1, new Color(0, 0, 0, .1f));
			_activeTexture = MakeTex(1, 1, new Color(0, .5f, 1, .2f));

			_activeStyle = new GUIStyle(GUI.skin.label)
			{
				normal = { background = _activeTexture },
				hover = { background = _highlightTexture },
				wordWrap = false
			};

			_normalStyle = new GUIStyle(GUI.skin.label)
			{
				hover = { background = _highlightTexture },
				wordWrap = false
			};

			// Repaint when the search text changes instead of every frame.
			_searchData.onEndCheck = Repaint;

			wantsMouseMove = true;
			_initialized = true;
			Repaint();
		}

		private static Texture2D MakeTex(int width, int height, Color col)
		{
			var pix = new Color[width * height];
			for (var i = 0; i < pix.Length; i++)
				pix[i] = col;

			var result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();

			return result;
		}

		protected void OnGUI()
		{
			if (!_initialized)
			{
				EditorGUILayout.PrefixLabel("...fetching");
				return;
			}

			GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"), GUILayout.Height(ToolbarHeight));
			this.DrawSearchBar(_searchData, textFieldName: SearchControlName);
			_searchString = _searchData.searchText;
			GUI.FocusControl(SearchControlName);
			GUILayout.EndHorizontal();

			// Re-filter only when the search text actually changes, not on every repaint.
			if (!_filterKeyValid || !string.Equals(_filterKey, _searchString, StringComparison.Ordinal))
			{
				_filterKey = _searchString;
				_filterKeyValid = true;
				if (string.IsNullOrEmpty(_searchString))
				{
					_filtered = _options;
				}
				else
				{
					var searchLower = _searchString.ToLower();
					_filtered = _options.Where(o => o.DisplayNameLower.Contains(searchLower)).ToList();
				}
			}

			var rowHeight = EditorGUIUtility.singleLineHeight;
			var areaRect = GUILayoutUtility.GetRect(position.width, position.height - ToolbarHeight);
			var totalHeight = _filtered.Count * rowHeight;
			var contentRect = new Rect(0, 0, areaRect.width - ScrollBarWidth, totalHeight);

			// Handle the scroll wheel manually so the popup does not reuse a stale scroll id.
			if (Event.current.type == EventType.ScrollWheel && areaRect.Contains(Event.current.mousePosition))
			{
				_scrollPos.y = Mathf.Clamp(_scrollPos.y + Event.current.delta.y * 10f, 0,
				   Mathf.Max(0, totalHeight - areaRect.height));
				Event.current.Use();
				Repaint();
			}

			_scrollPos = GUI.BeginScrollView(areaRect, _scrollPos, contentRect, false, true);

			// Only draw the rows intersecting the viewport -> O(visible) instead of O(all options).
			var first = Mathf.Max(0, Mathf.FloorToInt(_scrollPos.y / rowHeight));
			var last = Mathf.Min(_filtered.Count - 1, first + Mathf.CeilToInt(areaRect.height / rowHeight) + 1);

			var clicked = -1;
			for (var i = first; i <= last; i++)
			{
				var rowRect = new Rect(0, i * rowHeight, contentRect.width, rowHeight);
				var style = i == _selectedIndex ? _activeStyle : _normalStyle;
				if (GUI.Button(rowRect, _filtered[i].DisplayName, style))
				{
					clicked = i;
				}
			}

			GUI.EndScrollView();

			if (clicked >= 0)
			{
				Undo.RecordObject(Object, "Change Content Reference");
				ApplySelection(_filtered[clicked]);
				EditorUtility.SetDirty(Object);

				foreach (var targetObject in Property.serializedObject.targetObjects)
				{
					if (targetObject is ContentObject contentObject)
					{
						contentObject.ForceValidate();
					}
				}

				Close();
			}

			Property.serializedObject.UpdateIfRequiredOrScript();

			// Reactive hover/selection without burning a repaint every frame.
			if (Event.current.type == EventType.MouseMove)
			{
				Repaint();
			}

			foreach (var act in delayedActions)
			{
				act?.Invoke();
			}
			delayedActions.Clear();
		}

		public void AddDelayedAction(Action act)
		{
			delayedActions.Add(act);
		}
	}

	/// <summary>
	/// Searchable dropdown popup for <see cref="BaseContentRef"/> fields.
	/// </summary>
	public class ContentRefSearchWindow : ContentSearchWindowBase
	{
		public BaseContentRef FieldValue { get; set; }
		public FieldInfo FieldInfo { get; set; }
		public GUIContent Label { get; set; }

		protected override string SearchControlName => "contentRefSearchBar";

		protected override string CurrentId => FieldValue.GetId();

		protected override List<ContentSearchOption> BuildOptions()
		{
			var referenceType = FieldValue.GetReferencedBaseType();
			var contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
			var typeName = contentTypeReflectionCache.TypeToName(referenceType);

			var contentEntries = BeamEditorContext.Default.CliContentService.GetContentsFromType(referenceType, true);

			var options = new List<ContentSearchOption>
			{
				new ContentSearchOption { DisplayName = "<none>", DisplayNameLower = "none", Id = null }
			};
			foreach (var content in contentEntries)
			{
				var displayName = content.FullId.Substring(typeName.Length + 1);
				options.Add(new ContentSearchOption
				{
					Id = content.FullId,
					DisplayName = displayName,
					DisplayNameLower = displayName.ToLower()
				});
			}

			return options;
		}

		protected override void ApplySelection(ContentSearchOption option)
		{
			var fieldValues = ContentRefPropertyDrawer.GetTargetObjectsOfProperty(Property);
			foreach (BaseContentRef fieldValue in fieldValues)
			{
				fieldValue.SetId(option.Id);
			}

			Property.serializedObject.Update();
		}
	}

	/// <summary>
	/// Searchable dropdown popup for plain <see cref="string"/> content-id fields decorated with
	/// <see cref="Beamable.Common.Content.Validation.MustReferenceContent"/>. Writes the selected id
	/// straight into the string <see cref="SerializedProperty"/> instead of going through a
	/// <see cref="BaseContentRef"/>.
	/// </summary>
	public class ContentStringSearchWindow : ContentSearchWindowBase
	{
		public Type[] AllowedTypes { get; set; }
		public bool AllowNull { get; set; }

		protected override string SearchControlName => "contentStringSearchBar";

		protected override string CurrentId => Property.stringValue;

		protected override List<ContentSearchOption> BuildOptions()
		{
			var de = BeamEditorContext.Default;

			// Collect candidate manifest entries: union of AllowedTypes (incl. sub-types), or
			// all content when no type constraint is supplied.
			var entries = new List<LocalContentManifestEntry>();
			if (AllowedTypes != null && AllowedTypes.Length > 0)
			{
				foreach (var type in AllowedTypes)
				{
					entries.AddRange(de.CliContentService.GetContentsFromType(type, true));
				}
			}
			else
			{
				entries.AddRange(de.CliContentService.EntriesCache.Values);
			}

			var options = new List<ContentSearchOption>();
			var seen = new HashSet<string>();
			foreach (var content in entries)
			{
				if (string.IsNullOrEmpty(content.FullId) || !seen.Add(content.FullId))
				{
					continue;
				}

				options.Add(new ContentSearchOption
				{
					Id = content.FullId,
					DisplayName = content.FullId,
					DisplayNameLower = content.FullId.ToLower()
				});
			}

			options.Sort((a, b) => string.CompareOrdinal(a.DisplayNameLower, b.DisplayNameLower));

			if (AllowNull)
			{
				options.Insert(0, new ContentSearchOption
				{
					DisplayName = "<none>",
					DisplayNameLower = "none",
					Id = string.Empty
				});
			}

			return options;
		}

		protected override void ApplySelection(ContentSearchOption option)
		{
			Property.stringValue = option.Id;
			Property.serializedObject.ApplyModifiedProperties();
		}
	}

}
