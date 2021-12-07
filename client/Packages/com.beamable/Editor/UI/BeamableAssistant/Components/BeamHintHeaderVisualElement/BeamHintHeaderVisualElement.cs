using Beamable.Editor.BeamableAssistant.Models;
using Beamable.Editor.UI.Components;
using Common.Runtime.BeamHints;
using Editor.BeamableAssistant.BeamHints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Experimental.UIElements;

namespace Beamable.Editor.BeamableAssistant.Components
{
	public class BeamHintHeaderVisualElement : BeamableAssistantComponent
	{
		public new class UxmlFactory : UxmlFactory<BeamHintHeaderVisualElement, UxmlTraits> { }

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription {name = "custom-text", defaultValue = "nada"};

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get
				{
					yield break;
				}
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as BeamHintHeaderVisualElement;
			}
		}

		public BeamHintsDataModel Model
		{
			get;
			internal set;
		}

		private int _indexIntoDisplayingHints;
		private Label _hintDisplayName;
		private Button _moreDetailsButton;
		private VisualElement _detailsContainer;

		private readonly BeamHintDetailsReflectionCache.Registry _hintDetailsReflectionCache;
		private BeamHintDetailsConfig _hintDetailsConfig;
		private int _indexIntoLoadedConverters;

		private readonly BeamHintsDataModel _hintDataModel;
		private BeamHintHeader _displayingHintHeader;

		public BeamHintHeaderVisualElement(BeamHintsDataModel dataModel, 
		                                   BeamHintDetailsReflectionCache.Registry library,
		                                   in BeamHintHeader hint, int headerIdx) : base(nameof(BeamHintHeaderVisualElement))
		{
			_hintDataModel = dataModel;
			_hintDetailsReflectionCache = library;
			UpdateFromBeamHintHeader(in hint, headerIdx);
		}

		public void UpdateFromBeamHintHeader(in BeamHintHeader hint, int headerIdx)
		{
			_displayingHintHeader = hint;
			_indexIntoLoadedConverters = _hintDetailsReflectionCache.GetFirstMatchingDetailsConfig(hint, out _hintDetailsConfig);
			_indexIntoDisplayingHints = headerIdx;
		}

		public override void Refresh()
		{
			base.Refresh();

			_hintDisplayName = Root.Q<Label>("hintDisplayName");
			_moreDetailsButton = Root.Q<Button>("moreDetailsButton");
			_detailsContainer = Root.Q<VisualElement>("hintDetailsContainer");
			
			// Update the hint's label
			_hintDisplayName.text = _displayingHintHeader.Id;

			// If there are no mapped converters, we don't display a more button since there would be no details to show.
			var detailsUxmlPath = _hintDetailsConfig.UxmlFile;
			var detailsUssPaths = _hintDetailsConfig.StylesheetsToAdd;
			
			// If there are no configured UXML Path or a Converter tied to the matching HintDetailsVisualConfig, simply disable the button.
			if (_indexIntoLoadedConverters == -1 || string.IsNullOrEmpty(detailsUxmlPath))
				_moreDetailsButton.visible = false;
			else
			{
				_moreDetailsButton.clickable.clicked += () =>
				{
					_detailsContainer.visible = !_detailsContainer.visible;
				};
				_detailsContainer.visible = false;
				
				// Ensure no null or empty paths exist in the configured USS files.
				detailsUssPaths.RemoveAll(string.IsNullOrEmpty);

				// Ensure paths exist.
				Assert.IsTrue(File.Exists(detailsUxmlPath), $"Cannot find {detailsUxmlPath}");
				Assert.IsTrue(detailsUssPaths.TrueForAll(File.Exists), $"Cannot find one of {string.Join(",", detailsUssPaths)}");

				// Load UXML for details and add it to the details container.
				var detailsTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(detailsUxmlPath);
				_detailsContainer.Add(detailsTreeAsset.CloneTree());

				// Create an new injection bag
				var injectionBag = new BeamHintVisualsInjectionBag();

				// Call the converter to fill up this injection bag.
				var converter = _hintDetailsReflectionCache.GetConverterAtIdx(_indexIntoLoadedConverters);
				var beamHint = _hintDataModel.GetHint(_displayingHintHeader);
				converter.Invoke(in beamHint, in _hintDetailsConfig, injectionBag);
 
				// Resolve all supported injections.
				ResolveInjections(injectionBag.TextInjections, _detailsContainer); 
				ResolveInjections(injectionBag.ParameterlessActionInjections, _detailsContainer);
			}

		}

		

		public void ResolveInjections<T>(IEnumerable<BeamHintVisualsInjectionBag.Injection<T>> injections, VisualElement container)
		{
			foreach (var injection in injections)
			{
				// Finds all elements
				var query = injection.Query;
				var queryExpectedType = query.ExpectedType;
				var queriedElements = container
				                     .Query(query.Name, query.Classes)
				                     .Where(element => element.GetType() == queryExpectedType)
				                     .Build()
				                     .ToList();

				System.Diagnostics.Debug.Assert(queriedElements.Count != 0,
				                                $"Query [{query}] found no matches when searching in the {nameof(VisualElement)} [{container.name}]");

				System.Diagnostics.Debug.Assert(queriedElements.TrueForAll(element => element.GetType() == queryExpectedType),
				                                $"Query [{query}] does not match its expected type when searching in the {nameof(VisualElement)} [{container.name}]");

				
				
				// For each found element, inject based on the type of element and the type of the injection
				foreach (var queriedElement in queriedElements) 
					Inject(queriedElement, injection);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Inject<T>(VisualElement matchedElement, BeamHintVisualsInjectionBag.Injection<T> toInject)
		{
			switch (matchedElement)
			{
				case Button button:
				{
					ResolveButtonInjection(toInject, button);
					break;
				}
				case Label label:
				{
					ResolveLabelInjection(toInject, label);
					break;
				}
				default:
					break;
			}
		}

		private static void ResolveLabelInjection<T>(BeamHintVisualsInjectionBag.Injection<T> toInject, Label label)
		{
			switch (toInject.ObjectToInject)
			{
				case Action clicked:
				{
					label.RegisterCallback(new EventCallback<MouseUpEvent>(evt => clicked?.Invoke()));
					break;
				}
				case string text:
				{
					label.text = text;
					break;
				}
				default:
					throw new ArgumentException();
			}
		}

		private static void ResolveButtonInjection<T>(BeamHintVisualsInjectionBag.Injection<T> toInject, Button button)
		{
			switch (toInject.ObjectToInject)
			{
				case Action clicked:
				{
					button.clickable.clicked += clicked;
					break;
				}
				case string label:
				{
					button.text = label;
					break;
				}
				default:
					throw new ArgumentException($"Unsupported Injection Type !!!!");
			}
		}
	}

	public readonly struct VisualElementsQuery
	{
		public readonly Type ExpectedType;

		public readonly string Name;
		public readonly string[] Classes;

		public VisualElementsQuery(Type expectedType, string name, string[] classes)
		{
			ExpectedType = expectedType;
			Name = name;
			Classes = classes;
		}

		public override string ToString()
		{
			return $"{nameof(ExpectedType)}: {ExpectedType}, {nameof(Name)}: {Name}, {nameof(Classes)}: {Classes}";
		}
	}

	public readonly struct LocalizationInstance
	{
		public readonly string LocalizationId;
		public readonly object[] LocalizationParams;
		
		public LocalizationInstance(string localizationId, object[] localizationParams)
		{
			LocalizationId = localizationId;
			LocalizationParams = localizationParams;
		}
	}

	public class BeamHintVisualsInjectionBag
	{
		public readonly IEnumerable<Injection<string>> TextInjections;
		public readonly IEnumerable<Injection<Action>> ParameterlessActionInjections;

		private readonly List<Injection<string>> _textInjections;
		private readonly List<Injection<Action>> _parameterlessActionInjections;

		public BeamHintVisualsInjectionBag()
		{
			_textInjections = new List<Injection<string>>();
			_parameterlessActionInjections = new List<Injection<Action>>();
			TextInjections = new TextIterator(this);
			ParameterlessActionInjections = new ParameterlessActionIterator(this);
		}
		
		public readonly struct Injection<T>
		{
			public readonly VisualElementsQuery Query;
			public readonly T ObjectToInject;

			public Injection(VisualElementsQuery query, T objectToInject)
			{
				Query = query;
				ObjectToInject = objectToInject;
			}
		}

		public class TextIterator : IEnumerable<Injection<string>>
		{
			public readonly BeamHintVisualsInjectionBag bag;

			public TextIterator(BeamHintVisualsInjectionBag beamHintVisualsInjectionBag)
			{
				bag = beamHintVisualsInjectionBag;
			}

			public IEnumerator<Injection<string>> GetEnumerator()
			{
				return bag._textInjections.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public class ParameterlessActionIterator : IEnumerable<Injection<Action>>
		{
			public readonly BeamHintVisualsInjectionBag bag;

			public ParameterlessActionIterator(BeamHintVisualsInjectionBag beamHintVisualsInjectionBag)
			{
				bag = beamHintVisualsInjectionBag;
			}

			public IEnumerator<Injection<Action>> GetEnumerator()
			{
				return bag._parameterlessActionInjections.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public void SetButtonLabel(string buttonLabel, string name, params string[] classes)
		{
			_textInjections.Add(new Injection<string>(new VisualElementsQuery(typeof(Button), name, classes), buttonLabel));
		}
		
		public void SetButtonLabel(LocalizationInstance buttonLabel, string name, params string[] classes)
		{
			//_textInjections.Add(new Injection<string>(new VisualElementsQuery(typeof(Button), name, classes), buttonLabel));
		}

		public void SetButtonClicked(Action buttonAction, string name, params string[] classes)
		{
			_parameterlessActionInjections.Add(new Injection<Action>(new VisualElementsQuery(typeof(Button), name, classes), buttonAction));
		}

		public void SetLabel(string buttonLabel, string name, params string[] classes)
		{
			_textInjections.Add(new Injection<string>(new VisualElementsQuery(typeof(Label), name, classes), buttonLabel));
		}

		public void SetLabelClicked(Action buttonAction, string name, params string[] classes)
		{
			_parameterlessActionInjections.Add(new Injection<Action>(new VisualElementsQuery(typeof(Label), name, classes), buttonAction));
		}

		
	}
}
