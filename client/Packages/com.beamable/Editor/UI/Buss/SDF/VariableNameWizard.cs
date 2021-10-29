using System;
using UnityEditor;

namespace Beamable.Editor.UI.SDF {
    public class VariableNameWizard : ScriptableWizard {
        public string variableName;
        private Action<string> onVariableCreated;

        public static void ShowWizard(Action<string> onVariableCreated) {
            var wizard = CreateInstance<VariableNameWizard>();
            wizard.onVariableCreated += onVariableCreated;
            wizard.ShowModalUtility();
        }

        private void OnWizardCreate() {
            if (string.IsNullOrWhiteSpace(variableName)) return;
            onVariableCreated?.Invoke("--" + variableName);
        }
    }
}