using System;
using UnityEngine.UI;

namespace Beamable.UI.SDF.Styles {
    public abstract class EnumProperty<T> : IEnumProperty where T : Enum {
        public T Enum;

        public Enum EnumValue => Enum;

        protected EnumProperty() { }

        protected EnumProperty(T @enum) {
            Enum = @enum;
        }

        public T1 CastEnumValue<T1>() where T1 : Enum {
            return (T1) EnumValue;
        }

        public abstract ISDFProperty Clone();
    }
    
    [Serializable]
    public class ImageTypeProperty : EnumProperty<Image.Type> {
        public ImageTypeProperty() { }
        public ImageTypeProperty(Image.Type @enum) : base(@enum) { }

        public override ISDFProperty Clone() {
            return new ImageTypeProperty(Enum);
        }
    }
}