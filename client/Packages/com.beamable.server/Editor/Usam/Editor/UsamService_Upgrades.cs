using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Beamable.Editor.BeamCli.Commands;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	[Serializable]
	public class UsamServiceUpgrade
	{
		public string beamoId;
		public string description;
		public string file;
		public string currentText;
		public string replacement;
		public int startIndex;
		public int endIndex;
	}

	[Serializable]
	public class UsamServiceUpgradesForBeamoId
	{
		public string beamoId;
		public List<UsamServiceUpgrade> sortedUpgrades = new List<UsamServiceUpgrade>();
	}
	
	public partial class UsamService
	{
		public delegate UsamServiceUpgrade UsamServiceUpgradeFunction(BeamManifestServiceEntry service);


		public static List<UsamServiceUpgradeFunction> serviceUpgradeFunctions = new List<UsamServiceUpgradeFunction>
		{
			DisallowNet6Or7
		};

		public void DoUpgrades(UsamServiceUpgradesForBeamoId upgrades)
		{
			for (var i = 0; i < upgrades.sortedUpgrades.Count; i++)
			{
				var upgrade = upgrades.sortedUpgrades[i];
				var file = File.ReadAllText(upgrade.file);
				var beforeText = file.Substring(0, upgrade.startIndex);
				var afterText = file.Substring(upgrade.endIndex);
				var finalText = beforeText + upgrade.replacement + afterText;
				
				Debug.Log($"Upgrading {upgrade.beamoId} description=[{upgrade.description}] file=[{upgrade.file}]");
				File.WriteAllText(upgrade.file, finalText);
			}
			
			Reload();
		}
		
		public static UsamServiceUpgradesForBeamoId CheckForRequiredUpgrades(BeamManifestServiceEntry service)
		{
			var upgrades = new UsamServiceUpgradesForBeamoId()
			{
				beamoId = service.beamoId
			};

			{ // collect all the upgrades for this service...
				foreach (var function in serviceUpgradeFunctions)
				{
					var maybeUpgrade = function(service);
					if (maybeUpgrade != null)
					{
						upgrades.sortedUpgrades.Add(maybeUpgrade);
					}
				}
			}
			
			// if there are no upgrades to be made, then there is no point
			if (upgrades.sortedUpgrades.Count == 0) return null;

			//  sort the upgrades so they are in reverse order of index
			SortUpgrades(upgrades.sortedUpgrades);

			return upgrades;
		}

		static void SortUpgrades(List<UsamServiceUpgrade> upgrades)
		{
			// TODO: create a check that validates there are no intersecting ranges
			upgrades.Sort((a, b) => a.startIndex.CompareTo(b.startIndex));
		}



		public static UsamServiceUpgrade DisallowNet6Or7(BeamManifestServiceEntry service)
		{
			var text = File.ReadAllText(service.csprojPath);
			var matches = Regex.Matches(text, @"<TargetFramework>\s*net(6|7)\.0\s*</TargetFramework>");

			if (matches.Count > 0)
			{
				return new UsamServiceUpgrade
				{
					beamoId = service.beamoId,
					description = "Microservices must use net8.0",
					file = service.csprojPath,
					currentText = matches[0].Value,
					replacement = "<TargetFramework>net8.0</TargetFramework>",
					startIndex = matches[0].Index,
					endIndex = matches[0].Index + matches[0].Length
				};

			}
			return null;
		}
		
		
	}
}
