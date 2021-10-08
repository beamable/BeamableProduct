namespace Editor.UI.Validation
{
    public class SchedulesDatesRule : PatternMatchRule
    {
        // TODO: add proper pattern for DD-MM-YYYY;...
        public SchedulesDatesRule(string componentLabel) : base("", componentLabel)
        {
        }
    }
}