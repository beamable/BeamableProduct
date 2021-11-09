using System;
using System.Collections.Generic;

namespace Beamable.UI.BUSS
{
    public partial class BUSSStyle {
        
        internal static Dictionary<string, IPropertyBiding> _bidings = new Dictionary<string, IPropertyBiding>();
        
        
        protected readonly Dictionary<string, IBUSSProperty> _properties = new Dictionary<string, IBUSSProperty>();
        
        public IBUSSProperty this[string key] {
            get {
                if (_properties.TryGetValue(key, out var property)) {
                    return property;
                }
                return null;
            }
            
            set {
                _properties[key] = value;
            }
        }

        public static IEnumerable<string> Keys => _bidings.Keys;

        public static Type GetBaseType(string key) {
            if (_bidings.TryGetValue(key, out var biding)) {
                return biding.PropertyType;
            }
            return typeof(IBUSSProperty);
        }

        public static IBUSSProperty GetDefaultValue(string key) {
            if (_bidings.TryGetValue(key, out var biding)) {
                return biding.GetDefaultValue();
            }
            return null;
        }

        public void Clear() {
            _properties.Clear();
        }
    }
}