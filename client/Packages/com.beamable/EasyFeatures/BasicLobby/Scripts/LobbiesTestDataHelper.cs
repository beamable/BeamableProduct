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
					Name = $"Test lobby #{i:00}",
					CurrentPlayers = Random.Range(0, maxUsers + 1),
					MaxPlayers = maxUsers
				};

				data.Add(entry);
			}

			return data;
		}

		public static List<LobbySlotPresenter.Data> GetTestPlayersData(int players, int maxPlayers)
		{
			List<LobbySlotPresenter.Data> data = new List<LobbySlotPresenter.Data>(maxPlayers);

			for (int i = 0; i < maxPlayers; i++)
			{
				if (i < players)
				{
					data.Add(new LobbySlotPresenter.Data
					{
						Name = $"Random Player #{i:00}",
						IsReady = Random.Range(0.0f, 1.0f) > 0.5f,
						IsOccupied = true
					});
				}
				else
				{
					data.Add(new LobbySlotPresenter.Data {Name = string.Empty, IsReady = false, IsOccupied = false});
				}
			}

			return data;
		}
	}
}
