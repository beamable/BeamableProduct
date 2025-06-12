using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Beamable.Editor.BeamCli.Commands;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public partial class UsamService
	{

		public void DoUpgrade(params string[] codes)
		{
			_cli.ChecksScan(new ChecksScanArgs
			{
				fix = codes
			}).Run().Then(_ =>
			{
				Reload();
			});
		}
		
	}
}
