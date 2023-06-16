using Beamable.Common.Scheduler;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace microserviceTests.CronBuilderTests;

public class CronBuilderTests
{
	public List<(Func<CronBuilder, string>, string)> parseTestCases = new List<(Func<CronBuilder, string>, string)>
	{
		{ (b => b.UseDefaults(), 
			"0 0 0 * * *") },
		
		{ (b => b
				.AtSecond(5)
				.UseDefaults(), 
			"5 0 0 * * *") },
		
		{ (b => b
				.AtSecond(5, 2, 6)
				.UseDefaults(), 
			"5,2,6 0 0 * * *") },
		
		{ (b => b
				.AtMinute(5)
				.UseDefaults(), 
			"0 5 0 * * *") },
		
		{ (b => b
				.AtHour(5)
				.UseDefaults(), 
			"0 0 5 * * *") },
		
		{ (b => b
				.EverySecond()
				.UseDefaults(), 
			"* * * * * *") },
		
		{ (b => b
				.EveryMinute()
				.UseDefaults(), 
			"0 * * * * *") },

		{ (b => b
				.EveryHour()
				.UseDefaults(), 
			"0 0 * * * *") },
		
		{ (b => b
				.EverySecond()
				.AtMinute(30)
				.UseDefaults(), 
			"* 30 * * * *") },
		
		{ (b => b
				.EverySecond()
				.AtHour(2)
				.UseDefaults(), 
			"* * 2 * * *") },
		
		{ (b => b
				.EverySecond()
				.AtMinute(12)
				.AtHour(2)
				.UseDefaults(), 
			"* 12 2 * * *") },
		
		{ (b => b
				.EveryMinute()
				.AtHour(4)
				.UseDefaults(), 
			"0 * 4 * * *") },
		
		{ (b => b
				.AtSecond(5)
				.AtMinute(10)
				.EveryHour()
				.UseDefaults(), 
			"5 10 * * * *") },
		
		{ (b => b
				.EveryNthSecond(2)
				.UseDefaults(), 
			"*/2 * * * * *") },
		
		{ (b => b
				.EveryNthSecond(3)
				.AtHour(8)
				.UseDefaults(), 
			"*/3 * 8 * * *") },
		
		{ (b => b
				.BetweenSeconds(0, 2)
				.AtHour(8)
				.UseDefaults(), 
			"0-2 0 8 * * *") },
		
		{ (b => b
				.BetweenSeconds(0, 2)
				.EveryNthMinute(5)
				.AtHour(8)
				.UseDefaults(), 
			"0-2 */5 8 * * *") },
		
		{ (b => b
				.ComplexSeconds("3,5-10,*/13")
				.UseDefaults(), 
			"3,5-10,*/13 0 0 * * *") },
		
		{ (b => b
				.BetweenMinutes(20,23)
				.UseDefaults(), 
			"0 20-23 0 * * *") },
		
		{ (b => b
				.ComplexMinutes("4,6-10,*/12")
				.UseDefaults(), 
			"0 4,6-10,*/12 0 * * *") },
		
		{ (b => b
				.BetweenHours(7, 20)
				.UseDefaults(), 
			"0 0 7-20 * * *") },
		
		{ (b => b
				.EveryNthHour(7)
				.UseDefaults(), 
			"0 0 */7 * * *") },
		
		{ (b => b
				.ComplexHours("8/2,4")
				.UseDefaults(), 
			"0 0 8/2,4 * * *") },
		
		{ (b => b
				.OnDayOfMonth(2)
				.UseDefaults(), 
			"0 0 0 2 * *") },
		
		{ (b => b
				.OnDayOfMonth(1,15)
				.UseDefaults(), 
			"0 0 0 1,15 * *") },
		
		{ (b => b
				.AtHour(12)
				.OnDayOfMonth(15)
				.UseDefaults(), 
			"0 0 12 15 * *") },
		
		{ (b => b
				.EveryMinute()
				.OnDayOfMonth(15)
				.UseDefaults(), 
			"0 * * 15 * *") },
		
		{ (b => b
				.AtHour(12)
				.EveryDayOfTheMonth()
				.UseDefaults(), 
			"0 0 12 * * *") },
		
		{ (b => b
				.ComplexDayOfMonth("12-18")
				.UseDefaults(), 
			"0 0 0 12-18 * *") }, 

		{ (b => b
				.AtHour(4)
				.InMonth(3)
				.UseDefaults(), 
			"0 0 4 * 3 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InJanuary()
				.UseDefaults(), 
			"0 0 9 * 1 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InFebruary()
				.UseDefaults(), 
			"0 0 9 * 2 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InMarch()
				.UseDefaults(), 
			"0 0 9 * 3 *") }, 
		
		
		{ (b => b
				.AtHour(9)
				.InApril()
				.UseDefaults(), 
			"0 0 9 * 4 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InMay()
				.UseDefaults(), 
			"0 0 9 * 5 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InJune()
				.UseDefaults(), 
			"0 0 9 * 6 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InJuly()
				.UseDefaults(), 
			"0 0 9 * 7 *") }, 

		{ (b => b
				.AtHour(9)
				.InAugust()
				.UseDefaults(), 
			"0 0 9 * 8 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InSeptember()
				.UseDefaults(), 
			"0 0 9 * 9 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InOctober()
				.UseDefaults(), 
			"0 0 9 * 10 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InNovember()
				.UseDefaults(), 
			"0 0 9 * 11 *") }, 
		
		{ (b => b
				.AtHour(9)
				.InDecember()
				.UseDefaults(), 
			"0 0 9 * 12 *") }, 
		
		{ (b => b
				.AtHour(9)
				.BetweenMonths(3, 5)
				.UseDefaults(), 
			"0 0 9 * 3-5 *") }, 
		
		{ (b => b
				.AtHour(9)
				.ComplexMonth("8,3-5")
				.UseDefaults(), 
			"0 0 9 * 8,3-5 *") }, 
		
		{ (b => b
				.AtHour(22)
				.EveryMonth()
				.UseDefaults(), 
			"0 0 22 * * *") }, 
		
		{ (b => b
				.EveryNthMonth(3)
				.UseDefaults(), 
			"0 0 0 * */3 *") }, 
		
		{ (b => b
				.OnDays(1,5,3)
				.UseDefaults(), 
			"0 0 0 * * 1,5,3") }, 
		
		{ (b => b
				.EveryNthDay(3)
				.UseDefaults(), 
			"0 0 0 * * */3") }, 
		
		{ (b => b
				.ComplexDay("2-4,6")
				.UseDefaults(), 
			"0 0 0 * * 2-4,6") }, 
		
		{ (b => b
				.BetweenDays(2,5)
				.UseDefaults(), 
			"0 0 0 * * 2-5") }, 
		
		{ (b => b
				.OnSunday()
				.UseDefaults(), 
			"0 0 0 * * 0") }, 
		
		{ (b => b
				.OnMonday()
				.UseDefaults(), 
			"0 0 0 * * 1") }, 
		
		{ (b => b
				.OnTuesday()
				.UseDefaults(), 
			"0 0 0 * * 2") }, 
		
		{ (b => b
				.OnWednesday()
				.UseDefaults(), 
			"0 0 0 * * 3") }, 
		
		{ (b => b
				.OnThursday()
				.UseDefaults(), 
			"0 0 0 * * 4") }, 
		
		{ (b => b
				.OnFriday()
				.UseDefaults(), 
			"0 0 0 * * 5") }, 
		
		{ (b => b
				.OnSaturday()
				.UseDefaults(), 
			"0 0 0 * * 6") }, 
		
		{ (b => b
				.AtMinute(30)
				.AtHour(12)
				.InApril()
				.OnFriday()
				.UseDefaults(), 
			"0 30 12 * 4 5") },

		{ (b => b
				.EveryMinute()
				.BetweenMonths(1,2)
				.UseDefaults(), 
			"0 * * * 1-2 *") }, 
		
		{ (b => b
				.EveryDayOfTheWeek()
				.UseDefaults(), 
			"0 0 0 * * *") },

		{ (b => b
				.OnDayOfMonth(3)
				.OnDays(6)
				.UseDefaults(), 
			"0 0 0 3 * 6") }, 
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
			Assert.AreEqual(actual,expected);
		}
	}

	[Test]
	public void Test()
	{
		var b = new CronBuilder();
		var cron = b
			.EveryMinute()
			.BetweenMonths(1, 2)
			.UseDefaults();
		
		Assert.AreEqual("0 * * * 1-2 *", cron);
			// "0 * * * 1-2 *") }, 
	}
}
