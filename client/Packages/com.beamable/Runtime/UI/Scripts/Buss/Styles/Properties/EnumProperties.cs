using System;
using Beamable.UI.Sdf;
using Beamable.UI.Sdf.MaterialManagement;
using Beamable.UI.Tweening;
using UnityEngine.UI;

namespace Beamable.UI.Buss {
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

        public abstract IBussProperty Clone();
    }
    
    [Serializable]
    public class ImageTypeProperty : EnumProperty<SdfImage.ImageType> {
        public ImageTypeProperty() { }
        public ImageTypeProperty(SdfImage.ImageType @enum) : base(@enum) { }

        public override IBussProperty Clone() {
            return new ImageTypeProperty(Enum);
        }
    }

    // TODO: can't we move BorderMode class into Beamable.UI.BUSS namespace?
    [Serializable]
    public class SdfModeProperty : EnumProperty<SdfImage.SdfMode> {
        public SdfModeProperty() { }
        public SdfModeProperty(SdfImage.SdfMode @enum) : base(@enum) { }

        public override IBussProperty Clone() {
            return new SdfModeProperty(Enum);
        }
    }

    [Serializable]
    public class BorderModeProperty : EnumProperty<SdfImage.BorderMode> {
        public BorderModeProperty() { }
        public BorderModeProperty(SdfImage.BorderMode @enum) : base(@enum) { }

        public override IBussProperty Clone() {
            return new BorderModeProperty(Enum);
        }
    }

    [Serializable]
    public class BackgroundModeProperty : EnumProperty<SdfBackgroundMode> {
        public BackgroundModeProperty(){}
        public BackgroundModeProperty(SdfBackgroundMode @enum) : base(@enum) { }

        public override IBussProperty Clone() {
            return new BackgroundModeProperty(Enum);
        }
    }

    [Serializable]
    public class ShadowModeProperty : EnumProperty<SdfShadowMode> {
        public ShadowModeProperty(){}
        public ShadowModeProperty(SdfShadowMode @enum) : base(@enum) { }

        public override IBussProperty Clone() {
            return new ShadowModeProperty(Enum);
        }
    }

    [Serializable]
    public class EasingProperty : EnumProperty<Easing> {
        public EasingProperty(){}

        public EasingProperty(Easing easing) : base(easing) { }

        public override IBussProperty Clone() {
            return new EasingProperty(Enum);
        }
    }
}
