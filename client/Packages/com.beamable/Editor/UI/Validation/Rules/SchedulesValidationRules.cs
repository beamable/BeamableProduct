using System.Text.RegularExpressions;

namespace Editor.UI.Validation
{
    public class SchedulesDatesRule : ValidationRule<string>
    {
        public override string ErrorMessage =>
            $"{ComponentName} field doesn't match pattern. Dates should have format DD-MM-YYYY separated with semicolon";

        public SchedulesDatesRule(string componentName) : base(componentName)
        {
        }

        public override void Validate(string value)
        {
            string[] values = value.Split(';');

            Satisfied = true;

            foreach (string s in values)
            {
                if (!Regex.IsMatch(s, "([0-9]{2}-[0-9]{2}-[0-9]{4})"))
                {
                    Satisfied = false;
                }
            }
        }
    }
}