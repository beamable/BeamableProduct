using System;
using Beamable.UI.SDF;
using Beamable.UI.SDF.MaterialManagement;
using UnityEngine.UI;

namespace Beamable.UI.BUSS {
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

        public abstract IBUSSProperty Clone();
    }
    
    [Serializable]
    public class ImageTypeProperty : EnumProperty<Image.Type> {
        public ImageTypeProperty() { }
        public ImageTypeProperty(Image.Type @enum) : base(@enum) { }

        public override IBUSSProperty Clone() {
            return new ImageTypeProperty(Enum);
        }
    }

    // TODO: can't we move BorderMode class into Beamable.UI.BUSS namespace?
    [Serializable]
    public class SdfModeProperty : EnumProperty<SDFImage.SdfMode> {
        public SdfModeProperty() { }
        public SdfModeProperty(SDFImage.SdfMode @enum) : base(@enum) { }

        public override IBUSSProperty Clone() {
            return new SdfModeProperty(Enum);
        }
    }

    [Serializable]
    public class BorderModeProperty : EnumProperty<SDFImage.BorderMode> {
        public BorderModeProperty() { }
        public BorderModeProperty(SDFImage.BorderMode @enum) : base(@enum) { }

        public override IBUSSProperty Clone() {
            return new BorderModeProperty(Enum);
        }
    }

    [Serializable]
    public class BackgroundModeProperty : EnumProperty<SdfBackgroundMode> {
        public BackgroundModeProperty(){}
        public BackgroundModeProperty(SdfBackgroundMode @enum) : base(@enum) { }

        public override IBUSSProperty Clone() {
            return new BackgroundModeProperty(Enum);
        }
    }

    [Serializable]
    public class ShadowModeProperty : EnumProperty<SdfShadowMode> {
        public ShadowModeProperty(){}
        public ShadowModeProperty(SdfShadowMode @enum) : base(@enum) { }

        public override IBUSSProperty Clone() {
            return new ShadowModeProperty(Enum);
        }
    }
}
