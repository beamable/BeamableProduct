using Beamable.Common;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.UI.Buss
{
	public static class BussStyleSheetUtility
	{
		public static bool IsValidVariableName(string name) => name?.StartsWith("--") ?? false;

		public static bool TryAddProperty(this BussStyleDescription target,
		                                  string key,
		                                  IBussProperty property,
		                                  out BussPropertyProvider propertyProvider)
		{
			var isKeyValid = BussStyle.IsKeyValid(key) || IsValidVariableName(key);
			if (isKeyValid && !target.HasProperty(key) &&
			    BussStyle.GetBaseType(key).IsInstanceOfType(property))
			{
				propertyProvider = BussPropertyProvider.Create(key, property.CopyProperty());
				target.Properties.Add(propertyProvider);
				return true;
			}

			propertyProvider = null;
			return false;
		}

		public static bool HasProperty(this BussStyleDescription target, string key)
		{
			return target.Properties.Any(p => p.Key == key);
		}

		public static BussPropertyProvider GetPropertyProvider(this BussStyleDescription target, string key)
		{
			return target.Properties.FirstOrDefault(p => p.Key == key);
		}

		public static IBussProperty GetProperty(this BussStyleDescription target, string key)
		{
			return target.GetPropertyProvider(key)?.GetProperty();
		}

		public static IEnumerable<BussPropertyProvider> GetVariablePropertyProviders(this BussStyleDescription target)
		{
			return target.Properties.Where(p => IsValidVariableName(p.Key));
		}

		public static void AssignAssetReferencesFromReferenceList(this BussStyleDescription style, List<Object> assetReferences)
		{
			foreach (BussPropertyProvider propertyProvider in style.Properties)
			{
				var property = propertyProvider.GetProperty();
				if (property is BaseAssetProperty assetProperty)
				{
					if (assetProperty.AssetSerializationKey >= 0 && assetProperty.AssetSerializationKey < assetReferences.Count)
					{
						assetProperty.GenericAsset = assetReferences[assetProperty.AssetSerializationKey];
					}
					else
					{
						assetProperty.GenericAsset = null;
						assetProperty.AssetSerializationKey = -1;
					}
				}
			}
		}

		public static void PutAssetReferencesInReferenceList(this BussStyleDescription style, List<Object> assetReferences)
		{
			if (style == null || style.Properties == null)
			{
				return;
			}
			foreach (BussPropertyProvider propertyProvider in style.Properties)
			{
				var property = propertyProvider.GetProperty();
				if (property is BaseAssetProperty assetProperty)
				{
					var asset = assetProperty.GenericAsset;
					if (asset != null)
					{
						var index = assetReferences.IndexOf(asset);
						if (index == -1)
						{
							index = assetReferences.Count;
							assetReferences.Add(asset);
						}

						assetProperty.AssetSerializationKey = index;
					}
					else
					{
						assetProperty.AssetSerializationKey = -1;
					}
				}
			}
		}

		public static void CopySingleStyle(BussStyleSheet targetStyleSheet, BussStyleRule style)
		{
			if (style == null)
			{
				BeamableLogger.LogWarning($"Style to copy can't be null");
				return;
			}

			BussStyleRule rule = BussStyleRule.Create(style.SelectorString, style.Properties);
			targetStyleSheet.Styles.Add(rule);
			AssetDatabase.SaveAssets();
			targetStyleSheet.TriggerChange();
		}

		public static void RemoveSingleStyle(BussStyleSheet targetStyleSheet, BussStyleRule style)
		{
			targetStyleSheet.RemoveStyle(style);
			AssetDatabase.SaveAssets();
			targetStyleSheet.TriggerChange();
		}
	}
}
