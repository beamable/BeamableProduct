using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.UI.Buss {
    public abstract class BaseAssetProperty<T> {
        
        [SerializeField]
        protected T _asset;

        public T Asset {
            get => _asset;
            set => _asset = value;
        }
    }

    [Serializable]
    public class SpriteProperty : BaseAssetProperty<Sprite>, ISpriteProperty {
        public Sprite SpriteValue => Asset;
        
        public SpriteProperty() { }

        public SpriteProperty(Sprite sprite) {
            Asset = sprite;
        }

        public IBussProperty Clone() {
            return new SpriteProperty(Asset);
        }
    }

    [Serializable]
    public class FontAssetProperty : BaseAssetProperty<TMP_FontAsset>, IFontProperty {
        public TMP_FontAsset FontAsset => Asset;

        public FontAssetProperty() { }

        public FontAssetProperty(TMP_FontAsset asset) {
            Asset = asset;
        }

        public IBussProperty Clone() {
            return new FontAssetProperty(Asset);
        }
    }
}