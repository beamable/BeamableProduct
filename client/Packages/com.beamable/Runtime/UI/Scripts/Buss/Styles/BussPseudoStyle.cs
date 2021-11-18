using System.Collections.Generic;

namespace Beamable.UI.Buss {
    public class BussPseudoStyle : BussStyle {
        public BussStyle BaseStyle { get; set; }
        public float BlendValue;
        private Dictionary<string, IBussProperty> _interpolatedStyles = new Dictionary<string, IBussProperty>();

        public BussPseudoStyle(BussStyle baseStyle) {
            BaseStyle = baseStyle;
        }
    }
}