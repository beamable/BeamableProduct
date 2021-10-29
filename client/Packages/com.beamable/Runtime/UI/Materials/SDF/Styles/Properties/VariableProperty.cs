using System;
using Beamable.UI.SDF.Styles;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.UI.BUSS {
    [Serializable]
    public class VariableProperty : IBUSSProperty {
        [SerializeField]
        private string _variableName;
        public string VariableName {
            get => _variableName;
            set => _variableName = value;
        }
        
        public VariableProperty(){}

        public VariableProperty(string variableName) {
            VariableName = variableName;
        }

        public IBUSSProperty Clone() {
            return new VariableProperty(VariableName);
        }
    }
}