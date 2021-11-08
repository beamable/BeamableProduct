namespace CronExpressionDescriptor
{
    /// <summary>
    ///     Options for parsing and describing a Cron Expression
    /// </summary>
    public class Options
    {
        public Options()
        {
            ThrowExceptionOnParseError = true;
            Verbose = false;
            DayOfWeekStartIndexZero = true;
        }

        public bool ThrowExceptionOnParseError { get; }
        public bool Verbose { get; }
        public bool DayOfWeekStartIndexZero { get; }
        public bool? Use24HourTimeFormat { get; set; }
        public string Locale { get; set; }
    }
}