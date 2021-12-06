using System.Collections.Generic;
using System.Linq;

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

		public static bool IsValidVariableName(string name) => name.StartsWith("--");

		public static IEnumerable<BussPropertyProvider> GetVariablePropertyProviders(this BussStyleDescription target)
		{
			return target.Properties.Where(p => IsValidVariableName(p.Key));
		}
	}
}
