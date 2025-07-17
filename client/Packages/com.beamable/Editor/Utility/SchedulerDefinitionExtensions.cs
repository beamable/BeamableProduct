using Beamable.Common.Content;
using Beamable.CronExpression;
using UnityEngine;

namespace Beamable.Editor.Utility
{
	public static class SchedulerDefinitionExtensions
	{
		public static void ApplyCronToScheduleDefinition(this ScheduleDefinition definition, string expression)
		{
			var split = expression.Split(' ');

			if (split.Length != 7)
			{
				Debug.LogError("Cron expression should consist of exactly 7 parts!");
				return;
			}
			
			definition.second = ExpressionDescriptor.ConvertCronPart(split[0]);
			definition.minute = ExpressionDescriptor.ConvertCronPart(split[1]);
			definition.hour = ExpressionDescriptor.ConvertCronPart(split[2]);
			definition.dayOfMonth = ExpressionDescriptor.ConvertCronPart(split[3]);
			definition.month = ExpressionDescriptor.ConvertCronPart(split[4]);
			definition.dayOfWeek = ExpressionDescriptor.ConvertCronPart(split[5]);
			definition.year = ExpressionDescriptor.ConvertCronPart(split[6]);
		}
	}
}
