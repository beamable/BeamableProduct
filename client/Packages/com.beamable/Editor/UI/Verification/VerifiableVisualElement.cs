using System;
using Beamable.Editor.UI.Buss;

namespace Editor.UI.Verification
{
    public abstract class VerifiableVisualElement<T> : BeamableVisualElement
    {
        private VerificationRule<T> _rule;
        public Action<bool, string> PostVerification;

        protected VerifiableVisualElement(string commonPath) : base(commonPath)
        {
        }

        protected VerifiableVisualElement(string uxmlPath, string ussPath) : base(uxmlPath, ussPath)
        {
        }

        public VerificationRule<T> RegisterRule(VerificationRule<T> rule, Action<bool,string> onPostVerification)
        {
            _rule = rule;
            PostVerification = onPostVerification;
            return rule;
        }

        protected void InvokeVerificationCheck(T value)
        {
            bool satisfied = false;
            satisfied = _rule != null && _rule.Check(value);
            PostVerification?.Invoke(satisfied, "Error message");
        }
    }
}