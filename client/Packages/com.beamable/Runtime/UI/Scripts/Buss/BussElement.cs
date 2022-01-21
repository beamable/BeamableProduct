using Beamable.Editor.UI.Buss;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.UI.Buss
{
	[ExecuteAlways, DisallowMultipleComponent]
	public class BussElement : MonoBehaviour, ISerializationCallbackReceiver
	{
#pragma warning disable CS0649
		[SerializeField, BussId] private string _id;
		[SerializeField, BussClass] private List<string> _classes = new List<string>();
		[SerializeField] private BussStyleDescription _inlineStyle;
		[SerializeField] private BussStyleSheet _styleSheet;
		private List<string> _pseudoClasses = new List<string>();
		[SerializeField, HideInInspector] private List<Object> _assetReferences = new List<Object>();

		[SerializeField, HideInInspector] private BussElement _parent;
		[SerializeField, HideInInspector] private List<BussElement> _children = new List<BussElement>();
#pragma warning restore CS0649

		public List<BussStyleSheet> AllStyleSheets { get; } = new List<BussStyleSheet>();
		public BussStyle Style { get; } = new BussStyle();

		public string Id
		{
			get
			{
				return _id;
			}
			set
			{
				_id = value;
				OnStyleChanged();
			}
		}
		public IEnumerable<string> Classes => _classes;
		public IEnumerable<string> PseudoClasses => _pseudoClasses;
		public string TypeName => GetType().Name;
		public Dictionary<string, BussStyle> PseudoStyles { get; } = new Dictionary<string, BussStyle>();

		public BussStyleDescription InlineStyle => _inlineStyle;
		public BussStyleSheet StyleSheet
		{
			get
			{
				return _styleSheet;
			}
			set
			{
				_styleSheet = value;
				OnStyleChanged();
			}
		}

		public BussElement Parent => _parent;

		private IReadOnlyList<BussElement> _childrenReadOnly;

		public IReadOnlyList<BussElement> Children
		{
			get
			{
				if (_childrenReadOnly == null)
				{
					if (_children == null)
					{
						_children = new List<BussElement>();
					}

					_childrenReadOnly = _children.AsReadOnly();
				}

				return _childrenReadOnly;
			}
		}

		public virtual void ApplyStyle()
		{
			// TODO: common style implementation for BUSS Elements, so: applying all properties that affect RectTransform
		}

		#region Unity Callbacks

		private void OnEnable()
		{
			CheckParent();
			OnStyleChanged();
		}

		private void Reset()
		{
			CheckParent();
			OnStyleChanged();
		}

		private void OnValidate()
		{
			CheckParent();
			OnStyleChanged();
		}

		private void OnTransformParentChanged()
		{
			CheckParent();
			OnStyleChanged();
		}

		private void OnDisable()
		{
			if (Parent != null)
			{
				Parent._children.Remove(this);
			}
			else
			{
				BussConfiguration.UseConfig(c => c.UnregisterObserver(this));
			}
		}

		#endregion

		#region Changing Classes

		public void AddClass(string className)
		{
			if (!_classes.Contains(className))
			{
				_classes.Add(className);
				RecalculateStyle();
			}
		}

		public void RemoveClass(string className)
		{
			if (_classes.Remove(className))
			{
				RecalculateStyle();
			}
		}

		public void SetClass(string className, bool enabled)
		{
			if (enabled)
			{
				AddClass(className);
			}
			else
			{
				RemoveClass(className);
			}
		}

		public void SetPseudoClass(string className, bool enabled)
		{
			var changed = false;
			if (enabled)
			{
				if (!_pseudoClasses.Contains(className))
				{
					_pseudoClasses.Add(className);
					changed = true;
				}
			}
			else
			{
				changed = _pseudoClasses.Remove(className);
			}

			if (changed)
			{
				Style.SetStyleAnimatedListener(ApplyStyle);
				Style.SetPseudoStyle(className, enabled);
			}
		}

		#endregion

		/// <summary>
		/// Used when the parent or the style sheet is changed.
		/// Recalculates the list of style sheets that are used for this BussElement and then recalculates BussStyle.
		/// </summary>
		public void OnStyleChanged()
		{
			RecalculateStyleSheets();
			RecalculateStyle();
		}

		public void RecalculateStyleSheets()
		{
			AllStyleSheets.Clear();
			AddParentStyleSheets(this);
		}

		private void AddParentStyleSheets(BussElement element)
		{
			if (element.Parent != null)
			{
				AddParentStyleSheets(element.Parent);
			}

			if (element.StyleSheet != null)
			{
				AllStyleSheets.Add(element.StyleSheet);
			}
		}

		/// <summary>
		/// Recalculates style for this and children BussElements.
		/// </summary>
		public void RecalculateStyle()
		{
			BussConfiguration.UseConfig(c => c.RecalculateStyle(this));
			ApplyStyle();

			foreach (BussElement child in Children)
			{
				if (child != null)
				{
					child.OnStyleChanged();
				}
			}
		}

		public void CheckParent()
		{
			var foundParent = (transform == null || transform.parent == null)
				? null
				: transform.parent.GetComponentInParent<BussElement>();
			if (Parent != null)
			{
				Parent._children.Remove(this);
			}
			else
			{
				BussConfiguration.UseConfig(c => c.UnregisterObserver(this));
			}

			_parent = foundParent;
			if (Parent == null)
			{
				BussConfiguration.UseConfig(c => c.RegisterObserver(this));
			}
			else
			{
				if (!Parent._children.Contains(this))
				{
					Parent._children.Add(this);
				}
			}
		}

		public void OnBeforeSerialize()
		{
			_assetReferences.Clear();
			_inlineStyle.PutAssetReferencesInReferenceList(_assetReferences);
		}

		public void OnAfterDeserialize()
		{
			_inlineStyle.AssignAssetReferencesFromReferenceList(_assetReferences);
		}
	}
}
