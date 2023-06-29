using Beamable.Common.Scheduler;
using NUnit.Framework;
using Swan;
using System;
using System.Collections.Generic;

namespace microserviceTests.CronBuilderTests;

public class CronBuilderTests
{
	public List<(Func<ICronInitial, string>, string)> parseTestCases = new List<(Func<ICronInitial, string>, string)>
	{
		
		{ (b => b
				.AtMidnight()
				.ToCron(), 
			"0 0 0 * * *") },
		
		{ (b => b
				.Weekly()
				.ToCron(), 
			"0 0 0 * * 0") },
		
		{ (b => b
				.Weekly(day: 5)
				.ToCron(), 
			"0 0 0 * * 5") },
		
		{ (b => b
				.Weekly(day: 5, hour: 12)
				.ToCron(), 
			"0 0 12 * * 5") },

		{ (b => b
				.Daily()
				.ToCron(), 
			"0 0 0 * * *") },
		
		{ (b => b
				.Daily(hour: 3)
				.ToCron(), 
			"0 0 3 * * *") },
		
		{ (b => b
				.Monthly()
				.ToCron(), 
			"0 0 0 1 * *") },
		
		{ (b => b
				.Monthly(day: 15)
				.ToCron(), 
			"0 0 0 15 * *") },
		
		{ (b => b
				.TwiceMonthly()
				.ToCron(), 
			"0 0 0 1,15 * *") },
		
		{ (b => b
				.TwiceMonthly(day1:3, day2:18)
				.ToCron(), 
			"0 0 0 3,18 * *") },
		
		{ (b => b
				.AtSecond(30)
				.AtMinute(0)
				.AtHour(12)
				.EveryDayOfTheWeek()
				.EveryMonth()
				.ToCron(), 
			"30 0 12 * * *") },
		
		{ (b => b
				.AtSecond(5, 2, 6)
				.AtMinute(0)
				.AtHour(12)
				.EveryDayOfTheMonth()
				.EveryMonth()
				.ToCron(), 
			"5,2,6 0 12 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(5)
				.AtHour(0)
				.EveryDayOfTheWeek()
				.EveryMonth()
				.ToCron(), 
			"0 5 0 * * *") }, // not allowed?
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(5)
				.EveryDayOfTheWeek()
				.EveryMonth()
				.ToCron(), 
			"0 0 5 * * *") },
		
		{ (b => b
				.EverySecond()
				.EveryMinute()
				.EveryHour()
				.EveryDay()
				.ToCron(), 
			"* * * * * *") },

		{ (b => b
				.AtSecond(0)
				.EveryMinute()
				.EveryHour()
				.EveryDay()
				.ToCron(), 
			"0 * * * * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.EveryHour()
				.EveryDay()
				.ToCron(), 
			"0 0 * * * *") },
		
		{ (b => b
				.EverySecond()
				.AtMinute(30)
				.EveryHour()
				.EveryDay()
				.ToCron(), 
			"* 30 * * * *") },
		
		{ (b => b
				.EverySecond()
				.EveryMinute()
				.AtHour(2)
				.EveryDay()
				.ToCron(), 
			"* * 2 * * *") },
		
		{ (b => b
				.EverySecond()
				.AtMinute(12)
				.AtHour(2)
				.EveryDay()
				.ToCron(), 
			"* 12 2 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.EveryMinute()
				.AtHour(4)
				.EveryDay()
				.ToCron(), 
			"0 * 4 * * *") },
		
		{ (b => b
				.AtSecond(5)
				.AtMinute(10)
				.EveryHour()
				.EveryDay()
				.ToCron(), 
			"5 10 * * * *") },
		
		{ (b => b
				.EveryNthSecond(2)
				.EveryMinute()
				.EveryHour()
				.EveryDay()
				.ToCron(), 
			"*/2 * * * * *") },
		
		{ (b => b
				.EveryNthSecond(3)
				.EveryMinute()
				.AtHour(8)
				.EveryDay()
				.ToCron(), 
			"*/3 * 8 * * *") },
		
		{ (b => b
				.BetweenSeconds(0,2)
				.EveryMinute()
				.AtHour(8)
				.EveryDay()
				.ToCron(), 
			"0-2 * 8 * * *") },
		
		{ (b => b
				.BetweenSeconds(0, 2)
				.EveryNthMinute(5)
				.AtHour(8)
				.EveryDay()
				.ToCron(), 
			"0-2 */5 8 * * *") },
		
		{ (b => b
				.ComplexSeconds("3,5-10,*/13")
				.AtMinute(0)
				.AtHour(0)
				.EveryDay()
				.ToCron(), 
			"3,5-10,*/13 0 0 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.BetweenMinutes(20,23)
				.AtHour(0)
				.EveryDay()
				.ToCron(), 
			"0 20-23 0 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.ComplexMinutes("4,6-10,*/12")
				.AtHour(0)
				.EveryDay()
				.ToCron(), 
			"0 4,6-10,*/12 0 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.BetweenHours(7, 20)
				.EveryDay()
				.ToCron(), 
			"0 0 7-20 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.EveryNthHour(7)
				.EveryDay()
				.ToCron(), 
			"0 0 */7 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.ComplexHours("8/2,4")
				.EveryDay()
				.ToCron(), 
			"0 0 8/2,4 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnDayOfMonth(2)
				.EveryMonth()
				.ToCron(), 
			"0 0 0 2 * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnDayOfMonth(1,15)
				.EveryMonth()
				.ToCron(), 
			"0 0 0 1,15 * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(12)
				.OnDayOfMonth(15)
				.EveryMonth()
				.ToCron(), 
			"0 0 12 15 * *") },
		
		{ (b => b
				.AtSecond(0)
				.EveryMinute()
				.EveryHour()
				.OnDayOfMonth(15)
				.EveryMonth()
				.ToCron(), 
			"0 * * 15 * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(12)
				.EveryDayOfTheMonth()
				.EveryMonth()
				.ToCron(), 
			"0 0 12 * * *") },
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.ComplexDayOfMonth("12-18")
				.EveryMonth()
				.ToCron(), 
			"0 0 0 12-18 * *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(4)
				.EveryDayOfTheWeek()
				.InMonth(3)
				.ToCron(), 
			"0 0 4 * 3 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InJanuary()
				
				.ToCron(), 
			"0 0 9 * 1 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InFebruary()
				.ToCron(), 
			"0 0 9 * 2 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InMarch()
				.ToCron(), 
			"0 0 9 * 3 *") }, 
		
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InApril()
				.ToCron(), 
			"0 0 9 * 4 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InMay()
				.ToCron(), 
			"0 0 9 * 5 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InJune()
				.ToCron(), 
			"0 0 9 * 6 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InJuly()
				.ToCron(), 
			"0 0 9 * 7 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InAugust()
				.ToCron(), 
			"0 0 9 * 8 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InSeptember()
				.ToCron(), 
			"0 0 9 * 9 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InOctober()
				.ToCron(), 
			"0 0 9 * 10 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InNovember()
				.ToCron(), 
			"0 0 9 * 11 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.InDecember()
				.ToCron(), 
			"0 0 9 * 12 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.BetweenMonths(3, 5)
				.ToCron(), 
			"0 0 9 * 3-5 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(9)
				.EveryDay()
				.ComplexMonth("8,3-5")
				.ToCron(), 
			"0 0 9 * 8,3-5 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(22)
				.EveryDay()
				.EveryMonth()
				.ToCron(), 
			"0 0 22 * * *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.EveryDay()
				.EveryNthMonth(3)
				.ToCron(), 
			"0 0 0 * */3 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnDays(1,5,3)
				.ToCron(), 
			"0 0 0 * * 1,5,3") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.EveryNthDay(3)
				.ToCron(), 
			"0 0 0 * * */3") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.ComplexDay("2-4,6")
				.ToCron(), 
			"0 0 0 * * 2-4,6") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.BetweenDays(2,5)
				.ToCron(), 
			"0 0 0 * * 2-5") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnSunday()
				.ToCron(), 
			"0 0 0 * * 0") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnMonday()
				.ToCron(), 
			"0 0 0 * * 1") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnTuesday()
				.ToCron(), 
			"0 0 0 * * 2") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnWednesday()
				.ToCron(), 
			"0 0 0 * * 3") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnThursday()
				.ToCron(), 
			"0 0 0 * * 4") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnFriday()
				.ToCron(), 
			"0 0 0 * * 5") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.OnSaturday()
				.ToCron(), 
			"0 0 0 * * 6") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(30)
				.AtHour(12)
				.OnFriday()
				.InApril()
				.ToCron(), 
			"0 30 12 * 4 5") },
		
		{ (b => b
				.AtSecond(0)
				.EveryMinute()
				.EveryHour()
				.EveryDay()
				.BetweenMonths(1,2)
				.ToCron(), 
			"0 * * * 1-2 *") }, 
		
		{ (b => b
				.AtSecond(0)
				.AtMinute(0)
				.AtHour(0)
				.EveryDayOfTheWeek()
				.ToCron(), 
			"0 0 0 * * *") },
		
	};

	[Test]
	public void Parse()
	{
		//https://github.com/atifaziz/NCrontab
		for (var i = 0; i < parseTestCases.Count; i++)
		{
			var builder = new CronBuilder();
			var test = parseTestCases[i];
			var actual = test.Item1(builder);
			var expected = test.Item2;
			
			Assert.AreEqual(expected, actual, $"cron expression at index=[{i}] failed. Was=[{actual}] Expected=[{expected}]");
		}
	}

}
