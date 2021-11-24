using System;
using System.Collections.Generic;

namespace Beamable.UI.Buss
{
    public partial class BussStyle {
        
        public IBussProperty this[string key] {
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

        public static IEnumerable<string> Keys => _bindings.Keys;

        public static Type GetBaseType(string key) {
            if (_bindings.TryGetValue(key, out var binding)) {
                return binding.PropertyType;
            }
            return typeof(IBussProperty);
        }

        public static IBussProperty GetDefaultValue(string key) {
            if (_bindings.TryGetValue(key, out var binding)) {
                return binding.GetDefaultValue();
            }
            return null;
        }

        public void Clear() {
            _properties.Clear();
        }
    }
}