using Beamable.Common;
using Beamable.Editor.Common;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class StylePropertyModel
	{
		private readonly Action<string> _removePropertyAction;
		public event Action Change;
		public BussStyleSheet StyleSheet { get; }
		public BussStyleRule StyleRule { get; }
		public BussPropertyProvider PropertyProvider { get; }
		public VariableDatabase VariablesDatabase { get; }
		public PropertySourceTracker PropertySourceTracker { get; }
		public BussElement InlineStyleOwner { get; }

		public bool IsVariable => PropertyProvider.IsVariable;
		public bool IsInStyle => IsInline || (StyleRule != null && StyleRule.Properties.Contains(PropertyProvider));
		public bool IsWritable => IsInline || (StyleSheet != null && StyleSheet.IsWritable);
		private bool IsInline => InlineStyleOwner != null;

		public StylePropertyModel(BussStyleSheet styleSheet,
		                          BussStyleRule styleRule,
		                          BussPropertyProvider propertyProvider,
		                          VariableDatabase variablesDatabase,
		                          PropertySourceTracker propertySourceTracker,
		                          BussElement inlineStyleOwner,
		                          Action<string> removePropertyAction)
		{
			_removePropertyAction = removePropertyAction;
			StyleSheet = styleSheet;
			StyleRule = styleRule;
			PropertyProvider = propertyProvider;
			VariablesDatabase = variablesDatabase;
			PropertySourceTracker = propertySourceTracker;
			InlineStyleOwner = inlineStyleOwner;
		}

		public void LabelClicked(MouseDownEvent evt)
		{
			if (StyleSheet != null && !StyleSheet.IsWritable)
			{
				return;
			}

			List<GenericMenuCommand> commands = new List<GenericMenuCommand>
			{
				new GenericMenuCommand(Constants.Features.Buss.MenuItems.REMOVE, ()=>
				{
					_removePropertyAction?.Invoke(PropertyProvider.Key);
				})
			};

			GenericMenu context = new GenericMenu();

			foreach (GenericMenuCommand command in commands)
			{
				GUIContent label = new GUIContent(command.Name);
				context.AddItem(new GUIContent(label), false, () => { command.Invoke(); });
			}

			context.ShowAsContext();
		}

		public void HandlePropertyChanged()
		{
			if (PropertyProvider.IsVariable)
			{
				VariablesDatabase.SetVariableDirty(PropertyProvider.Key);
			}
			else if (PropertyProvider.GetProperty() is VariableProperty vp)
			{
				VariablesDatabase.SetVariableDirty(vp.VariableName);
			}
			else
			{
				VariablesDatabase.SetPropertyDirty(StyleSheet, StyleRule, PropertyProvider);
			}

			if (!IsInStyle)
			{
				if (StyleRule.TryAddProperty(PropertyProvider.Key, PropertyProvider.GetProperty()))
				{
					StyleSheet.TriggerChange();
				}
			}

			Change?.Invoke();
		}
	}
}
