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
    public class SpriteBussProperty : BaseAssetProperty<Sprite>, ISpriteBussProperty {
        public Sprite SpriteValue => Asset;
        
        public SpriteBussProperty() { }

        public SpriteBussProperty(Sprite sprite) {
            Asset = sprite;
        }

        public IBussProperty CopyProperty() {
            return new SpriteBussProperty(Asset);
        }
    }

    [Serializable]
    public class FontBussAssetProperty : BaseAssetProperty<TMP_FontAsset>, IFontBussProperty {
        public TMP_FontAsset FontAsset => Asset;

        public FontBussAssetProperty() { }

        public FontBussAssetProperty(TMP_FontAsset asset) {
            Asset = asset;
        }

        public IBussProperty CopyProperty() {
            return new FontBussAssetProperty(Asset);
        }
    }
}