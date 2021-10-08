using System.Text.RegularExpressions;

namespace Editor.UI.Validation
{
    public class IsNotEmptyRule : ValidationRule<string>
    {
        public override string ErrorMessage => $"{ComponentName} field can't be empty";

        public IsNotEmptyRule(string componentLabel) : base(componentLabel)
        {
        }

        public override void Validate(string value)
        {
            Satisfied = !string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value);
        }
    }

    public class PatternMatchRule : ValidationRule<string>
    {
        private readonly string _pattern;

        public override string ErrorMessage => $"{ComponentName} field doesn't mach pattern";

        public PatternMatchRule(string pattern, string componentLabel) : base(componentLabel)
        {
            _pattern = pattern;
        }

        public override void Validate(string value)
        {
            Satisfied = Regex.IsMatch(value, _pattern);
        }
    }

    public class AtLeastOneOptionSelectedRule : ValidationRule<int>
    {
        public AtLeastOneOptionSelectedRule(string componentName) : base(componentName)
        {
        }

        public override string ErrorMessage => $"{ComponentName} must have minimum one option selected";
        
        public override void Validate(int value)
        {
            Satisfied = value > 0;
        }
    }
}