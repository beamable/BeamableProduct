using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Beamable.Common.CronExpression
{
	/// <summary>
	///     Cron Expression Parser
	/// </summary>
	public class ExpressionParser
	{
		private readonly string _expression;
		private readonly Options _options;
		private readonly CultureInfo _en_culture;
		private static readonly string SecondRegex =
			@"^(?:\*|(?:[0-5]?\d(?:-[0-5]?\d)?(?:\/[0-5]?\d)?)(?:,(?:[0-5]?\d(?:-[0-5]?\d)?(?:\/[0-5]?\d)?))*)$";
		private static readonly string MinuteRegex =
			@"^(?:\*|(?:[0-5]?\d(?:-[0-5]?\d)?(?:\/[0-5]?\d)?)(?:,(?:[0-5]?\d(?:-[0-5]?\d)?(?:\/[0-5]?\d)?))*)$";
		private static readonly string HourRegex =
			@"^(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?(?:\/(?:[01]?\d|2[0-3]))?(?:,(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?(?:\/(?:[01]?\d|2[0-3]))?)*)$";
		private static readonly string DayOfMonthRegex =
			@"^(?:\*|(?:0?[1-9]|[12]\d|3[01])(?:-(?:0?[1-9]|[12]\d|3[01]))?(?:\/(?:0?[1-9]|[12]\d|3[01]))?(?:,(?:0?[1-9]|[12]\d|3[01])(?:-(?:0?[1-9]|[12]\d|3[01]))?(?:\/(?:0?[1-9]|[12]\d|3[01]))?)*)$";
		private static readonly string MonthRegex =
			@"^(?:\*|(?:[1-9]|1[0-2])(?:-(?:[1-9]|1[0-2]))?(?:\/(?:[1-9]|1[0-2]))?(?:,(?:[1-9]|1[0-2])(?:-(?:[1-9]|1[0-2]))?(?:\/(?:[1-9]|1[0-2]))?)*)$";
		private static readonly string DayOfWeekRegex =
			@"^(?:\*|(?:[0-7])(?:-(?:[0-7]))?(?:\/(?:[0-7]))?(?:,(?:[0-7])(?:-(?:[0-7]))?(?:\/(?:[0-7]))?)*)$";
		private static readonly string YearRegex =
			@"^(?:\*|(?:19[7-9]\d|20\d{2})(?:-(?:19[7-9]\d|20\d{2}))?(?:\/(?:19[7-9]\d|20\d{2}))?(?:,(?:19[7-9]\d|20\d{2})(?:-(?:19[7-9]\d|20\d{2}))?(?:\/(?:19[7-9]\d|20\d{2}))?)*)$";


		/// <summary>
		///     Initializes a new instance of the <see cref="ExpressionParser" /> class
		/// </summary>
		/// <param name="expression">The cron expression string</param>
		/// <param name="options">Parsing options</param>
		public ExpressionParser(string expression, Options options)
		{
			_expression = expression;
			_options = options;
			_en_culture = new CultureInfo("en-US"); //Default to English
		}

		/// <summary>
		///     Parses the cron expression string
		/// </summary>
		/// <returns>A 7 part string array, one part for each component of the cron expression (seconds, minutes, etc.)</returns>
		public string[] Parse(out ErrorData errorData)
		{
			// Initialize all elements of parsed array to empty strings
			errorData = new ErrorData();
			var parsed = new string[7].Select(el => "").ToArray();

			if (string.IsNullOrEmpty(_expression))
			{
				throw new FormatException($"Error: Field 'expression' not found.");
			}

			var expressionPartsTemp = _expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			if (expressionPartsTemp.Length != 7)
				throw new FormatException($"Error: Expression has {expressionPartsTemp.Length} parts. Exactly 7 parts are required.");

			StringBuilder errorMessage = new StringBuilder();
			bool validateSeconds = ValidateField(expressionPartsTemp[0], SecondRegex, "seconds", errorMessage, 0, 59);
			bool validateMinutes = ValidateField(expressionPartsTemp[1], MinuteRegex, "minutes", errorMessage, 0, 59);
			bool validateHours = ValidateField(expressionPartsTemp[2], HourRegex, "hours", errorMessage, 0,23);
			bool validateDayOfMonth = ValidateField(expressionPartsTemp[3], DayOfMonthRegex, "dayOfMonth", errorMessage, 1, 31);
			bool validateMonth = ValidateField(expressionPartsTemp[4], MonthRegex, "month", errorMessage, 1, 12);
			bool validateDayOfWeek = ValidateField(expressionPartsTemp[5], DayOfWeekRegex, "dayOfWeek", errorMessage, 1, 7);
			bool validateYear = ValidateField(expressionPartsTemp[6], YearRegex, "year", errorMessage);
			if (!validateSeconds || !validateMinutes || !validateHours || !validateDayOfMonth || !validateMonth || !validateDayOfWeek || !validateYear)
			{
				errorData.ErrorMessage = errorMessage.ToString();
				return null;
			}

			if (expressionPartsTemp.Length == 7)
				parsed = expressionPartsTemp;

			NormalizeExpression(parsed);
			return parsed;
		}

		/// <summary>
		///     Converts cron expression components into consistent, predictable formats.
		/// </summary>
		/// <param name="expressionParts">A 7 part string array, one part for each component of the cron expression</param>
		private void NormalizeExpression(string[] expressionParts)
		{
			// Convert ? to * only for DOM and DOW
			expressionParts[3] = expressionParts[3].Replace("?", "*");
			expressionParts[5] = expressionParts[5].Replace("?", "*");

			// Convert 0/, 1/ to */
			if (expressionParts[0].StartsWith("0/"))
				// Seconds
				expressionParts[0] = expressionParts[0].Replace("0/", "*/");

			if (expressionParts[1].StartsWith("0/"))
				// Minutes
				expressionParts[1] = expressionParts[1].Replace("0/", "*/");

			if (expressionParts[2].StartsWith("0/"))
				// Hours
				expressionParts[2] = expressionParts[2].Replace("0/", "*/");

			if (expressionParts[3].StartsWith("1/"))
				// DOM
				expressionParts[3] = expressionParts[3].Replace("1/", "*/");

			if (expressionParts[4].StartsWith("1/"))
				// Month
				expressionParts[4] = expressionParts[4].Replace("1/", "*/");

			if (expressionParts[5].StartsWith("1/"))
				// DOW
				expressionParts[5] = expressionParts[5].Replace("1/", "*/");

			if (expressionParts[6].StartsWith("1/"))
				// Years
				expressionParts[6] = expressionParts[6].Replace("1/", "*/");

			// Adjust DOW based on dayOfWeekStartIndexZero option
			expressionParts[5] = Regex.Replace(expressionParts[5], @"(^\d)|([^#/\s]\d)", t =>
			{
				//skip anything preceeding by # or /
				var dowDigits =
					Regex.Replace(t.Value, @"\D", ""); // extract digit part (i.e. if "-2" or ",2", just take 2)
				var dowDigitsAdjusted = dowDigits;

				if (_options.DayOfWeekStartIndexZero)
				{
					// "7" also means Sunday so we will convert to "0" to normalize it
					if (dowDigits == "7") dowDigitsAdjusted = "0";
				}
				else
				{
					// If dayOfWeekStartIndexZero==false, Sunday is specified as 1 and Saturday is specified as 7.
					// To normalize, we will shift the  DOW number down so that 1 becomes 0, 2 becomes 1, and so on.
					dowDigitsAdjusted = (int.Parse(dowDigits) - 1).ToString();
				}

				return t.Value.Replace(dowDigits, dowDigitsAdjusted);
			});

			// Convert DOM '?' to '*'
			if (expressionParts[3] == "?") expressionParts[3] = "*";

			// Convert 0 second to (empty)
			if (expressionParts[0] == "0") expressionParts[0] = string.Empty;

			// If time interval is specified for seconds or minutes and next time part is single item, make it a "self-range" so
			// the expression can be interpreted as an interval 'between' range.
			//     For example:
			//     0-20/3 9 * * * => 0-20/3 9-9 * * * (9 => 9-9)
			//     */5 3 * * * => */5 3-3 * * * (3 => 3-3)
			if (expressionParts[2].IndexOfAny(new[] { '*', '-', ',', '/' }) == -1 &&
				(Regex.IsMatch(expressionParts[1], @"\*|\/") || Regex.IsMatch(expressionParts[0], @"\*|\/")))
				expressionParts[2] += $"-{expressionParts[2]}";

			// Loop through all parts and apply global normalization
			for (var i = 0; i < expressionParts.Length; i++)
			{
				// convert all '*/1' to '*'
				if (expressionParts[i] == "*/1") expressionParts[i] = "*";

				/* Convert Month,DOW,Year step values with a starting value (i.e. not '*') to between expressions.
                   This allows us to reuse the between expression handling for step values.
        
                   For Example:
                    - month part '3/2' will be converted to '3-12/2' (every 2 months between March and December)
                    - DOW part '3/2' will be converted to '3-6/2' (every 2 days between Tuesday and Saturday)
                */

				if (expressionParts[i].Contains("/") && expressionParts[i].IndexOfAny(new[] { '*', '-', ',' }) == -1)
				{
					string stepRangeThrough = null;
					switch (i)
					{
						case 4:
							stepRangeThrough = "12";
							break;
						case 5:
							stepRangeThrough = "6";
							break;
						case 6:
							stepRangeThrough = "9999";
							break;
						default:
							stepRangeThrough = null;
							break;
					}

					if (stepRangeThrough != null)
					{
						var parts = expressionParts[i].Split('/');
						expressionParts[i] = $"{parts[0]}-{stepRangeThrough}/{parts[1]}";
					}
				}
			}
		}
		
		private bool ValidateField(string field, string pattern, string partName, StringBuilder errorMessage, int? minValue = null, int? maxValue = null)
		{
			if (!Regex.IsMatch(field, pattern))
			{
				int min = minValue ?? 0;
				int max = maxValue ?? 9;
				
				errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. CRON supports only numbers [{min}-{max}] and special characters [,-*/].");
				return false;
			}

			// Additional validation for numeric values
			if (field == "*" || field == "?")
			{
				return true;
			}

			string[] segments = field.Split(',');

			foreach (string segment in segments)
			{
				string rangePart = segment;

				// Handle Step and remove it from part
				if (segment.Contains("/"))
				{
					string[] stepSplit = segment.Split('/');
					if (stepSplit.Length != 2)
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Missing value after step (/)");
						return false;
					}

					rangePart = stepSplit[0];
					string stepPart = stepSplit[1];

					if (!int.TryParse(stepPart, out int step) || step <= 0)
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Step value (value after '/') must be a positive number.");
						return false;
					}
						
				}

				// Handle range or single number
				if (rangePart.Contains("-"))
				{
					string[] rangeSplit = rangePart.Split('-');
					if (rangeSplit.Length != 2)
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Missing range values.");
						return false;
					}

					if (!int.TryParse(rangeSplit[0], out int start) || !int.TryParse(rangeSplit[1], out int end))
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Range value must be a positive number.");
						return false;
					}

					if (!ValidateMinValue(start) || !ValidateMaxValue(end))
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Values must be between {minValue.Value} and {maxValue.Value}.");
						return false;
					}

					if (start > end)
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Range start ({start}) is greater than end ({end})");
						return false;
					}
						
				}
				else
				{
					if (!int.TryParse(rangePart, out int value))
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Value must be a positive number.");
						return false;
					}


					if (!ValidateMinValue(value) || !ValidateMaxValue(value))
					{
						errorMessage.AppendLine($"Error: CRON validation is invalid for {partName}. Values must be between {minValue.Value} and {maxValue.Value}.");
						return false;
					}
				}
			}

			return true;

			bool ValidateMaxValue(int value)
			{
				return !maxValue.HasValue || value <= maxValue;
			}

			bool ValidateMinValue(int value)
			{
				return !minValue.HasValue || value >= minValue;
			}
		}
		
		/// <summary>
		///     Converts schedule definition into cron expression
		/// </summary>
		/// <param name="scheduleDefinition">Schedule definition</param>
		/// <returns>The cron expression<returns>
		public static string ScheduleDefinitionToCron(ScheduleDefinition scheduleDefinition)
		{
			var second = ConvertToCronString(scheduleDefinition.second);
			var minute = ConvertToCronString(scheduleDefinition.minute);
			var hour = ConvertToCronString(scheduleDefinition.hour);
			var dayOfMonth = ConvertToCronString(scheduleDefinition.dayOfMonth);
			var month = ConvertToCronString(scheduleDefinition.month);
			var dayOfWeek = ConvertToCronString(scheduleDefinition.dayOfWeek);
			var year = ConvertToCronString(scheduleDefinition.year);

			var expression = $"{second} {minute} {hour} {dayOfMonth} {month} {dayOfWeek} {year}";
			return expression;
		}

		public static string ConvertToCronString(IReadOnlyList<string> part)
		{
			if (part.Contains("*") && part.Count == 1)
				return "*";

			var outputParts = new List<string>();
			var currentRange = new List<int>();
			int? lastNumber = null;

			void AddCurrentRangeToOutput(string partValue = null)
			{
				if (currentRange.Count == 0)
					return;
				
				int start = currentRange[0];
				int end = currentRange.Last();

				string result = (start == end) ? $"{start}" : $"{start}-{end}";
				if (partValue != null)
				{
					result += partValue;
				}

				outputParts.Add(result);
				currentRange.Clear();
				lastNumber = null;
			}

			foreach (var item in part)
			{
				if (item.StartsWith("/"))
				{
					AddCurrentRangeToOutput(item);
				}
				else if (int.TryParse(item, out int num))
				{
					if (lastNumber.HasValue && num != lastNumber.Value + 1 && num != lastNumber.Value - 1)
					{
						AddCurrentRangeToOutput();
					}

					currentRange.Add(num);
					lastNumber = num;
				}
			}

			AddCurrentRangeToOutput();
			return string.Join(",", outputParts);
		}
	}
}
