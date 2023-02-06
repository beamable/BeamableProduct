using Beamable.Common.Dependencies;
using Beamable.UI.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public partial class BussStyle
	{
		#region Property Binders

		private static Dictionary<string, IPropertyBinding> _bindings = new Dictionary<string, IPropertyBinding>();

		/// <summary>
		/// Get the <see cref="IPropertyBinding"/> structure for a given property name
		/// </summary>
		/// <param name="propertyKey">the name of the css property that the binding was created for. for example, "backgroundColor"</param>
		/// <param name="binding">the binding will be be bound to this variable after the method has run. Will be null if the binding wasn't found</param>
		/// <returns>true if the binding was found, false otherwise. </returns>
		public static bool TryGetBinding(string propertyKey, out IPropertyBinding binding)
		{
			return _bindings.TryGetValue(propertyKey, out binding);
		}

		private static IVertexColorBussProperty[] _defaultVertexColorValues = new IVertexColorBussProperty[]
		{
			new SingleColorBussProperty(), new VertexColorBussProperty()
		};
		private static IVertexColorBussProperty[] _defaultTransparentVertexColorValues = new IVertexColorBussProperty[]
		{
			new SingleColorBussProperty(Color.clear), new VertexColorBussProperty(Color.clear)
		};
		
		protected readonly Dictionary<string, IBussProperty> _properties = new Dictionary<string, IBussProperty>();

		public static readonly PropertyBinding<MainTextureBussProperty> MainTextureSource =
			new PropertyBinding<MainTextureBussProperty>("mainTexture", new MainTextureBussProperty(MainTextureBussProperty.Options.SdfSprite));

		// Shape
		public static readonly PropertyBinding<IFloatBussProperty> Threshold =
			new PropertyBinding<IFloatBussProperty>("threshold", new FloatBussProperty());

		public static readonly PropertyBinding<ISpriteBussProperty> SdfImage =
			new PropertyBinding<ISpriteBussProperty>("sdfImage", new SpriteBussProperty());

		public static readonly PropertyBinding<SdfModeBussProperty> SdfMode =
			new PropertyBinding<SdfModeBussProperty>("sdfMode", new SdfModeBussProperty());

		public static readonly PropertyBinding<ImageTypeBussProperty> ImageType =
			new PropertyBinding<ImageTypeBussProperty>("imageType", new ImageTypeBussProperty());

		public static readonly PropertyBinding<FloatBussProperty> PixelsPerUnitMultiplier =
			new PropertyBinding<FloatBussProperty>("pixelsPerUnitMultiplier", new FloatBussProperty(1f));

		public static readonly PropertyBinding<NineSliceSourceBussProperty> NineSliceSource =
			new PropertyBinding<NineSliceSourceBussProperty>("nineSliceSource", new NineSliceSourceBussProperty(Sdf.SdfImage.NineSliceSource.SdfFirst));

		// Background
		public static readonly PropertyBinding<IVertexColorBussProperty> BackgroundColor =
			new PropertyBinding<IVertexColorBussProperty>("backgroundColor", _defaultVertexColorValues);

		public static readonly PropertyBinding<IFloatFromFloatBussProperty> RoundCorners =
			new PropertyBinding<IFloatFromFloatBussProperty>("roundCorners", new FloatBussProperty());

		public static readonly PropertyBinding<ISpriteBussProperty> BackgroundImage =
			new PropertyBinding<ISpriteBussProperty>("backgroundImage", new SpriteBussProperty());

		public static readonly PropertyBinding<BackgroundModeBussProperty> BackgroundMode =
			new PropertyBinding<BackgroundModeBussProperty>("backgroundMode", new BackgroundModeBussProperty());

		// Border
		public static readonly PropertyBinding<BorderModeBussProperty> BorderMode =
			new PropertyBinding<BorderModeBussProperty>("borderMode", new BorderModeBussProperty());

		public static readonly PropertyBinding<IFloatBussProperty> BorderWidth =
			new PropertyBinding<IFloatBussProperty>("borderWidth", new FloatBussProperty());

		public static readonly PropertyBinding<IVertexColorBussProperty> BorderColor =
			new PropertyBinding<IVertexColorBussProperty>("borderColor", _defaultVertexColorValues);

		// Transform
		public static readonly PropertyBinding<IRectTransformBussProperty> RectTransform
			= new PropertyBinding<IRectTransformBussProperty>("rect", new RectTransformProperty());
		
		// Shadow
		public static readonly PropertyBinding<IVector2BussProperty> ShadowOffset =
			new PropertyBinding<IVector2BussProperty>("shadowOffset", new Vector2BussProperty());

		public static readonly PropertyBinding<IFloatBussProperty> ShadowThreshold =
			new PropertyBinding<IFloatBussProperty>("shadowThreshold", new FloatBussProperty());

		public static readonly PropertyBinding<IVertexColorBussProperty> ShadowColor =
			new PropertyBinding<IVertexColorBussProperty>("shadowColor", _defaultTransparentVertexColorValues);

		public static readonly PropertyBinding<IFloatBussProperty> ShadowSoftness =
			new PropertyBinding<IFloatBussProperty>("shadowSoftness", new FloatBussProperty());

		public static readonly PropertyBinding<ShadowModeBussProperty> ShadowMode =
			new PropertyBinding<ShadowModeBussProperty>("shadowMode", new ShadowModeBussProperty());

		// Font
		public static readonly PropertyBinding<IFontBussProperty> Font =
			new PropertyBinding<IFontBussProperty>("font", new FontBussAssetProperty(), true);

		public static readonly PropertyBinding<IFloatBussProperty> FontSize =
			new PropertyBinding<IFloatBussProperty>("fontSize", new FloatBussProperty(18f), true);

		public static readonly PropertyBinding<IColorBussProperty> FontColor =
			new PropertyBinding<IColorBussProperty>("fontColor", new SingleColorBussProperty(), true);

		public static readonly PropertyBinding<TextAlignmentOptionsBussProperty> TextAlignment =
			new PropertyBinding<TextAlignmentOptionsBussProperty>("textAlignment",
																  new TextAlignmentOptionsBussProperty(
																	  TextAlignmentOptions.TopLeft), true);

		// Transitions
		// TODO: Disabled with BEAM-3130 due to incomplete implementation
		// public static readonly PropertyBinding<IFloatBussProperty> TransitionDuration =
		// 	new PropertyBinding<IFloatBussProperty>("transitionDuration", new FloatBussProperty(0f));

		// TODO: Disabled with BEAM-3130 due to incomplete implementation
		// public static readonly PropertyBinding<EasingBussProperty> TransitionEasing =
		// 	new PropertyBinding<EasingBussProperty>("transitionEasing", new EasingBussProperty(Easing.InOutQuad));

		#endregion

		public interface IPropertyBinding
		{
			string Key
			{
				get;
			}

			Type PropertyType
			{
				get;
			}
			// Type[] PropertyTypes { get; }

			/// <summary>
			/// When true, an element can inherit this property from the element's parent.
			/// </summary>
			bool Inheritable { get; }
			
			IBussProperty GetProperty(BussStyle style);
			void SetProperty(BussStyle style, IBussProperty property);
			IBussProperty GetDefaultValue(Type specificType);
			Type[] GetTypesOfDefaultValues();
		}

		public sealed class PropertyBinding<T> : IPropertyBinding where T : class, IBussProperty
		{
			public string Key
			{
				get;
			}

			public T DefaultValue => DefaultValues[0];

			public T[] DefaultValues
			{
				get;
			}
			public Type[] PropertyTypes { get; }
			
			public Type[] DefaultValueTypes { get; }

			public bool Inheritable { get; }

			public Type PropertyType => typeof(T);

			private static HashSet<string> _keyControler = new HashSet<string>();

			internal PropertyBinding(string key, T defaultValue, bool inheritable = false)
			{
				Key = key;
				DefaultValues = new T[]{defaultValue};
				DefaultValueTypes = new Type[] {defaultValue.GetType()};

				if (DefaultValueTypes.Contains(typeof(T)))
				{
					PropertyTypes = DefaultValueTypes;
				}
				else
				{
					PropertyTypes = new Type[DefaultValueTypes.Length + 1];
					for (var i = 0; i < DefaultValueTypes.Length; i++)
					{
						PropertyTypes[i] = DefaultValueTypes[0];
					}

					PropertyTypes[DefaultValueTypes.Length] = typeof(T);
				}
				_bindings[key] = this;
				Inheritable = inheritable;
			}
			
			internal PropertyBinding(string key, T[] defaultValues, bool inheritable = false)
			{
				Key = key;
				DefaultValues = defaultValues.ToArray();
				DefaultValueTypes = new Type[defaultValues.Length];
				for (var i = 0; i < defaultValues.Length; i++)
				{
					DefaultValueTypes[i] = defaultValues[i].GetType();
				}
				_bindings[key] = this;
				Inheritable = inheritable;
			}


			IBussProperty IPropertyBinding.GetProperty(BussStyle style) => Get(style);

			void IPropertyBinding.SetProperty(BussStyle style, IBussProperty property)
			{
				if (property is T t)
				{
					Set(style, t);
				}
				else if (property is VariableProperty v)
				{
					Set(style, v);
				}
			}

			public IBussProperty GetDefaultValue(Type specificType=null)
			{
				if (specificType == null)
				{
					return DefaultValues[0];
				}

				foreach (var value in DefaultValues)
				{
					if (value.GetType().IsAssignableFrom(specificType))
					{
						return value;
					}
				}

				throw new InvalidOperationException(
					$"Cannot find a default value for {this.Key} with type {specificType.Name}");
			}

			public Type[] GetTypesOfDefaultValues()
			{
				return DefaultValueTypes;
			}

			public T Get(BussStyle style)
			{
				if (style is BussPseudoStyle pseudoStyle)
				{
					return GetFromPseudoStyle(pseudoStyle) ?? DefaultValue;
				}

				return GetFromStyle(style) ?? DefaultValue;
			}

			public T GetFromStyle(BussStyle style, bool useDefaultIfNotFound=true)
			{
				if (!style.TryGetValue<T>(Key, out var styleProperty))
				{
					styleProperty = useDefaultIfNotFound ? DefaultValue : null;
				}

				return styleProperty;
			}

			private T GetFromPseudoStyle(BussPseudoStyle style)
			{
				if (style._properties.ContainsKey(Key))
				{
					var pseudoProperty = GetFromStyle(style);
					if (Get(style.BaseStyle) is IInterpolatedBussProperty interpolatedProperty)
					{
						// TODO: cache interpolated properties
						return (T)interpolatedProperty.Interpolate(pseudoProperty, style.BlendValue);
					}

					return pseudoProperty;
				}

				return Get(style.BaseStyle);
			}

			private T GetFromVariable(BussStyle style, string variableName)
			{
				if (!style.TryGetFromVariable<T>(variableName, out var variableProperty))
				{
					variableProperty = DefaultValue;
				}

				return variableProperty;
			}

			public void Set(BussStyle style, T property)
			{
				style._properties[Key] = property.CopyProperty();
			}

			public void Set(BussStyle style, VariableProperty property)
			{
				style._properties[Key] = property.CopyProperty();
			}
		}
	}
}
