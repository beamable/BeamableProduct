using Beamable.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beamable.UI.Buss
{
	public static class BussStyleSheetUtility
	{
		public static bool TryAddProperty(this BussStyleDescription target,
		                                  string key,
		                                  IBussProperty property,
		                                  out BussPropertyProvider propertyProvider)
		{
			var isKeyValid = BussStyle.IsKeyValid(key) || IsValidVariableName(key);
			if (isKeyValid && !target.HasProperty(key) &&
			    BussStyle.GetBaseType(key).IsAssignableFrom(property.GetType()))
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

		public static bool IsValidVariableName(string name) => name?.StartsWith("--") ?? false;

		public static IEnumerable<BussPropertyProvider> GetVariablePropertyProviders(this BussStyleDescription target)
		{
			return target.Properties.Where(p => IsValidVariableName(p.Key));
		}

		public static void AssignAssetReferencesFromReferenceList(this BussStyleDescription style,
		                                                          List<Object> assetReferences)
		{
			foreach (BussPropertyProvider propertyProvider in style.Properties)
			{
				var property = propertyProvider.GetProperty();
				if (property is BaseAssetProperty assetProperty)
				{
					if (assetProperty.AssetSerializationKey >= 0 &&
					    assetProperty.AssetSerializationKey < assetReferences.Count)
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

		public static void PutAssetReferencesInReferenceList(this BussStyleDescription style,
		                                                     List<Object> assetReferences)
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

#if UNITY_EDITOR
		public static void ValidateStyleSheets(GameObject gameObject)
		{
			List<BussElement> bussElements = gameObject.GetComponentsInChildren<BussElement>(true).ToList();

			foreach (BussElement bussElement in bussElements)
			{
				if (bussElement == null)
				{
					continue;
				}

				if (!Directory.Exists(Constants.Features.Buss.STYLE_SHEETS_PATH))
				{
					Directory.CreateDirectory(Constants.Features.Buss.STYLE_SHEETS_PATH);
				}

				if (BussConfiguration.OptionalInstance.Value.GlobalStyleSheet == null)
				{
					BussStyleSheet globalStyleSheet = CreateGlobalStyleSheet();

					if (globalStyleSheet == null)
					{
						BeamableLogger.LogError("Problem while trying to create global style sheet. Terminating...");
						return;
					}
					
					BussConfiguration.OptionalInstance.Value.SetGlobalStyleSheet(globalStyleSheet);
				}

				BussStyleSheet defaultGlobalStyleSheet = AssetDatabase.LoadAssetAtPath<BussStyleSheet>(
					Constants.Features.Buss.DEFAULT_GLOBAL_STYLE_SHEET_PATH);
				
				CopySingleStyle(defaultGlobalStyleSheet, BussConfiguration.OptionalInstance.Value.GlobalStyleSheet, bussElement.Id);
			
				foreach (string classSelector in bussElement.Classes)
				{
					CopySingleStyle(defaultGlobalStyleSheet, BussConfiguration.OptionalInstance.Value.GlobalStyleSheet, classSelector);
				}
			}
		}

		public static void CopyStyles(BussStyleSheet sourceStyleSheet, BussStyleSheet targetStyleSheet)
		{
			foreach (BussStyleRule bussStyleRule in sourceStyleSheet.Styles)
			{
				CopySingleStyle(sourceStyleSheet, targetStyleSheet, bussStyleRule.SelectorString);
			}
		}

		public static void CopySingleStyle(BussStyleSheet sourceStyleSheet,
		                                   BussStyleSheet targetStyleSheet,
		                                   string styleId)
		{
			BussStyleRule sourceStyle = sourceStyleSheet.Styles.Find(style => style.SelectorString == styleId);

			if (sourceStyle == null)
			{
				BeamableLogger.Log($"Source style sheet doesn't contain style with id {styleId}");
				return;
			}

			BussStyleRule targetStyle = targetStyleSheet.Styles.Find(style => style.SelectorString == styleId);

			if (targetStyle != null)
			{
				BeamableLogger.Log($"Target style sheet already contains style with id {styleId}. Skipping...");
				return;
			}

			BussStyleRule rule = BussStyleRule.Create(sourceStyle.SelectorString, sourceStyle.Properties);
			targetStyleSheet.Styles.Add(rule);
		}

		public static BussStyleSheet CreateGlobalStyleSheet()
		{
			BussStyleSheet globalStyleSheet;
			
			try
			{
				AssetDatabase.StartAssetEditing();
				string globalStyleSheetPath = $"{Constants.Features.Buss.BEAMABLE_GLOBAL_STYLE_SHEET_PATH}";
				globalStyleSheet = ScriptableObject.CreateInstance<BussStyleSheet>();
				AssetDatabase.CreateAsset(globalStyleSheet, globalStyleSheetPath);
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				AssetDatabase.SaveAssets();
			}

			return globalStyleSheet;
		}
#endif
	}
}
