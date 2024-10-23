using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class UsamRoutingResolution : IServiceRoutingResolution
	{
		private readonly UsamRoutingStrategy _strat;

		public UsamRoutingResolution(UsamRoutingStrategy strat)
		{
			_strat = strat;
		}
		
		public Promise Init()
		{
			// eh?
			return Promise.Success;
		}

		public string RoutingMap => string.Join(",", _strat.GetMap().Select(kvp => $"{kvp.Key}:{kvp.Value}"));
	}
	public class UsamRoutingStrategy : IServiceRoutingStrategy
	{
		private UsamService _usam;

		public UsamRoutingStrategy(UsamService usam)
		{
			_usam = usam;
		}

		public Dictionary<string, string> GetMap()
		{
			var map = new Dictionary<string, string>();
			foreach (var setting in _usam.routingSettings)
			{
				var routableServiceName = $"micro_{setting.beamoId}";
				BeamServiceStatus status = null;
				
				switch (setting.selectedOption.type)
				{
					case RoutingOptionType.REMOTE:
						// do not set; an absence of value implies remote
						break;
					case RoutingOptionType.LOCAL:
						//map[routableServiceName] = _usam.latestManifest.localRoutingKey;
						
						if (_usam.TryGetStatus(setting.beamoId, out status))
						{
							foreach (var route in status.availableRoutes)
							{
								if (route.routingKey == _usam.latestManifest.localRoutingKey && route.instances.Count > 0)
								{
									map[routableServiceName] = route.routingKey;
									break;
								}
							}
						}

						
						break;
					case RoutingOptionType.FRIEND:
						map[routableServiceName] = setting.selectedOption.routingKey;
						break;
					case RoutingOptionType.AUTO:
						
						if (_usam.TryGetStatus(setting.beamoId, out status))
						{
							foreach (var route in status.availableRoutes)
							{
								if (route.routingKey == _usam.latestManifest.localRoutingKey && route.instances.Count > 0)
								{
									map[routableServiceName] = route.routingKey;
									break;
								}
							}
						}
						
						// if (_usam.latestStatus.)
							// map[routableServiceName] = 
						break;
				}
			}

			return map;
		}
		
		public Promise<Dictionary<string, string>> GetServiceMap()
		{
			throw new NotImplementedException();
		}
	}
}
