using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyBehaviour : MonoBehaviour, ILightComponent<PlayerLobbyData>
{
	[Header("Scene References")]
	public TextMeshProUGUI playerIdLabel;
	public TextMeshProUGUI gamerTagLabel;
	public TextMeshProUGUI lobbyStatusLabel;
	public TextMeshProUGUI playersAmountLabel;
	public Button createLobbyBtn;
	public Button findLobbyBtn;
	public Button joinLobbyBtn;
	public Button myLobbyBtn;
	public TextMeshProUGUI joinLeaveButtonLabel;

	private LightBeam _beam;
	private PlayerLobbyData _lobbyData;
	private PlayerLobby _lobby;

	public Promise OnInstantiated(LightBeam beam, PlayerLobbyData model)
	{
		_beam = beam;
		_lobbyData = model;
		_lobby = model.playerLobby;

		UpdatePlayerInformation(model);

		_lobby.OnDataUpdated += DataUpdateCallback;

		createLobbyBtn.HandleClicked(async () =>
		{
			await beam.GotoPage<CreateLobbyDisplayBehaviour, PlayerLobby>(model.playerLobby);
		});

		findLobbyBtn.HandleClicked(async () =>
		{
			await beam.GotoPage<FindLobbyDisplayBehaviour, PlayerLobby>(model.playerLobby);
		});

		myLobbyBtn.HandleClicked(async () =>
		{
			if (!model.playerLobby.IsInLobby)
			{
				Debug.Log("[Lobby] You are not currently in a lobby!");
				return;
			}

			await beam.GotoPage<LobbyDetailsDisplayBehaviour, Lobby>(model.playerLobby.Value);
		});

		if (_lobby.IsInLobby)
		{
			ChangeButtonToLeaveLobby();
		}
		else
		{
			ChangeButtonToJoinLobby();
		}

		return Promise.Success;
	}

	private void ChangeButtonToJoinLobby()
	{
		joinLeaveButtonLabel.text = "Join";
		joinLobbyBtn.HandleClicked(async () =>
		{
			await _beam.GotoPage<JoinLobbyDisplayBehaviour, PlayerLobby>(_lobby);
		});
	}

	private void ChangeButtonToLeaveLobby()
	{
		joinLeaveButtonLabel.text = "Leave";
		joinLobbyBtn.HandleClicked(async () =>
		{
			await _lobby.Leave().Then((_) =>
			{
				_lobby.OnDataUpdated += DataUpdateCallback;
				ChangeButtonToJoinLobby();
			});
		});
	}

	private void DataUpdateCallback(Lobby data)
	{
		int amount = _lobby.IsInLobby ? data.players.Count : 0;
		UpdatePlayerInformation(_lobbyData.playerId, _lobbyData.playerName, _lobbyData.playerLobby.IsInLobby, amount);
	}

	private void UpdatePlayerInformation(PlayerLobbyData model)
	{
		int amount = model.playerLobby.IsInLobby ? model.playerLobby.Value.players.Count : 0;

		UpdatePlayerInformation(model.playerId, model.playerName, model.playerLobby.IsInLobby, amount);
	}

	private void UpdatePlayerInformation(long playerId, string playerName, bool isInLobby, int amount)
	{
		playerIdLabel.text = $"Player Id: {playerId.ToString()}";
		gamerTagLabel.text = $"Player Name: {playerName}";
		lobbyStatusLabel.text = $"Is in Lobby: {isInLobby.ToString()}";

		playersAmountLabel.text = $"Players amount in Lobby: {amount}";
	}
}
