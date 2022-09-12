using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Buss
{
	public class ThemeManagerModel
	{
		public event Action Change;
		// public event Action StyleSheetChange;

		private readonly BussCardFilter _filter;

		public readonly Dictionary<BussElement, int> FoundElements = new Dictionary<BussElement, int>();

		public List<BussStyleSheet> StyleSheets { get; } = new List<BussStyleSheet>();

		public Dictionary<BussStyleRule, BussStyleSheet> FilteredRules =>
			_filter != null
				? _filter.FilteredAlt(StyleSheets, SelectedElement)
				: new Dictionary<BussStyleRule, BussStyleSheet>();

		public IEnumerable<BussStyleSheet> WritableStyleSheets
		{
			get
			{
#if BEAMABLE_DEVELOPER
				return StyleSheets ?? Enumerable.Empty<BussStyleSheet>();
#else
				return StyleSheets?.Where(s => !s.IsReadOnly) ?? Enumerable.Empty<BussStyleSheet>();
#endif
			}
		}

		public BussElement SelectedElement { get; private set; }

		public string SelectedElementId =>
			SelectedElement != null ? BussNameUtility.AsIdSelector(SelectedElement.Id) : String.Empty;

		public BussStyleSheet SelectedElementStyleSheet => SelectedElement != null ? SelectedElement.StyleSheet : null;
		public VariableDatabase VariableDatabase { get; }
		public PropertySourceDatabase PropertyDatabase { get; } = new PropertySourceDatabase();

		public ThemeManagerModel()
		{
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			Selection.selectionChanged += OnSelectionChanged;

			_filter = new BussCardFilter();
			VariableDatabase = new VariableDatabase(this);

			OnHierarchyChanged();
		}

		public void Clear()
		{
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			Selection.selectionChanged -= OnSelectionChanged;

			foreach (var styleSheet in StyleSheets)
			{
				styleSheet.Change -= OnStyleSheetChanged;
			}

			StyleSheets.Clear();

			foreach (var element in FoundElements)
			{
				element.Key.Change -= OnStyleSheetChanged;
			}

			FoundElements.Clear();
		}

		public void ForceRefresh()
		{
			Change?.Invoke();
		}

		public void NavigationElementClicked(BussElement element)
		{
			Selection.activeGameObject = Selection.activeGameObject == element.gameObject ? null : element.gameObject;
		}

		public void OnFocus()
		{
			Change?.Invoke();
		}

		public void OnIdChanged(string value)
		{
			if (SelectedElement == null)
			{
				return;
			}

			SelectedElement.Id = BussNameUtility.CleanString(value);

			EditorUtility.SetDirty(SelectedElement);
			Change?.Invoke();
		}

		public void OnStyleSheetSelected(UnityEngine.Object styleSheet)
		{
			if (SelectedElement == null)
			{
				return;
			}

			BussStyleSheet newStyleSheet = (BussStyleSheet)styleSheet;
			SelectedElement.StyleSheet = newStyleSheet;
			Change?.Invoke();
		}

		private void BussElementClicked(BussElement element)
		{
			SelectedElement = element;
			Change?.Invoke();
		}

		private void OnHierarchyChanged()
		{
			FoundElements.Clear();

			foreach (UnityEngine.Object foundObject in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
			{
				GameObject gameObject = (GameObject)foundObject;
				if (gameObject.transform.parent == null)
				{
					Traverse(gameObject, 0);
				}
			}

			Change?.Invoke();
		}

		private void OnObjectRegistered(BussElement registeredObject)
		{
			registeredObject.Change += OnStyleSheetChanged;

			BussStyleSheet styleSheet = registeredObject.StyleSheet;

			if (styleSheet == null) return;

			if (!StyleSheets.Contains(styleSheet))
			{
				StyleSheets.Add(styleSheet);
				styleSheet.Change += OnStyleSheetChanged;
			}
		}

		private void OnStyleSheetChanged()
		{
			VariableDatabase.ReconsiderAllStyleSheets();
			// Change?.Invoke();
		}

		private void OnSelectionChanged()
		{
			if (Selection.activeGameObject != null)
			{
				BussElement bussElement = Selection.activeGameObject.GetComponent<BussElement>();
				BussElementClicked(bussElement);
			}
			else
			{
				BussElementClicked(null);
			}
		}

		private void Traverse(GameObject gameObject, int currentLevel)
		{
			if (!gameObject) return;

			BussElement foundComponent = gameObject.GetComponent<BussElement>();

			if (foundComponent != null)
			{
				FoundElements.Add(foundComponent, currentLevel);
				OnObjectRegistered(foundComponent);

				foreach (Transform child in gameObject.transform)
				{
					Traverse(child.gameObject, currentLevel + 1);
				}
			}
			else
			{
				foreach (Transform child in gameObject.transform)
				{
					Traverse(child.gameObject, currentLevel);
				}
			}
		}

		#region Action bar buttons' actions

		public void OnAddStyleButtonClicked()
		{
			int styleSheetCount = WritableStyleSheets.Count();

			if (styleSheetCount == 0)
			{
				return;
			}

			if (styleSheetCount == 1)
			{
				CreateEmptyStyle(WritableStyleSheets.First());
			}
			else if (styleSheetCount > 1)
			{
				OpenAddStyleMenu(WritableStyleSheets);
			}
		}

		private void OpenAddStyleMenu(IEnumerable<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();
			context.AddItem(new GUIContent(ADD_STYLE_OPTIONS_HEADER), false, () => { });
			context.AddSeparator(string.Empty);
			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					CreateEmptyStyle(styleSheet);
				});
			}

			context.ShowAsContext();
		}

		private void CreateEmptyStyle(BussStyleSheet selectedStyleSheet, string selectorName = "*")
		{
			if (SelectedElement != null)
			{
				selectorName = BussNameUtility.GetLabel(SelectedElement);
			}

			BussStyleRule selector = BussStyleRule.Create(selectorName, new List<BussPropertyProvider>());
			selectedStyleSheet.Styles.Add(selector);
			selectedStyleSheet.TriggerChange();
			AssetDatabase.SaveAssets();

			Change?.Invoke();
		}

		public void OnCopyButtonClicked()
		{
			List<BussStyleSheet> readonlyStyles = StyleSheets.Where(styleSheet => styleSheet.IsReadOnly).ToList();
			OpenCopyMenu(readonlyStyles);
		}

		private void OpenCopyMenu(IEnumerable<BussStyleSheet> bussStyleSheets)
		{
			GenericMenu context = new GenericMenu();
			context.AddItem(new GUIContent(DUPLICATE_STYLESHEET_OPTIONS_HEADER), false, () => { });
			context.AddSeparator(string.Empty);
			foreach (BussStyleSheet styleSheet in bussStyleSheets)
			{
				context.AddItem(new GUIContent(styleSheet.name), false, () =>
				{
					NewStyleSheetWindow window = NewStyleSheetWindow.ShowWindow();
					if (window != null)
					{
						window.Init(styleSheet.Styles);
					}
				});
			}

			context.ShowAsContext();
		}

		public void OnDocsButtonClicked()
		{
			Application.OpenURL(URLs.Documentations.URL_DOC_BUSS_THEME_MANAGER);
		}

		public void OnSearch(string value)
		{
			_filter.CurrentFilter = value;
			Change?.Invoke();
		}

		#endregion

		public void AddInlineVariable()
		{
			if (SelectedElement == null)
			{
				return;
			}

			NewVariableWindow window = NewVariableWindow.ShowWindow();
			if (window != null)
			{
				window.Init(SelectedElement.InlineStyle, (key, property) =>
				{
					if (SelectedElement.InlineStyle.TryAddProperty(key, property))
					{
						// TODO: TD000004. We shouldn't need to call this from model. This should happen "under the hood". Subject for deeper refactor of buss core system.
						EditorUtility.SetDirty(SelectedElement);
						SelectedElement.RecalculateStyle();
						VariableDatabase.ReconsiderAllStyleSheets();
						Change?.Invoke();
					}
				});
			}
		}

		public void AddInlineProperty()
		{
			if (SelectedElement == null)
			{
				return;
			}

			var keys = new HashSet<string>();
			foreach (BussPropertyProvider propertyProvider in SelectedElement.InlineStyle.Properties)
			{
				keys.Add(propertyProvider.Key);
			}

			IOrderedEnumerable<string> sorted = BussStyle.Keys.OrderBy(k => k);
			var context = new GenericMenu();

			foreach (string key in sorted)
			{
				if (keys.Contains(key)) continue;
				Type baseType = BussStyle.GetBaseType(key);
				SerializableValueImplementationHelper.ImplementationData data =
					SerializableValueImplementationHelper.Get(baseType);
				IEnumerable<Type> types = data.subTypes.Where(t => t != null && t.IsClass && !t.IsAbstract &&
				                                                   t != typeof(FractionFloatBussProperty)).ToList();
				foreach (Type type in types)
				{
					var label = new GUIContent(types.Count() > 1 ? key + "/" + type.Name : key);
					context.AddItem(new GUIContent(label), false, () =>
					{
						if (SelectedElement.InlineStyle.TryAddProperty(
							    key, (IBussProperty)Activator.CreateInstance(type)))
						{
							// TODO: TD000004. We shouldn't need to call this from model. This should happen "under the hood". Subject for deeper refactor of buss core system.
							EditorUtility.SetDirty(SelectedElement);
							SelectedElement.RecalculateStyle();
							VariableDatabase.ReconsiderAllStyleSheets();
							Change?.Invoke();
						}
					});
				}
			}

			context.ShowAsContext();
		}

		public void RemoveInlineProperty(string value)
		{
			if (SelectedElement == null)
			{
				return;
			}

			var propertyProvider = SelectedElement.InlineStyle.Properties.Find(property => property.Key == value);

			if (propertyProvider != null)
			{
				SelectedElement.InlineStyle.Properties.Remove(propertyProvider);
				
				// TODO: TD000004. We shouldn't need to call this from model. This should happen "under the hood". Subject for deeper refactor of buss core system.
				EditorUtility.SetDirty(SelectedElement);
				SelectedElement.RecalculateStyle();
				VariableDatabase.ReconsiderAllStyleSheets();
				Change?.Invoke();
			}
		}
	}
}
