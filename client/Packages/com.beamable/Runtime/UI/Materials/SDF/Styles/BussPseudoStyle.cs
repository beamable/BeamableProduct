using System.Collections.Generic;

namespace Beamable.UI.BUSS {
    public class BussPseudoStyle : BUSSStyle {
        public BUSSStyle BaseStyle { get; set; }
        public float BlendValue;
        private Dictionary<string, IBUSSProperty> _interpolatedStyles = new Dictionary<string, IBUSSProperty>();

        public BussPseudoStyle(BUSSStyle baseStyle) {
            BaseStyle = baseStyle;
        }
    }
}