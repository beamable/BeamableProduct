using System;
using System.Text.RegularExpressions;

namespace Beamable.Common.Scheduler
{
	public class CronInvalidException : Exception
	{
		public string CronExpression { get; }
		public string Error { get; }

		public CronInvalidException(string cronExpression, string error)
			: base($"cron=[{cronExpression}] is not valid. reason=[{error}]")
		{
			CronExpression = cronExpression;
			Error = error;
		}
	}

	public static class CronValidation
	{
		static readonly Regex CRON_CLAUSE_REGEX = new Regex("^([0-9]|-|/|\\*|,)+$");

		/// <summary>
		/// Given some string, try to validate if the string is a valid 6 part cron string.
		/// </summary>
		/// <param name="cron">The cron string to test</param>
		/// <param name="message">An out parameter that will be null if the given cron string is valid, or will be some message with a hint as to why the cron string is not valid.</param>
		/// <returns>true if the cron string is valid, false otherwise.</returns>
		public static bool TryValidate(string cron, out string message)
		{
			const int CRON_CLAUSE_LENGTH = 6;
			if (string.IsNullOrEmpty(cron))
			{
				message = "cron string is null or empty";
				return false;
			}

			var parts = cron.Split(new char[] { ' ' }, StringSplitOptions.None);
			if (parts.Length != CRON_CLAUSE_LENGTH)
			{
				message = "must contain six parts";
				return false;
			}

			for (var i = 0; i < CRON_CLAUSE_LENGTH; i++)
			{
				var part = parts[i];
				if (!CRON_CLAUSE_REGEX.IsMatch(part))
				{
					message = $"part=[{i}] does not look like a cron clause";
					return false;
				}

			}

			message = null;
			return true;
		}
	}
}
