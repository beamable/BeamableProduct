namespace Editor.UI.Verification
{
    public abstract class VerificationRule<T>
    {
        public abstract string ErrorMessage { get; }
        public abstract bool Check(T value);
    }
    
    public class IsNotEmptyRule : VerificationRule<string>
    {
        public override string ErrorMessage => "field can't be empty";

        public override bool Check(string value)
        {
            return !string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value);
        }
    }

    // public class PatternMatchRule : IVerificationRule
    // {
    //     private readonly string _pattern;
    //
    //     public PatternMatchRule(string pattern)
    //     {
    //         _pattern = pattern;
    //     }
    //     
    //     public override bool Check(string value)
    //     {
    //         return Regex.IsMatch(value, _pattern);
    //     }
    // }
}