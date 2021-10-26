using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.UI.SDF {
    public static class SerializableValueImplementationHelper {

        private static Dictionary<Type, ImplementationData> _data = new Dictionary<Type, ImplementationData>();

        public static ImplementationData Get(Type baseType) {
            if (!_data.TryGetValue(baseType, out var data)) {
                data = new ImplementationData(baseType);
                _data[baseType] = data;
            }
            return data;
        }
        
        public class ImplementationData {
            public readonly Type baseType;
            public readonly Type[] subTypes;
            public readonly GUIContent[] labels;
        
            internal ImplementationData(Type baseType) {
                this.baseType = baseType;
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t =>
                    t.IsSerializable && t.IsClass && baseType.IsAssignableFrom(t)).ToList();
                types.Insert(0, null);
                subTypes = types.ToArray();
                labels = subTypes.Select(t => new GUIContent(t == null ? "Null" : t.Name)).ToArray();
            }
        }
    }
}