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
					Name = $"Test lobby #{i:00}", CurrentUsers = Random.Range(0, maxUsers+1), MaxUsers = maxUsers
				};
				
				data.Add(entry);
			}

			return data;
		}
	}
}
