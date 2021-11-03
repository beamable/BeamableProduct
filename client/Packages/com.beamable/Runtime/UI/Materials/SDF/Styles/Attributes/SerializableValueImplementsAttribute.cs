using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.UI.SDF {
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializableValueImplementsAttribute : PropertyAttribute {
        public readonly Type baseType;
        
        public SerializableValueImplementsAttribute(Type baseType) {
            this.baseType = baseType;
        }
    }
}