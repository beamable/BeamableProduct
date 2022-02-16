using Beamable.Theme;
using Beamable.Theme.Appliers;
using Beamable.Theme.Palettes;
using Beamable.UnityEngineClone.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using FontStyle = UnityEngine.FontStyle;
using static Beamable.Common.Constants.BeamableConstants;

namespace Beamable.Editor.Modules.Theme
{
	[Obsolete(Commons.OBSOLETE_BUSS_INTRODUCED)]
	public class ThemeWindow : EditorWindow
	{
		// [MenuItem(
		//    BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
		//    BeamableConstants.OPEN + " " +
		//    BeamableConstants.THEME_MANAGER,
		//    priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_2 + 5)]
		public static void Init()
		{
			var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			GetWindow<ThemeWindow>(MenuItems.Windows.Names.THEME_MANAGER, true, inspector);
		}

		private GameObject _lastRawSelection;

		private Dictionary<Type, IPaletteStyleObject> _styleToWrapper;
		private Dictionary<int, bool> _hashCodeToFoldout = new Dictionary<int, bool>();
		private Vector2 _hierachyScrollPosition, _sectionsScrollPosition;
		private bool _wasPressed = false;

		private Texture2D _blackPixel, _transparentPixel, _greyPixel;

		private GUIStyle _parentLabelStyle;
		private GUIStyle _backButtonStyle;
		private GUIStyle _backBarStyle;

		private readonly Vector2 windowMax = new Vector2(600, 750);
		private readonly Vector2 windowMin = new Vector2(335, 200);


		private void OnInspectorUpdate()
		{
			if (_lastRawSelection != Selection.activeObject)
			{
				Repaint();
			}
		}



		private void OnEnable()
		{
			this.maxSize = windowMax;
			this.minSize = windowMin;
			if (_blackPixel == null)
			{
				_blackPixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
				_blackPixel.SetPixel(0, 0, Color.black);
				_blackPixel.Apply();

				_transparentPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
				_transparentPixel.SetPixel(0, 0, Color.clear);
				_transparentPixel.Apply();

				_greyPixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
				_greyPixel.SetPixel(0, 0, new Color(.3f, .3f, .3f));
				_greyPixel.Apply();
			}

			_backBarStyle = new GUIStyle
			{
				normal = new GUIStyleState
				{
					background = _greyPixel,
				}
			};

			_backButtonStyle = new GUIStyle
			{
				normal = new GUIStyleState
				{
					background = _greyPixel,
					textColor = new Color(.8f, .8f, .8f)
				},
				active = new GUIStyleState { background = _greyPixel, textColor = Color.white },
				padding = new RectOffset(5, 5, 5, 5)
			};

			_parentLabelStyle = new GUIStyle
			{
				fontStyle = FontStyle.Bold,
				normal = new GUIStyleState { textColor = new Color(.8f, .8f, .8f) },
				active = new GUIStyleState { textColor = Color.white },
				padding = new RectOffset(2, 2, 3, 3)
			};

			RecreateStyleToWrapperTable();

		}

		private void OnDisable()
		{
			RecreateStyleToWrapperTable();
		}

		void RecreateStyleToWrapperTable()
		{
			_styleToWrapper = new Dictionary<Type, IPaletteStyleObject>
		 {
			{ typeof(ColorBinding), CreateInstance<ColorStyleObject>() },
			{ typeof(ImageBinding), CreateInstance<ImageStyleObject>() },
			{ typeof(ButtonBinding), CreateInstance<ButtonStyleObject>() },
			{ typeof(FontBinding), CreateInstance<FontStyleObject>() },
			{ typeof(GradientBinding), CreateInstance<GradientStyleObject>() },
			{ typeof(LayoutBinding), CreateInstance<LayoutStyleObject>() },
			{ typeof(WindowBinding), CreateInstance<WindowStyleObject>() },
			{ typeof(SelectableBinding), CreateInstance<SelectableStyleObject>() },
			{ typeof(StringBinding), CreateInstance<StringStyleObject>() },
			{ typeof(TextBinding), CreateInstance<TextStyleObject>() },
			{ typeof(TransformBinding), CreateInstance<TransformStyleObject>() },
			{ typeof(SoundBinding), CreateInstance<SoundStyleObject>() },
		 };
		}

		private void OnGUI()
		{

			var isPressed = GUILayout.Toggle(_wasPressed, "UI Skinning Mode");

			if (isPressed)
			{
				_wasPressed = isPressed;
				FilterSelection.SetSearchFilter("t:StyleBehaviour", 0);
			}
			else if (_wasPressed && !isPressed)
			{
				FilterSelection.SetSearchFilter("", 0);
				_wasPressed = isPressed;
			}

			var anySelection = TryGetSelected(out var styleBehaviour);
			EditorGUILayout.ObjectField("Selected", styleBehaviour, typeof(StyleBehaviour), true);

			RefactorHierarchy();


			if (!anySelection)
			{
				EditorGUILayout.LabelField("Select a style behaviour...");
				return;
			}


			EditorGUILayout.LabelField(styleBehaviour.name);
			CanHideAttribute.Hide = true;


			EditorGUI.indentLevel++;

			var anyEdit = false;
			_sectionsScrollPosition = EditorGUILayout.BeginScrollView(_sectionsScrollPosition, false, false);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledImages);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledButtons);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledGradients);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledLayouts);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledSelectables);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledSounds);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledStrings);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledTexts);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledTransforms);
			anyEdit |= RenderSection(styleBehaviour, styleBehaviour.StyledWindow);
			EditorGUILayout.EndScrollView();
			EditorGUI.indentLevel--;

			if (anyEdit)
			{
				ThemeConfiguration.Instance.Style.BumpVersion();
			}

			CanHideAttribute.Hide = false;


		}

		private bool RenderTreeFoldout(GameObject gob, float indent, float arrowWidth, bool hasChildren)
		{
			var key = gob.GetHashCode();
			if (!_hashCodeToFoldout.ContainsKey(key))
			{
				_hashCodeToFoldout.Add(key, false);
			}



			var value = _hashCodeToFoldout[key];

			var rect = EditorGUILayout.GetControlRect(false, 1f * EditorGUI.GetPropertyHeight(SerializedPropertyType.ObjectReference, GUIContent.none));
			var objectFieldRect = new Rect(rect.x + arrowWidth + indent, rect.y, rect.width - (arrowWidth + indent), rect.height);
			EditorGUI.ObjectField(objectFieldRect, gob, typeof(GameObject), true);

			if (hasChildren)
			{
				var foldoutRect = new Rect(rect.x + indent, rect.y, arrowWidth, rect.height);
				var nextValue = EditorGUI.Foldout(foldoutRect, value, "");
				_hashCodeToFoldout[key] = nextValue;
				return nextValue;
			}

			return false;

		}

		private void RenderGameObjectTree(GameObject gob, float indent)
		{
			var children = new List<StyleBehaviour>();
			var possibleNestedChildren = new List<GameObject>();
			for (var i = 0; i < gob.transform.childCount; i++)
			{
				var child = gob.transform.GetChild(i);
				var childStyle = child.GetComponent<StyleBehaviour>();
				if (childStyle != null)
				{
					children.Add(childStyle);
				}
				else
				{
					if (child.GetComponentInChildren<StyleBehaviour>() != null)
					{
						possibleNestedChildren.Add(child.gameObject);
					}
				}
			}
			if (RenderTreeFoldout(gob, indent, 15, children.Count > 0 || possibleNestedChildren.Count > 0))
			{
				foreach (var child in children)
				{
					RenderGameObjectTree(child.gameObject, indent + 20);
				}

				foreach (var child in possibleNestedChildren)
				{
					RenderGameObjectTree(child, indent + 20);
				}
			}
		}


		private void RefactorHierarchy()
		{
			if (_lastRawSelection == null) return;
			RenderParentBar();
			RenderGameObjectTree(_lastRawSelection, 0);
		}

		private void RenderParentBar()
		{
			GUILayout.BeginHorizontal(_backBarStyle);

			var parentChain = new List<StyleBehaviour>();

			var curr = _lastRawSelection.transform;
			while (curr != null)
			{
				var styler = curr.GetComponent<StyleBehaviour>();
				if (styler != null)
				{
					parentChain.Add(styler);
				}
				curr = curr.transform.parent;
			}

			parentChain.Reverse();

			if (parentChain.Count > 1)
			{
				var backClicked = GUILayout.Button("<", _backButtonStyle, GUILayout.Width(30));
				if (backClicked && _lastRawSelection != null && _lastRawSelection.transform.parent != null)
				{
					Selection.SetActiveObjectWithContext(_lastRawSelection.transform.parent.gameObject, null);
				}
			}
			else
			{
				// provide blank space to make up for the lack of back button
				GUILayout.Box("", _backButtonStyle, GUILayout.Width(30));
			}

			foreach (var parent in parentChain)
			{
				var displayName = parent.transform == _lastRawSelection.transform ? parent.name : $"{parent.name}  /";
				if (GUILayout.Button(displayName, _parentLabelStyle, GUILayout.ExpandWidth(false)))
				{
					Selection.SetActiveObjectWithContext(parent.gameObject, null);
				}
			}
			GUILayout.EndHorizontal();

		}

		private bool RenderBindingValue(FieldInfo bindingField, GeneralPaletteBinding bindingValue, bool recursive)
		{
			if (!bindingValue.HasName) return false;

			var anyEdit = false;
			var createPaletteCopierMethod = typeof(ThemeObject).GetMethod(nameof(ThemeObject.CloneParentPaletteStyle));

			var paletteStyleType = bindingValue.GetType().BaseType.GenericTypeArguments[0];
			var wrapper = _styleToWrapper[bindingField.FieldType];

			var copier = createPaletteCopierMethod.MakeGenericMethod(paletteStyleType)
			   .Invoke(ThemeConfiguration.Instance.Style, new object[] { bindingValue }) as IPaletteStyleCopier;
			var paletteStyle = copier.GetStyle();
			wrapper.SetStyle(paletteStyle);

			try
			{
				var so = (wrapper as ScriptableObject);
				var sow = new SerializedObject(so);

				EditorGUILayout.PropertyField(sow.FindProperty("Style"), new GUIContent(bindingField.Name + $" ({bindingValue.Name})"), true);

				sow.ApplyModifiedProperties();
				if (wrapper.Modified)
				{
					copier?.Commit();
					anyEdit = true;
				}
				EditorGUILayout.Space();
			}
			catch
			{
				// sometimes, the creation of the SerializedObject fails, and won't succeed until the style table is re-created ?
				RecreateStyleToWrapperTable();
			}

			var subBindings = paletteStyle.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
			   .Where(f => typeof(GeneralPaletteBinding).IsAssignableFrom(f.FieldType))
			   .ToList();

			if (subBindings.Count > 0 && recursive)
			{
				var foldKey = (bindingField.Name + "." + bindingValue.Name).GetHashCode();
				if (!_hashCodeToFoldout.ContainsKey(foldKey))
				{
					_hashCodeToFoldout.Add(foldKey, false);
				}

				var foldValue = _hashCodeToFoldout[foldKey];
				EditorGUI.indentLevel++;

				var nextFoldValue = (EditorGUILayout.Foldout(foldValue, "Sub-Bindings"));
				_hashCodeToFoldout[foldKey] = nextFoldValue;
				if (nextFoldValue)
				{
					EditorGUI.indentLevel++;
					foreach (var subBinding in subBindings)
					{
						var value = subBinding.GetValue(paletteStyle) as GeneralPaletteBinding;
						anyEdit |= RenderBindingValue(subBinding, value, recursive);
					}
					EditorGUI.indentLevel--;

				}
				EditorGUI.indentLevel--;

			}

			return anyEdit;
		}

		private bool RenderSection<TElement>(StyleBehaviour styleBehaviour, StyleApplier<TElement> applier)
		   where TElement : UIBehaviour
		{
			if (applier == null || applier.Components.Count == 0)
			{
				return false;
			}

			EditorGUILayout.LabelField(applier.GetType().Name);

			var bindingFields = applier.GetType()
			   .GetFields(BindingFlags.Instance | BindingFlags.Public)
			   .Where(f => typeof(GeneralPaletteBinding).IsAssignableFrom(f.FieldType))
			   .ToList();

			var anyEdit = false;
			foreach (var bindingField in bindingFields)
			{
				var bindingRaw = bindingField.GetValue(applier);
				var bindingValue = bindingRaw as GeneralPaletteBinding;
				anyEdit |= RenderBindingValue(bindingField, bindingValue, true);

				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}

			return anyEdit;
		}


		private bool TryGetSelected(out StyleBehaviour styleBehaviour)
		{
			var selected = Selection.activeGameObject;
			_lastRawSelection = selected;
			styleBehaviour = null;
			if (selected == null)
			{
				return false;
			}

			styleBehaviour = selected.GetComponent<StyleBehaviour>();
			return styleBehaviour != null;
		}
	}
}
