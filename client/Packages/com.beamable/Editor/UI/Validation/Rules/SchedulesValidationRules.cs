using System;
using System.Text.RegularExpressions;

namespace Beamable.Editor.UI.Validation
{
    public class IsProperHour : ValidationRule<string>
    {
        public IsProperHour(string componentName) : base(componentName)
        {
        }

        public override string ErrorMessage => $"{ComponentName} doesn't match hour. Check values";
        
        public override void Validate(string value)
        {
            string[] split = value.Split(':');
            string pattern = "[0-9]";

            Satisfied = Regex.IsMatch(split[0], pattern) && Regex.IsMatch(split[1], pattern) &&
                        Regex.IsMatch(split[2], pattern);
        }
    }

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
    
    public class HoursValidationRule
    {
        private readonly string _bComponentName;
        public string ErrorMessage => $"{BComponentName} should be higher than {AComponentName}";
        public string AComponentName { get; set; }
        public string BComponentName { get; set; }

        public HoursValidationRule(string aComponentName, string bComponentName)
        {
            _bComponentName = bComponentName;
            AComponentName = aComponentName;
        }

        public void Validate(string aValue, string bValue)
        {
            DateTime aTime = ParseHourString(aValue, out bool aParsed);
            DateTime bTime = ParseHourString(bValue, out bool bParsed);

            Satisfied = aParsed && bParsed && aTime.CompareTo(bTime) < 0;
        }

        public bool Satisfied { get; set; }

        private DateTime ParseHourString(string value, out bool success)
        {
            string[] split = value.Split(':');

            if (!int.TryParse(split[0], out int hour) || 
                !int.TryParse(split[1], out int minute) ||
                !int.TryParse(split[2], out int second))
            {
                success = false;
                return new DateTime(2000, 1, 1, 0, 0, 0);
            }
            
            success = true;
            return new DateTime(2000, 1, 1, hour , minute, second);
        }
    }
}