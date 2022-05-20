using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public static class LobbiesTestDataHelper
	{
		public static List<LobbiesListEntryPresenter.Data> GetTestLobbiesData(int maxUsers)
		{
			List<LobbiesListEntryPresenter.Data> data = new List<LobbiesListEntryPresenter.Data>();

			int lobbiesCount = Random.Range(0, 10);

			for (int i = 0; i < lobbiesCount; i++)
			{
				LobbiesListEntryPresenter.Data entry = new LobbiesListEntryPresenter.Data
				{
					Name = $"Test lobby #{i:00}", CurrentPlayers = Random.Range(0, maxUsers+1), MaxPlayers = maxUsers
				};
				
				data.Add(entry);
			}

			return data;
		}
		
		public static List<LobbiesListEntryPresenter.Data> GetTestLobbiesData()
		{
			List<LobbiesListEntryPresenter.Data> data = new List<LobbiesListEntryPresenter.Data>();

			int lobbiesCount = Random.Range(0, 10);

			for (int i = 0; i < lobbiesCount; i++)
			{
				LobbiesListEntryPresenter.Data entry = new LobbiesListEntryPresenter.Data
				{
					Name = $"Test lobby #{i:00}", CurrentPlayers = Random.Range(0, 8), MaxPlayers = 8
				};
				
				data.Add(entry);
			}

			return data;
		}
	}
}
