using System;
using System.Linq;

namespace Beamable.Common.Scheduler
{

	public interface ICronComponent
	{
		
	}

	public interface ICronInitial : ICronSeconds
	{
		
	}

	public interface ICronComplete : ICronComponent
	{
		string ToCron();
	}
	
	public interface ICronSeconds : ICronComponent
	{
		ICronMinutes EverySecond();
		ICronMinutes EveryNthSecond(int n);
		ICronMinutes BetweenSeconds(int start, int end);
		ICronMinutes AtSecond(int second, params int[] additionalSeconds);
		ICronMinutes ComplexSeconds(string secondStr);
	}
	public interface ICronMinutes: ICronComponent
	{
		ICronHours EveryMinute();
		ICronHours EveryNthMinute(int n);
		ICronHours BetweenMinutes(int start, int end);
		ICronHours AtMinute(int minute, params int[] additionalMinutes);
		ICronHours ComplexMinutes(string minuteStr);
		
	}
	public interface ICronHours: ICronComponent
	{
		ICronDaySplit EveryHour();
		ICronDaySplit EveryNthHour(int n);
		ICronDaySplit BetweenHours(int start, int end);
		ICronDaySplit AtHour(int hour, params int[] additionalHours);
		ICronDaySplit ComplexHours(string hourStr);
	}

	public interface ICronDaySplit : ICronComponent, ICronDayOfWeek, ICronDayOfMonth
	{
		
	}
	
	public interface ICronDayOfMonth: ICronComponent
	{
		ICronMonth EveryDayOfTheMonth();
		ICronMonth EveryNthDayOfTheMonth(int n);
		ICronMonth ComplexDayOfMonth(string dayOfMonthStr);
		ICronMonth OnDayOfMonth(int dayOfMonth, params int[] additionalDaysOfMonth); 
		ICronMonth BetweenDaysOfMonth(int start, int end);
	}
	public interface ICronMonth: ICronComponent
	{
		ICronComplete EveryMonth();
		ICronComplete EveryNthMonth(int n);
		ICronComplete BetweenMonths(int start, int end);
		ICronComplete InMonth(int month, params int[] additionalMonths);
		ICronComplete ComplexMonth(string monthStr);
	}
	
	public interface ICronDayOfWeek: ICronComponent
	{
		ICronMonth EveryDayOfTheWeek();
		ICronMonth EveryNthDay(int n);
		ICronMonth BetweenDays(int start, int end);
		ICronMonth OnDays(int day, params int[] additionalDays);
		ICronMonth ComplexDay(string dayStr);
	}

	public static class ICronExtensions
	{
		public static ICronComplete EveryDay(this ICronMonth self) => self.EveryMonth();
		public static ICronMonth EveryDay(this ICronDaySplit self) => self.EveryDayOfTheWeek();
		public static string ToCron(this ICronMonth self) => self.EveryMonth().ToCron();

		public static ICronComplete InJanuary(this ICronMonth self) => self.InMonth(1);
		public static ICronComplete InFebruary(this ICronMonth self) => self.InMonth(2);
		public static ICronComplete InMarch(this ICronMonth self) => self.InMonth(3);
		public static ICronComplete InApril(this ICronMonth self) => self.InMonth(4);
		public static ICronComplete InMay(this ICronMonth self) => self.InMonth(5);
		public static ICronComplete InJune(this ICronMonth self) => self.InMonth(6);
		public static ICronComplete InJuly(this ICronMonth self) => self.InMonth(7);
		public static ICronComplete InAugust(this ICronMonth self) => self.InMonth(8);
		public static ICronComplete InSeptember(this ICronMonth self) => self.InMonth(9);
		public static ICronComplete InOctober(this ICronMonth self) => self.InMonth(10);
		public static ICronComplete InNovember(this ICronMonth self) => self.InMonth(11);
		public static ICronComplete InDecember(this ICronMonth self) => self.InMonth(12);

		public static ICronMonth OnSunday(this ICronDayOfWeek self) => self.OnDays(0);
		public static ICronMonth OnMonday(this ICronDayOfWeek self) => self.OnDays(1);
		public static ICronMonth OnTuesday(this ICronDayOfWeek self) => self.OnDays(2);
		public static ICronMonth OnWednesday(this ICronDayOfWeek self) => self.OnDays(3);
		public static ICronMonth OnThursday(this ICronDayOfWeek self) => self.OnDays(4);
		public static ICronMonth OnFriday(this ICronDayOfWeek self) => self.OnDays(5);
		public static ICronMonth OnSaturday(this ICronDayOfWeek self) => self.OnDays(6);


		public static ICronComplete AtMidnight(this ICronInitial self) => Daily(self,0);
		
		public static ICronComplete Monthly(this ICronInitial self, int day=1) =>
			self
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnDayOfMonth(day)
				.EveryMonth()
			;

		public static ICronComplete TwiceMonthly(this ICronInitial self, int day1=1, int day2=15) =>
			self
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnDayOfMonth(day1, day2)
				.EveryMonth()
			;
		
		public static ICronComplete Weekly(this ICronInitial self, int day=0, int hour=0) =>
			self
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(hour)
				.OnDays(day)
				.EveryMonth()
			;

		public static ICronComplete Daily(this ICronInitial self, int hour=0) =>
			self
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(hour)
				.EveryDayOfTheWeek()
				.EveryMonth()
			;

	}
	


	public class CronBuilder : ICronSeconds, ICronInitial, ICronDaySplit, ICronMinutes, ICronHours, ICronMonth, ICronDayOfMonth, ICronDayOfWeek, ICronComplete
	{
		private const string STAR = "*";
		private const string ZERO = "0";
		private const string NULL = null;
		
		private const int SECOND_INDEX = 0;
		private const int MINUTE_INDEX = 1;
		private const int HOUR_INDEX = 2;
		private const int DAY_OF_MONTH_INDEX = 3;
		private const int MONTH_INDEX = 4;
		private const int DAY_OF_WEEK_INDEX = 5;

		private string secondStr { get => components[SECOND_INDEX]; set => components[SECOND_INDEX] = value; }
		private string minuteStr { get => components[MINUTE_INDEX]; set => components[MINUTE_INDEX] = value; }
		private string hourStr { get => components[HOUR_INDEX]; set => components[HOUR_INDEX] = value; }
		private string dayOfMonthStr { get => components[DAY_OF_MONTH_INDEX]; set => components[DAY_OF_MONTH_INDEX] = value; }
		private string monthStr { get => components[MONTH_INDEX]; set => components[MONTH_INDEX] = value; }
		private string dayOfWeekStr { get => components[DAY_OF_WEEK_INDEX]; set => components[DAY_OF_WEEK_INDEX] = value; }

		private int? starAfterIndex = null;
		
		
		private string[] defaults = new string[] { ZERO, ZERO, ZERO, STAR, STAR, STAR };
		private string[] components = new string[] { NULL, NULL, NULL, NULL, NULL, NULL };
		
		private string BuildString()
		{
			for (var i = 0; i < components.Length; i++)
			{
				if (i > starAfterIndex)
				{
					defaults[i] = STAR;
				}
			}

			var buffer = new string[components.Length];
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i] ?? defaults[i];
				buffer[i] = component;
			}

			return string.Join(" ", buffer);
			// return $"{secondStr } {minuteStr } {hourStr } {dayOfMonthStr } {monthStr } {dayOfWeekStr }";
			// return $"{secondStr ?? ZERO} {minuteStr ?? ZERO} {hourStr ?? ZERO} {dayOfMonthStr ?? ZERO} {monthStr ?? ZERO} {dayOfWeekStr ?? ZERO}";
		}

		public string ToCron()
		{
			return BuildString();
		}

		public ICronMonth EveryDayOfTheWeek()
		{
			dayOfWeekStr = STAR;
			starAfterIndex = DAY_OF_WEEK_INDEX;
			return this;
		}

		public ICronMonth OnDayOfMonth(int dayOfMonth, params int[] daysOfMonth)
		{
			daysOfMonth = Combine(dayOfMonth, daysOfMonth);
			ValidateDayOfMonth(daysOfMonth);
			dayOfMonthStr = string.Join(",", daysOfMonth);
			return this;
		}

		public ICronMonth BetweenDaysOfMonth(int start, int end)
		{
			ValidateDayOfMonth(start, end);
			dayOfMonthStr = $"{start}-{end}";
			return this;
		}

		public ICronMonth EveryDayOfTheMonth()
		{
			dayOfMonthStr = STAR;
			starAfterIndex = DAY_OF_MONTH_INDEX;
			return this;
		}

		public ICronMonth EveryNthDayOfTheMonth(int n)
		{
			ValidateDayOfMonth(n);
			dayOfMonthStr = $"*/{n}";
			starAfterIndex = DAY_OF_MONTH_INDEX;
			return this;
		}

		public ICronMonth ComplexDayOfMonth(string dayOfMonthStr)
		{
			this.dayOfMonthStr = dayOfMonthStr;
			return this;
		}

		public ICronMonth EveryNthDay(int n)
		{
			ValidateDays(n);
			dayOfWeekStr = $"*/{n}";
			starAfterIndex = DAY_OF_WEEK_INDEX;
			return this;
		}

		public ICronMonth BetweenDays(int start, int end)
		{
			ValidateDays(start, end);
			dayOfWeekStr = $"{start}-{end}";
			return this;
		}

		public ICronMonth OnDays(int day, params int[] days)
		{
			days = Combine(day, days);
			ValidateDays(days);
			dayOfWeekStr = string.Join(",", days);
			return this;
		}

		public ICronMonth ComplexDay(string dayStr)
		{
			this.dayOfWeekStr = dayStr;
			return this;
		}

		public ICronComplete EveryMonth()
		{
			monthStr = STAR;
			starAfterIndex = MONTH_INDEX;
			return this;
		}

		public ICronComplete EveryNthMonth(int n)
		{
			ValidateMonth(n);
			monthStr = $"*/{n}";
			starAfterIndex = MONTH_INDEX;
			return this;
		}

		public ICronComplete BetweenMonths(int start, int end)
		{
			ValidateMonth(start, end);
			monthStr = $"{start}-{end}";
			return this;
		}

		public ICronComplete InMonth(int month, params int[] months)
		{
			months = Combine(month, months);
			ValidateMonth(months);
			monthStr = string.Join(",", months);
			return this;
		}

		public ICronComplete ComplexMonth(string monthStr)
		{
			this.monthStr = monthStr;
			return this;
		}

		public ICronDaySplit EveryHour()
		{
			hourStr = STAR;
			starAfterIndex = HOUR_INDEX;
			return this;
		}


		public ICronDaySplit EveryNthHour(int n)
		{
			ValidateHour(n);
			starAfterIndex = HOUR_INDEX;
			hourStr = $"*/{n}";
			return this;
		}

		public ICronDaySplit BetweenHours(int start, int end)
		{
			ValidateHour(start, end);
			hourStr = $"{start}-{end}";
			return this;
		}

		public ICronDaySplit AtHour(int hour, params int[] hours)
		{
			hours = Combine(hour, hours);
			if (hours.Length == 0) hours = new int[] { 0 };
			ValidateHour(hours);
			
			hourStr = string.Join(",", hours);
			return this;
		}

		public ICronDaySplit ComplexHours(string hourStr)
		{
			this.hourStr = hourStr;
			return this;
		}

		public ICronHours EveryMinute()
		{
			minuteStr = STAR;
			starAfterIndex = MINUTE_INDEX;
			return this;
		}

		public ICronHours EveryNthMinute(int n)
		{
			ValidateMinute(n);
			starAfterIndex = MINUTE_INDEX;
			minuteStr = $"*/{n}";
			return this;
		}

		public ICronHours BetweenMinutes(int start, int end)
		{
			ValidateMinute(start, end);
			minuteStr = $"{start}-{end}";
			return this;
		}

		public ICronHours AtMinute(int minute, params int[] minutes)
		{
			minutes = Combine(minute, minutes);
			ValidateMinute(minutes);
			
			minuteStr = string.Join(",", minutes);
			return this;
		}

		public ICronHours ComplexMinutes(string minuteStr)
		{
			this.minuteStr = minuteStr;
			return this;
		}

		public ICronMinutes EverySecond()
		{
			secondStr = STAR;
			starAfterIndex = SECOND_INDEX;
			return this;
		}

		public ICronMinutes EveryNthSecond(int n)
		{
			ValidateSecond(n);
			secondStr = $"*/{n}";
			starAfterIndex = SECOND_INDEX;
			return this;
		}

		public ICronMinutes BetweenSeconds(int start, int end)
		{
			ValidateSecond(start, end);
			secondStr = $"{start}-{end}";
			return this;
		}

		public ICronMinutes AtSecond(int second, params int[] seconds)
		{
			seconds = Combine(second, seconds);
			ValidateSecond(seconds);
			
			secondStr = string.Join(",", seconds);
			return this;
		}

		private int[] Combine(int arg, int[] moreArgs)
		{
			var output = new int[moreArgs.Length + 1];
			output[0] = arg;
			for (var i = 1; i < output.Length; i++)
			{
				output[i] = moreArgs[i - 1];
			}

			return output;
		}

		private void ValidateSecond(params int[] seconds)
		{
			foreach (var second in seconds)
				if (second < 0 || second > 59)
					throw new ArgumentOutOfRangeException($"cron based second value must be 0-59. second=[{second}]");
		}
		private void ValidateMinute(params int[] minutes)
		{
			foreach (var minute in minutes)
				if (minute < 0 || minute > 59)
					throw new ArgumentOutOfRangeException($"cron based minute value must be 0-59. minute=[{minute}]");
		}
		
		private void ValidateHour(params int[] hours)
		{
			foreach (var hour in hours)
				if (hour < 0 || hour > 59)
					throw new ArgumentOutOfRangeException($"cron based hour value must be 0-23. hour=[{hour}]");
		}
		
		private void ValidateDayOfMonth(params int[] doms)
		{
			foreach (var dom in doms)
				if (dom < 1 || dom > 31)
					throw new ArgumentOutOfRangeException($"cron based day-of-month value must be 1-31. day-of-month=[{dom}]");
		}
		
		private void ValidateMonth(params int[] months)
		{
			foreach (var month in months)
				if (month < 1 || month > 12)
					throw new ArgumentOutOfRangeException($"cron based month value must be 1-12. month=[{month}]");
		}
		
		private void ValidateDays(params int[] days)
		{
			foreach (var day in days)
				if (day < 0 || day > 6)
					throw new ArgumentOutOfRangeException($"cron based day value must be 0-6. day=[{day}]");
		}

		public ICronMinutes ComplexSeconds(string secondStr)
		{
			this.secondStr = secondStr;
			return this;
		}
	}
	
}
