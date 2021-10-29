using System;
using System.Collections.Generic;

namespace Beamable.UI.BUSS
{
    public partial class BUSSStyle {
        
        internal static Dictionary<string, IPropertyBiding> _bidings = new Dictionary<string, IPropertyBiding>();
        
        
        private readonly Dictionary<string, IBUSSProperty> _properties = new Dictionary<string, IBUSSProperty>();
        
        public IBUSSProperty this[string key] {
            get {
                if (_bidings.TryGetValue(key, out var biding)) {
                    return biding.GetProperty(this);
                }

                return null;
            }
            
            set {
                if (_bidings.TryGetValue(key, out var biding)) {
                    biding.SetProperty(this, value);
                }
                else if (key.StartsWith("--")) { // variable that wasn't accessed before
                    _bidings[key] = biding = new PropertyBiding<IBUSSProperty>(key, null);
                    biding.SetProperty(this, value);
                }
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