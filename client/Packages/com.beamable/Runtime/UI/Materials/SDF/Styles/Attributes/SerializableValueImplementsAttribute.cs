using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.UI.SDF {
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializableValueImplementsAttribute : PropertyAttribute {
        public readonly Type baseType;
        public readonly Type[] subTypes;
        public readonly GUIContent[] labels;
        
        public SerializableValueImplementsAttribute(Type baseType) {
            this.baseType = baseType;
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t =>
                t.IsSerializable && t.IsClass && baseType.IsAssignableFrom(t)).ToList();
            types.Insert(0, null);
            subTypes = types.ToArray();
            labels = subTypes.Select(t => new GUIContent(t == null ? "Null" : t.Name)).ToArray();
        }
    }
}