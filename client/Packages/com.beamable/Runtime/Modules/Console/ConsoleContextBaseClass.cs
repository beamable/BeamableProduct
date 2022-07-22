using Beamable.ConsoleCommands;
using System.Linq;
using UnityEngine;

namespace Beamable.Console
{
	public class ConsoleContextBaseClass
	{
		public static long ConsoleContextId
		{
			get => long.Parse(PlayerPrefs.GetString("console_context_id",  "0"));
			set => PlayerPrefs.SetString("console_context_id",  value.ToString());
		}
		protected BeamContext ActiveContext
		{
			get
			{
				if (BeamContext.All.Any(context => context.PlayerId == ConsoleContextId))
				{
					return BeamContext.All.First(context => context.PlayerId == ConsoleContextId);
				}

				return BeamContext.Default;
			}
		}
	}
}
