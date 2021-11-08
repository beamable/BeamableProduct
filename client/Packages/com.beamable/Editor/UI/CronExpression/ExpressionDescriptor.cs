using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Beamable.CronExpression
{
    public static class Extensions
    {
        private static readonly Dictionary<CronLocale, string> _cronLocaleToLocale = new Dictionary<CronLocale, string>
        {
            { CronLocale.en_US, "en-US" },
            { CronLocale.pl_PL, "pl-PL" }
        };

        public static string ConvertCronLocaleToLocale(this CronLocale? cronLocale)
        {
            return !cronLocale.HasValue ? string.Empty : ConvertCronLocaleToLocale(cronLocale.Value);
        }
        public static string ConvertCronLocaleToLocale(this CronLocale cronLocale)
        {
            return _cronLocaleToLocale.ContainsKey(cronLocale) ? _cronLocaleToLocale[cronLocale] : string.Empty;
        }
    }
    
    /// <summary>
    ///     Converts a Cron Expression into a human readable string
    /// </summary>
    public class ExpressionDescriptor
    {
        private readonly char[] _specialCharacters = { '/', '-', ',', '*' };

        private readonly string[] _24hourTimeFormatTwoLetterISOLanguageName =
        {
            "ru", "uk", "de", "it", "tr", "pl", "ro", "da", "sl", "fi", "sv"
        };
        
        private readonly string _expression;
        private readonly Options _options;
        private string[] _expressionParts;
        private bool _parsed;
        private readonly bool _use24HourTimeFormat;
        private readonly CultureInfo _culture;
        private CronLocalizationData _localizationData;
        private CronLocalizationDatabase _localizationDatabase;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionDescriptor" /> class
        /// </summary>
        /// <param name="expression">The cron expression string</param>
        public ExpressionDescriptor(string expression) : this(expression, new Options())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionDescriptor" /> class
        /// </summary>
        /// <param name="expression">The cron expression string</param>
        /// <param name="options">Options to control the output description</param>
        public ExpressionDescriptor(string expression, Options options)
        {
            _expression = expression;
            _options = options;
            _expressionParts = new string[7];
            _parsed = false;
            
            _localizationDatabase = AssetDatabase.LoadAssetAtPath<CronLocalizationDatabase>($"Packages/com.beamable/Editor/UI/CronExpression/Resources/CronLocalizationDatabase.asset");
            if (_localizationDatabase == null)
            {
                _localizationDatabase = ScriptableObject.CreateInstance<CronLocalizationDatabase>();
                AssetDatabase.CreateAsset(_localizationDatabase, "Packages/com.beamable/Editor/UI/CronExpression/Resources/CronLocalizationDatabase.asset");
            }

            _options.Locale ??= _localizationDatabase.DefaultLocalization;

            _localizationData = _localizationDatabase.SupportedLocalizations.FirstOrDefault(x => x.Localization == _options.Locale.ConvertCronLocaleToLocale());
            if (_localizationData == null)
                _localizationData = ScriptableObject.CreateInstance<CronLocalizationData>();

            _culture = new CultureInfo(_localizationData.Localization);
            _use24HourTimeFormat = _options.Use24HourTimeFormat ?? _24hourTimeFormatTwoLetterISOLanguageName.Contains(_culture.TwoLetterISOLanguageName);
        }

        /// <summary>
        ///     Generates a human readable string for the Cron Expression
        /// </summary>
        /// <param name="type">Which part(s) of the expression to describe</param>
        /// <returns>The cron expression description</returns>
        public string GetDescription(DescriptionTypeEnum type)
        {
            var description = string.Empty;

            try
            {
                if (!_parsed)
                {
                    var parser = new ExpressionParser(_expression, _options);
                    _expressionParts = parser.Parse();
                    _parsed = true;
                }

                description = type switch
                {
                    DescriptionTypeEnum.FULL => GetFullDescription(),
                    DescriptionTypeEnum.TIMEOFDAY => GetTimeOfDayDescription(),
                    DescriptionTypeEnum.HOURS => GetHoursDescription(),
                    DescriptionTypeEnum.MINUTES => GetMinutesDescription(),
                    DescriptionTypeEnum.SECONDS => GetSecondsDescription(),
                    DescriptionTypeEnum.DAYOFMONTH => GetDayOfMonthDescription(),
                    DescriptionTypeEnum.MONTH => GetMonthDescription(),
                    DescriptionTypeEnum.DAYOFWEEK => GetDayOfWeekDescription(),
                    DescriptionTypeEnum.YEAR => GetYearDescription(),
                    _ => GetSecondsDescription()
                };
            }
            catch (Exception ex)
            {
                if (!_options.ThrowExceptionOnParseError)
                    description = ex.Message;
                else
                    throw;
            }

            // Uppercase the first letter
            description = string.Concat(_culture.TextInfo.ToUpper(description[0]), description.Substring(1));

            return description;
        }

        /// <summary>
        ///     Generates the FULL description
        /// </summary>
        /// <returns>The FULL description</returns>
        protected string GetFullDescription()
        {
            string description;

            try
            {
                var timeSegment = GetTimeOfDayDescription();
                var dayOfMonthDesc = GetDayOfMonthDescription();
                var monthDesc = GetMonthDescription();
                var dayOfWeekDesc = GetDayOfWeekDescription();
                var yearDesc = GetYearDescription();

                description = $"{timeSegment}{dayOfMonthDesc}{dayOfWeekDesc}{monthDesc}{yearDesc}";

                description = TransformVerbosity(description, _options.Verbose);
            }
            catch (Exception ex)
            {
                description = GetString("AnErrorOccuredWhenGeneratingTheExpressionD");
                if (_options.ThrowExceptionOnParseError) throw new FormatException(description, ex);
            }

            return description;
        }

        /// <summary>
        ///     Generates a description for only the TIMEOFDAY portion of the expression
        /// </summary>
        /// <returns>The TIMEOFDAY description</returns>
        protected string GetTimeOfDayDescription()
        {
            var secondsExpression = _expressionParts[0];
            var minuteExpression = _expressionParts[1];
            var hourExpression = _expressionParts[2];

            var description = new StringBuilder();

            //handle special cases first
            if (minuteExpression.IndexOfAny(_specialCharacters) == -1 &&
                hourExpression.IndexOfAny(_specialCharacters) == -1 &&
                secondsExpression.IndexOfAny(_specialCharacters) == -1)
            {
                //specific time of day (i.e. 10 14)
                description.Append(GetString("AtSpace"))
                    .Append(FormatTime(hourExpression, minuteExpression, secondsExpression));
            }
            else if (secondsExpression == "" && minuteExpression.Contains("-") && !minuteExpression.Contains(",") &&
                     hourExpression.IndexOfAny(_specialCharacters) == -1)
            {
                //minute range in single hour (i.e. 0-10 11)
                var minuteParts = minuteExpression.Split('-');
                description.Append(string.Format(GetString("EveryMinuteBetweenX0AndX1"),
                    FormatTime(hourExpression, minuteParts[0]), FormatTime(hourExpression, minuteParts[1])));
            }
            else if (secondsExpression == "" && hourExpression.Contains(",") && hourExpression.IndexOf('-') == -1 &&
                     minuteExpression.IndexOfAny(_specialCharacters) == -1)
            {
                //hours list with single minute (o.e. 30 6,14,16)
                var hourParts = hourExpression.Split(',');
                description.Append(GetString("At"));
                for (var i = 0; i < hourParts.Length; i++)
                {
                    description.Append(" ").Append(FormatTime(hourParts[i], minuteExpression));

                    if (i < hourParts.Length - 2) description.Append(",");

                    if (i == hourParts.Length - 2) description.Append(GetString("SpaceAnd"));
                }
            }
            else
            {
                //default time description
                var secondsDescription = GetSecondsDescription();
                var minutesDescription = GetMinutesDescription();
                var hoursDescription = GetHoursDescription();

                description.Append(secondsDescription);

                if (description.Length > 0 && minutesDescription.Length > 0) description.Append(", ");

                description.Append(minutesDescription);

                if (description.Length > 0 && hourExpression.Length > 0) description.Append(", ");

                description.Append(hoursDescription);
            }

            return description.ToString();
        }

        /// <summary>
        ///     Generates a description for only the SECONDS portion of the expression
        /// </summary>
        /// <returns>The SECONDS description</returns>
        protected string GetSecondsDescription()
        {
            var description = GetSegmentDescription(_expressionParts[0], GetString("EverySecond"), s => s,
                s => string.Format(GetString("EveryX0Seconds"), s), s => GetString("SecondsX0ThroughX1PastTheMinute"),
                s =>
                {
                    var i = 0;
                    if (int.TryParse(s, out i))
                        return s == "0" ? string.Empty :
                            i < 20 ? GetString("AtX0SecondsPastTheMinute") :
                            GetString("AtX0SecondsPastTheMinuteGt20") ?? GetString("AtX0SecondsPastTheMinute");
                    return GetString("AtX0SecondsPastTheMinute");
                }, s => GetString("ComaMinX0ThroughMinX1") ?? GetString("ComaX0ThroughX1"));

            return description;
        }

        /// <summary>
        ///     Generates a description for only the MINUTE portion of the expression
        /// </summary>
        /// <returns>The MINUTE description</returns>
        protected string GetMinutesDescription()
        {
            var secondsExpression = _expressionParts[0];
            var description = GetSegmentDescription(_expressionParts[1], GetString("EveryMinute"), s => s,
                s => string.Format(GetString("EveryX0Minutes"), s), s => GetString("MinutesX0ThroughX1PastTheHour"),
                s =>
                {
                    var i = 0;
                    if (int.TryParse(s, out i))
                        return s == "0" && secondsExpression == "" ? string.Empty :
                            int.Parse(s) < 20 ? GetString("AtX0MinutesPastTheHour") :
                            GetString("AtX0MinutesPastTheHourGt20") ?? GetString("AtX0MinutesPastTheHour");
                    return GetString("AtX0MinutesPastTheHour");
                }, s => GetString("ComaMinX0ThroughMinX1") ?? GetString("ComaX0ThroughX1"));

            return description;
        }

        /// <summary>
        ///     Generates a description for only the HOUR portion of the expression
        /// </summary>
        /// <returns>The HOUR description</returns>
        protected string GetHoursDescription()
        {
            var expression = _expressionParts[2];
            var description = GetSegmentDescription(expression, GetString("EveryHour"), s => FormatTime(s, "0"),
                s => string.Format(GetString("EveryX0Hours"), s), s => GetString("BetweenX0AndX1"),
                s => GetString("AtX0"), s => GetString("ComaMinX0ThroughMinX1") ?? GetString("ComaX0ThroughX1"));

            return description;
        }

        /// <summary>
        ///     Generates a description for only the DAYOFWEEK portion of the expression
        /// </summary>
        /// <returns>The DAYOFWEEK description</returns>
        protected string GetDayOfWeekDescription()
        {
            string description = null;

            if (_expressionParts[5] == "*")
                // DOW is specified as * so we will not generate a description and defer to DOM part.
                // Otherwise, we could get a contradiction like "on day 1 of the month, every day"
                // or a dupe description like "every day, every day".
                description = string.Empty;
            else
                description = GetSegmentDescription(_expressionParts[5], GetString("ComaEveryDay"), s =>
                {
                    var exp = s.Contains("#") ? s.Remove(s.IndexOf("#")) :
                        s.Contains("L") ? s.Replace("L", string.Empty) : s;

                    return _culture.DateTimeFormat.GetDayName((DayOfWeek)Convert.ToInt32(exp));
                }, s => string.Format(GetString("ComaEveryX0DaysOfTheWeek"), s), s => GetString("ComaX0ThroughX1"), s =>
                {
                    string format = null;
                    if (s.Contains("#"))
                    {
                        var dayOfWeekOfMonthNumber = s.Substring(s.IndexOf("#") + 1);
                        string dayOfWeekOfMonthDescription = null;
                        switch (dayOfWeekOfMonthNumber)
                        {
                            case "1":
                                dayOfWeekOfMonthDescription = GetString("First");
                                break;
                            case "2":
                                dayOfWeekOfMonthDescription = GetString("Second");
                                break;
                            case "3":
                                dayOfWeekOfMonthDescription = GetString("Third");
                                break;
                            case "4":
                                dayOfWeekOfMonthDescription = GetString("Fourth");
                                break;
                            case "5":
                                dayOfWeekOfMonthDescription = GetString("Fifth");
                                break;
                        }

                        format = string.Concat(GetString("ComaOnThe"), dayOfWeekOfMonthDescription,
                            GetString("SpaceX0OfTheMonth"));
                    }
                    else if (s.Contains("L"))
                    {
                        format = GetString("ComaOnTheLastX0OfTheMonth");
                    }
                    else
                    {
                        format = GetString("ComaOnlyOnX0");
                    }

                    return format;
                }, s => GetString("ComaX0ThroughX1"));

            return description;
        }

        /// <summary>
        ///     Generates a description for only the MONTH portion of the expression
        /// </summary>
        /// <returns>The MONTH description</returns>
        protected string GetMonthDescription()
        {
            var description = GetSegmentDescription(_expressionParts[4], string.Empty,
                s => new DateTime(DateTime.Now.Year, Convert.ToInt32(s), 1).ToString("MMMM", _culture),
                s => string.Format(GetString("ComaEveryX0Months"), s),
                s => GetString("ComaMonthX0ThroughMonthX1") ?? GetString("ComaX0ThroughX1"),
                s => GetString("ComaOnlyInX0"),
                s => GetString("ComaMonthX0ThroughMonthX1") ?? GetString("ComaX0ThroughX1"));

            return description;
        }

        /// <summary>
        ///     Generates a description for only the DAYOFMONTH portion of the expression
        /// </summary>
        /// <returns>The DAYOFMONTH description</returns>
        protected string GetDayOfMonthDescription()
        {
            string description = null;
            var expression = _expressionParts[3];

            switch (expression)
            {
                case "L":
                    description = GetString("ComaOnTheLastDayOfTheMonth");
                    break;
                case "WL":
                case "LW":
                    description = GetString("ComaOnTheLastWeekdayOfTheMonth");
                    break;
                default:
                    var weekDayNumberMatches = new Regex("(\\d{1,2}W)|(W\\d{1,2})");
                    if (weekDayNumberMatches.IsMatch(expression))
                    {
                        var m = weekDayNumberMatches.Match(expression);
                        var dayNumber = int.Parse(m.Value.Replace("W", ""));

                        var dayString = dayNumber == 1
                            ? GetString("FirstWeekday")
                            : string.Format(GetString("WeekdayNearestDayX0"), dayNumber);
                        description = string.Format(GetString("ComaOnTheX0OfTheMonth"), dayString);

                        break;
                    }
                    else
                    {
                        // Handle "last day offset" (i.e. L-5:  "5 days before the last day of the month")
                        var lastDayOffSetMatches = new Regex("L-(\\d{1,2})");
                        if (lastDayOffSetMatches.IsMatch(expression))
                        {
                            var m = lastDayOffSetMatches.Match(expression);
                            var offSetDays = m.Groups[1].Value;
                            description = string.Format(GetString("CommaDaysBeforeTheLastDayOfTheMonth"), offSetDays);
                            break;
                        }

                        description = GetSegmentDescription(expression, GetString("ComaEveryDay"), s => s,
                            s => s == "1" ? GetString("ComaEveryDay") : GetString("ComaEveryX0Days"),
                            s => GetString("ComaBetweenDayX0AndX1OfTheMonth"), s => GetString("ComaOnDayX0OfTheMonth"),
                            s => GetString("ComaX0ThroughX1"));
                        break;
                    }
            }

            return description;
        }

        /// <summary>
        ///     Generates a description for only the YEAR portion of the expression
        /// </summary>
        /// <returns>The YEAR description</returns>
        private string GetYearDescription()
        {
            var description = GetSegmentDescription(_expressionParts[6], string.Empty,
                s => Regex.IsMatch(s, @"^\d+$") ? new DateTime(Convert.ToInt32(s), 1, 1).ToString("yyyy") : s,
                s => string.Format(GetString("ComaEveryX0Years"), s),
                s => GetString("ComaYearX0ThroughYearX1") ?? GetString("ComaX0ThroughX1"),
                s => GetString("ComaOnlyInYearX0"),
                s => GetString("ComaYearX0ThroughYearX1") ?? GetString("ComaX0ThroughX1"));

            return description;
        }

        /// <summary>
        ///     Generates the segment description
        ///     <remarks>
        ///         Range expressions used the 'ComaX0ThroughX1' resource
        ///         However Romanian language has different idioms for
        ///         1. 'from number to number' (minutes, seconds, hours, days) => ComaMinX0ThroughMinX1 optional resource
        ///         2. 'from month to month' ComaMonthX0ThroughMonthX1 optional resource
        ///         3. 'from year to year' => ComaYearX0ThroughYearX1 oprtional resource
        ///         therefore <paramref name="getRangeFormat" /> was introduced
        ///     </remarks>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="allDescription"></param>
        /// <param name="getSingleItemDescription"></param>
        /// <param name="getIntervalDescriptionFormat"></param>
        /// <param name="getBetweenDescriptionFormat"></param>
        /// <param name="getDescriptionFormat"></param>
        /// <param name="getRangeFormat">function that formats range expressions depending on cron parts</param>
        /// <returns></returns>
        protected string GetSegmentDescription(string expression, string allDescription,
            Func<string, string> getSingleItemDescription, Func<string, string> getIntervalDescriptionFormat,
            Func<string, string> getBetweenDescriptionFormat, Func<string, string> getDescriptionFormat,
            Func<string, string> getRangeFormat)
        {
            string description = null;

            if (string.IsNullOrEmpty(expression))
            {
                description = string.Empty;
            }
            else if (expression == "*")
            {
                description = allDescription;
            }
            else if (expression.IndexOfAny(new[] { '/', '-', ',' }) == -1)
            {
                description = string.Format(getDescriptionFormat(expression), getSingleItemDescription(expression));
            }
            else if (expression.Contains("/"))
            {
                var segments = expression.Split('/');
                description = string.Format(getIntervalDescriptionFormat(segments[1]),
                    getSingleItemDescription(segments[1]));

                //interval contains 'between' piece (i.e. 2-59/3 )
                if (segments[0].Contains("-"))
                {
                    var betweenSegmentDescription = GenerateBetweenSegmentDescription(segments[0],
                        getBetweenDescriptionFormat, getSingleItemDescription);

                    if (!betweenSegmentDescription.StartsWith(", ")) description += ", ";

                    description += betweenSegmentDescription;
                }
                else if (segments[0].IndexOfAny(new[] { '*', ',' }) == -1)
                {
                    var rangeItemDescription = string.Format(getDescriptionFormat(segments[0]),
                        getSingleItemDescription(segments[0]));
                    //remove any leading comma
                    rangeItemDescription = rangeItemDescription.Replace(", ", "");

                    description += string.Format(GetString("CommaStartingX0"), rangeItemDescription);
                }
            }
            else if (expression.Contains(","))
            {
                var segments = expression.Split(',');

                var descriptionContent = string.Empty;
                for (var i = 0; i < segments.Length; i++)
                {
                    if (i > 0 && segments.Length > 2)
                    {
                        descriptionContent += ",";

                        if (i < segments.Length - 1) descriptionContent += " ";
                    }

                    if (i > 0 && segments.Length > 1 && (i == segments.Length - 1 || segments.Length == 2))
                        descriptionContent += GetString("SpaceAndSpace");

                    if (segments[i].Contains("-"))
                    {
                        var betweenSegmentDescription =
                            GenerateBetweenSegmentDescription(segments[i], getRangeFormat, getSingleItemDescription);

                        //remove any leading comma
                        betweenSegmentDescription = betweenSegmentDescription.Replace(", ", "");

                        descriptionContent += betweenSegmentDescription;
                    }
                    else
                    {
                        descriptionContent += getSingleItemDescription(segments[i]);
                    }
                }

                description = string.Format(getDescriptionFormat(expression), descriptionContent);
            }
            else if (expression.Contains("-"))
            {
                description = GenerateBetweenSegmentDescription(expression, getBetweenDescriptionFormat,
                    getSingleItemDescription);
            }

            return description;
        }

        /// <summary>
        ///     Generates the between segment description
        /// </summary>
        /// <param name="betweenExpression"></param>
        /// <param name="getBetweenDescriptionFormat"></param>
        /// <param name="getSingleItemDescription"></param>
        /// <returns>The between segment description</returns>
        protected string GenerateBetweenSegmentDescription(string betweenExpression,
            Func<string, string> getBetweenDescriptionFormat, Func<string, string> getSingleItemDescription)
        {
            var description = string.Empty;
            var betweenSegments = betweenExpression.Split('-');
            var betweenSegment1Description = getSingleItemDescription(betweenSegments[0]);
            var betweenSegment2Description = getSingleItemDescription(betweenSegments[1]);
            //betweenSegment2Description = betweenSegment2Description.Replace(":00", ":59");
            var betweenDescriptionFormat = getBetweenDescriptionFormat(betweenExpression);
            description += string.Format(betweenDescriptionFormat, betweenSegment1Description,
                betweenSegment2Description);

            return description;
        }

        /// <summary>
        ///     Given time parts, will contruct a formatted time description
        /// </summary>
        /// <param name="hourExpression">Hours part</param>
        /// <param name="minuteExpression">Minutes part</param>
        /// <returns>Formatted time description</returns>
        protected string FormatTime(string hourExpression, string minuteExpression)
        {
            return FormatTime(hourExpression, minuteExpression, string.Empty);
        }

        /// <summary>
        ///     Given time parts, will contruct a formatted time description
        /// </summary>
        /// <param name="hourExpression">Hours part</param>
        /// <param name="minuteExpression">Minutes part</param>
        /// <param name="secondExpression">Seconds part</param>
        /// <returns>Formatted time description</returns>
        protected string FormatTime(string hourExpression, string minuteExpression, string secondExpression)
        {
            var hour = Convert.ToInt32(hourExpression);

            var period = string.Empty;
            if (!_use24HourTimeFormat)
            {
                period = GetString(hour >= 12 ? "PMPeriod" : "AMPeriod");
                if (period.Length > 0)
                    // add preceeding space
                    period = string.Concat(" ", period);

                if (hour > 12) hour -= 12;
                if (hour == 0) hour = 12;
            }

            var minute = Convert.ToInt32(minuteExpression).ToString();
            var second = string.Empty;
            if (!string.IsNullOrEmpty(secondExpression))
                second = string.Concat(":", Convert.ToInt32(secondExpression).ToString().PadLeft(2, '0'));

            return string.Format("{0}:{1}{2}{3}", hour.ToString().PadLeft(2, '0'), minute.PadLeft(2, '0'), second,
                period);
        }

        /// <summary>
        ///     Transforms the verbosity of the expression description by stripping verbosity from original description
        /// </summary>
        /// <param name="description">The description to transform</param>
        /// <param name="isVerbose">If true, will leave description as it, if false, will strip verbose parts</param>
        /// <returns>The transformed description with proper verbosity</returns>
        protected string TransformVerbosity(string description, bool useVerboseFormat)
        {
            if (!useVerboseFormat)
            {
                description = description.Replace(GetString("ComaEveryMinute"), string.Empty);
                description = description.Replace(GetString("ComaEveryHour"), string.Empty);
                description = description.Replace(GetString("ComaEveryDay"), string.Empty);
                description = Regex.Replace(description, @"\, ?$", "");
            }

            return description;
        }
        
        /// <summary>
        ///     Gets a localized string resource
        /// </summary>
        /// <param name="resourceName">name of the resource</param>
        /// <returns>translated resource</returns>

        protected string GetString(string resourceName)
        {
            return _localizationData.GetString(resourceName);
        }

        #region Static

        /// <summary>
        ///     Generates a human readable string for the Cron Expression
        /// </summary>
        /// <param name="expression">The cron expression string</param>
        /// <returns>The cron expression description</returns>
        public static string GetDescription(string expression)
        {
            return GetDescription(expression, new Options());
        }

        /// <summary>
        ///     Generates a human readable string for the Cron Expression
        /// </summary>
        /// <param name="expression">The cron expression string</param>
        /// <param name="options">Options to control the output description</param>
        /// <returns>The cron expression description</returns>
        public static string GetDescription(string expression, Options options)
        {
            var descripter = new ExpressionDescriptor(expression, options);
            return descripter.GetDescription(DescriptionTypeEnum.FULL);
        }

        #endregion
    }
}